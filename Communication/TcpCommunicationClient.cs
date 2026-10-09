using PlantMaster.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace PlantMaster.Communication
{
    /// <summary>
    /// TCP字节流通信客户端。
    ///
    /// 只负责连接和字节传输，不负责PLC协议解析。
    /// 调用方必须通过GatewayService串行调度同一实例。
    /// </summary>
    public sealed class TcpCommunicationClient : ICommunicationClient
    {
        private TcpClient? tcpClient;

        private NetworkStream? networkStream;

        private readonly string ipAddress;

        private readonly int port;

        private readonly LogService logService;

        private int isDisposed;


        public TcpCommunicationClient(
            string ipAddress,
            int port,
            LogService logService)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.logService = logService;
        }


        public async Task ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();


            Exception? previousCleanupException =
                ReleaseCurrentResources(
                    "连接前清理旧资源"
                );

            if (previousCleanupException != null)
            {
                throw new IOException(
                    $"TCP旧连接资源清理失败 {ipAddress}:{port}",
                    previousCleanupException
                );
            }


            TcpClient? newClient =
                new TcpClient();

            NetworkStream? newStream = null;


            try
            {
                await newClient.ConnectAsync(
                    ipAddress,
                    port,
                    cancellationToken
                );

                newStream =
                    newClient.GetStream();


                tcpClient = newClient;
                networkStream = newStream;

                newClient = null;
                newStream = null;


                logService.Info(
                    $"TCP连接成功 {ipAddress}:{port}"
                );
            }
            catch (OperationCanceledException)
            {
                DisposeResources(
                    newStream,
                    newClient,
                    "连接取消清理"
                );

                logService.Warn(
                    $"TCP连接已取消 {ipAddress}:{port}"
                );

                throw;
            }
            catch (Exception ex)
            {
                DisposeResources(
                    newStream,
                    newClient,
                    "连接失败清理"
                );

                logService.Error(
                    $"TCP连接失败 {ipAddress}:{port} {ex.Message}"
                );

                throw;
            }
        }


        /// <summary>
        /// 断开并释放当前连接。
        /// 清理是幂等的，但清理异常会向调用方传播。
        /// </summary>
        public Task DisconnectAsync()
        {
            Exception? cleanupException =
                ReleaseCurrentResources(
                    "断开连接"
                );

            if (cleanupException != null)
            {
                throw new IOException(
                    $"TCP断开失败 {ipAddress}:{port}",
                    cleanupException
                );
            }


            logService.Info(
                $"TCP断开 {ipAddress}:{port}"
            );

            return Task.CompletedTask;
        }


        public async Task SendAsync(
            byte[] request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ThrowIfDisposed();

            NetworkStream stream =
                GetActiveStream("发送");


            try
            {
                await stream.WriteAsync(
                    request.AsMemory(),
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                logService.Warn(
                    $"TCP发送已取消 {ipAddress}:{port}"
                );

                throw;
            }
            catch (Exception ex)
            {
                logService.Error(
                    $"TCP发送失败 {ipAddress}:{port} {ex.Message}"
                );

                throw;
            }
        }


        public async Task<byte[]> ReceiveAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            NetworkStream stream =
                GetActiveStream("接收");


            try
            {
                byte[] buffer =
                    new byte[1024];

                int length =
                    await stream.ReadAsync(
                        buffer.AsMemory(),
                        cancellationToken
                    );

                if (length == 0)
                {
                    throw new EndOfStreamException(
                        $"TCP远端已关闭连接 {ipAddress}:{port}"
                    );
                }


                byte[] result =
                    new byte[length];

                Array.Copy(
                    buffer,
                    result,
                    length
                );

                return result;
            }
            catch (OperationCanceledException)
            {
                logService.Warn(
                    $"TCP接收已取消 {ipAddress}:{port}"
                );

                throw;
            }
            catch (Exception ex)
            {
                logService.Error(
                    $"TCP接收失败 {ipAddress}:{port} {ex.Message}"
                );

                throw;
            }
        }


        private NetworkStream GetActiveStream(
            string operationName)
        {
            NetworkStream? stream =
                networkStream;

            if (tcpClient == null
                || stream == null)
            {
                string message =
                    $"TCP未连接，无法{operationName}数据 {ipAddress}:{port}";

                logService.Warn(message);

                throw new InvalidOperationException(message);
            }


            return stream;
        }


        private Exception? ReleaseCurrentResources(
            string operationName)
        {
            NetworkStream? stream =
                Interlocked.Exchange(
                    ref networkStream,
                    null
                );

            TcpClient? client =
                Interlocked.Exchange(
                    ref tcpClient,
                    null
                );

            return DisposeResources(
                stream,
                client,
                operationName
            );
        }


        /// <summary>
        /// 分别尝试释放流和客户端，任何一个失败都不会阻止另一个。
        /// 返回异常而不是直接抛出，避免覆盖原始通信异常。
        /// </summary>
        private Exception? DisposeResources(
            NetworkStream? stream,
            TcpClient? client,
            string operationName)
        {
            List<Exception>? exceptions = null;


            try
            {
                stream?.Dispose();
            }
            catch (Exception ex)
            {
                exceptions ??=
                    new List<Exception>();

                exceptions.Add(ex);

                SafeLogError(
                    $"TCP网络流释放失败 {ipAddress}:{port} "
                    + $"操作:{operationName} {ex.Message}"
                );
            }


            try
            {
                client?.Dispose();
            }
            catch (Exception ex)
            {
                exceptions ??=
                    new List<Exception>();

                exceptions.Add(ex);

                SafeLogError(
                    $"TCP客户端释放失败 {ipAddress}:{port} "
                    + $"操作:{operationName} {ex.Message}"
                );
            }


            if (exceptions == null)
            {
                return null;
            }

            if (exceptions.Count == 1)
            {
                return exceptions[0];
            }

            return new AggregateException(exceptions);
        }


        private void SafeLogError(string message)
        {
            try
            {
                logService.Error(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"TCP资源清理日志记录失败:{ex}"
                );
            }
        }


        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref isDisposed) == 1,
                this
            );
        }


        /// <summary>
        /// 最终资源回收保障。Dispose本身不向外抛出异常。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(
                ref isDisposed,
                1
            ) == 1)
            {
                return;
            }


            ReleaseCurrentResources(
                "Dispose"
            );
        }
    }
}
