using PlantMaster.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace PlantMaster.Services
{
    /// <summary>
    /// 日志文件的单消费者保存服务。
    /// </summary>
    public class LogSaveService
    {
        private const int MaximumBatchSize = 1000;

        private static readonly TimeSpan SaveInterval =
            TimeSpan.FromSeconds(5);


        private readonly LogService logService;

        private readonly string logDirectory;

        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly SemaphoreSlim saveSemaphore;

        private readonly SemaphoreSlim stopSemaphore;

        private readonly List<LogItem> retryBatch;


        private Task? saveTask;

        private int isStarted;

        private int isStopped;

        private bool saveFailureReported;


        public LogSaveService(LogService logService)
        {
            this.logService = logService;

            logDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs"
            );

            cancellationTokenSource =
                new CancellationTokenSource();

            saveSemaphore =
                new SemaphoreSlim(1, 1);

            stopSemaphore =
                new SemaphoreSlim(1, 1);

            retryBatch =
                new List<LogItem>();
        }


        /// <summary>
        /// 启动唯一的后台保存消费者。
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(
                ref isStarted,
                1,
                0
            ) != 0)
            {
                return;
            }


            saveTask = Task.Run(
                () => RunSaveLoopAsync(
                    cancellationTokenSource.Token
                )
            );
        }


        private async Task RunSaveLoopAsync(
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        SaveInterval,
                        cancellationToken
                    ).ConfigureAwait(false);


                    bool saved =
                        await FlushOneBatchAsync(
                            cancellationToken
                        ).ConfigureAwait(false);


                    if (saved
                        && saveFailureReported)
                    {
                        saveFailureReported = false;

                        logService.Info(
                            "日志文件保存已恢复"
                        );
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ReportSaveFailure(ex);
                }
            }
        }


        private void ReportSaveFailure(Exception exception)
        {
            if (!saveFailureReported)
            {
                saveFailureReported = true;

                logService.Error(
                    $"日志文件保存失败，将在下一批次重试:{exception.Message}"
                );

                return;
            }


            // 磁盘持续失败时不反复向待保存队列追加错误日志。
            Debug.WriteLine(
                $"日志文件重试失败:{exception}"
            );
        }


        /// <summary>
        /// 保存一个有上限的日志批次。
        /// 失败时整个批次保留到retryBatch。
        /// </summary>
        private async Task<bool> FlushOneBatchAsync(
            CancellationToken cancellationToken)
        {
            await saveSemaphore.WaitAsync(
                cancellationToken
            ).ConfigureAwait(false);


            try
            {
                List<LogItem> batch =
                    TakeNextBatch();

                if (batch.Count == 0)
                {
                    return false;
                }


                try
                {
                    await WriteBatchAsync(
                        batch,
                        cancellationToken
                    ).ConfigureAwait(false);

                    return true;
                }
                catch
                {
                    // retryBatch只在saveSemaphore保护下读写。
                    retryBatch.InsertRange(
                        0,
                        batch
                    );

                    throw;
                }
            }
            finally
            {
                saveSemaphore.Release();
            }
        }


        private List<LogItem> TakeNextBatch()
        {
            List<LogItem> batch =
                new List<LogItem>(MaximumBatchSize);


            if (retryBatch.Count > 0)
            {
                batch.AddRange(retryBatch);
                retryBatch.Clear();
            }


            int remainingCount =
                MaximumBatchSize - batch.Count;

            if (remainingCount > 0)
            {
                batch.AddRange(
                    logService.DequeueBatch(
                        remainingCount
                    )
                );
            }


            return batch;
        }


        private async Task WriteBatchAsync(
            IReadOnlyList<LogItem> batch,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(
                logDirectory
            );


            string fileName =
                batch[0].Time.ToString("yyyy-MM-dd")
                + ".log";

            string filePath = Path.Combine(
                logDirectory,
                fileName
            );


            StringBuilder builder =
                new StringBuilder();

            foreach (LogItem log in batch)
            {
                builder.AppendLine(
                    $"{log.Time:yyyy-MM-dd HH:mm:ss} "
                    + $"[{log.Level}] "
                    + log.Message
                );
            }


            await File.AppendAllTextAsync(
                filePath,
                builder.ToString(),
                Encoding.UTF8,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// 停止定时消费者并将剩余日志全部刷盘。
        /// </summary>
        public async Task StopAsync(
            CancellationToken cancellationToken = default)
        {
            await stopSemaphore.WaitAsync(
                cancellationToken
            ).ConfigureAwait(false);


            try
            {
                if (Volatile.Read(ref isStopped) == 1)
                {
                    return;
                }


                cancellationTokenSource.Cancel();


                if (saveTask != null)
                {
                    await saveTask.ConfigureAwait(false);
                }


                while (retryBatch.Count > 0
                    || logService.HasPendingLogs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await FlushOneBatchAsync(
                        cancellationToken
                    ).ConfigureAwait(false);
                }


                Volatile.Write(
                    ref isStopped,
                    1
                );
            }
            finally
            {
                stopSemaphore.Release();
            }
        }
    }
}
