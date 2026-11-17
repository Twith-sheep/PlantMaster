using PlantMaster.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;


namespace PlantMaster.Services
{
    /// <summary>
    /// 线程安全的日志生产服务。
    ///
    /// 负责：
    /// 1.创建日志对象
    /// 2.将待保存日志放入并发队列
    /// 3.通知ViewModel有新日志
    ///
    /// 不负责UI集合和文件写入。
    /// </summary>
    public class LogService
    {
        /*
         * PlantMaster 使用 ConcurrentQueue<LogItem> 主要有三个原因：
        1. 线程安全：多个 PLC 网关同时产生日志，不会因为同时操作队列而发生数据竞争。
        2. 减少阻塞：产生日志时只需快速入队，不必等待磁盘写入。
        3. 提高效率：后台统一批量保存日志，减少频繁写文件的开销。
        一句话总结：使用线程安全队列，实现日志生成与保存分离，提高多线程环境下的安全性和运行效率。
        */
        private readonly ConcurrentQueue<LogItem> pendingLogs;


        /// <summary>
        /// event 只能在定义他的类中触发，外部只能订阅和取消订阅。
        /// 新日志事件。事件在产生日志的线程上发布，
        /// 订阅者必须自行切换到需要的同步上下文。
        /// </summary>
        public event Action<LogItem>? LogAdded;


        public LogService()
        {
            pendingLogs =
                new ConcurrentQueue<LogItem>();
        }


        public void Info(string message)
        {
            AddLog("INFO", message);
        }


        public void Warn(string message)
        {
            AddLog("WARN", message);
        }


        public void Error(string message)
        {
            AddLog("ERROR", message);
        }


        private void AddLog(
            string level,
            string message)
        {
            LogItem log = new LogItem()
            {
                Time = DateTime.Now,
                Level = level,
                Message = message
            };


            // 先进入持久化队列，
            // 即使UI通知失败也不会丢失待保存日志。
            pendingLogs.Enqueue(log);


            NotifyLogAdded(log);
        }


        private void NotifyLogAdded(LogItem log)
        {
            Action<LogItem>? handlers =
                LogAdded;

            if (handlers == null)
            {
                return;
            }


            foreach (Action<LogItem> handler
                in handlers.GetInvocationList())
            {
                try
                {
                    handler(log);
                }
                catch (Exception ex)
                {
                    // 不能用LogService记录自身事件异常，
                    // 否则会递归触发LogAdded。
                    Debug.WriteLine(
                        $"LogAdded通知失败:{ex}"
                    );
                }
            }
        }


        /// <summary>
        /// 从并发队列中取出一个待保存批次。
        /// </summary>
        internal List<LogItem> DequeueBatch(int maximumCount)
        {
            if (maximumCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCount)
                );
            }


            List<LogItem> batch =
                new List<LogItem>();

            while (batch.Count < maximumCount
                && pendingLogs.TryDequeue(
                    out LogItem? log
                ))
            {
                batch.Add(log);
            }


            return batch;
        }


        internal bool HasPendingLogs
        {
            get
            {
                return !pendingLogs.IsEmpty;
            }
        }
    }
}
