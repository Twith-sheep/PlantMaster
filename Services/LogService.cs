using PlantMaster.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PlantMaster.Services
{
    /// <summary>
    /// 日志服务
    ///
    /// 负责：
    /// 1. 创建日志
    /// 2. 提供日志集合给WPF显示
    /// 3. 缓存日志
    /// 4. 批量保存日志文件
    ///
    /// 注意：
    /// 不采用每条日志立即写文件方式。
    /// 
    /// 原因：
    /// PLC主站可能长期运行，
    /// 高频通信会产生大量日志，
    /// 每次写磁盘会增加IO压力。
    ///
    /// 所以采用：
    /// 内存缓存 + 批量保存
    /// </summary>
    public class LogService
    {


        /// <summary>
        /// 日志集合
        ///
        /// 提供给WPF界面绑定显示。
        ///
        /// ObservableCollection特点：
        /// 集合增加数据时，
        /// 会自动通知界面刷新。
        /// </summary>
        public ObservableCollection<LogItem> Logs { get; }



        /// <summary>
        /// 待保存日志集合
        ///
        /// 保存还没有写入文件的日志。
        ///
        /// 多条日志累计后，
        /// 一次性写入磁盘。
        /// readonl被他定义的指针不能换盒子
        /// </summary>
        private readonly List<LogItem> pendingLogs;



        /// <summary>
        /// 日志文件保存目录
        /// </summary>
        private readonly string logDirectory;



        /// <summary>
        /// 创建日志服务
        ///
        /// 初始化：
        /// 1. 创建显示日志集合
        /// 2. 创建待保存缓存
        /// 3. 创建日志目录
        /// </summary>
        public LogService()
        {

            // WPF绑定使用
            Logs = new ObservableCollection<LogItem>();


            // 等待写入文件的日志
            pendingLogs = new List<LogItem>();

            

            // 日志保存路径
            // 程序目录下 Logs 文件夹
            logDirectory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Logs"
                );


            // 不存在则创建目录
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

        }



        /// <summary>
        /// 记录普通信息日志
        ///
        /// 例如：
        /// 网关连接成功
        /// PLC读取成功
        /// </summary>
        public void Info(string message)
        {
            AddLog("INFO", message);
        }



        /// <summary>
        /// 记录警告日志
        ///
        /// 例如：
        /// 通信延迟
        /// 数据异常
        /// </summary>
        public void Warn(string message)
        {
            AddLog("WARN", message);
        }



        /// <summary>
        /// 记录错误日志
        ///
        /// 例如：
        /// 网关断开
        /// PLC读取失败
        /// </summary>
        public void Error(string message)
        {
            AddLog("ERROR", message);
        }



        /// <summary>
        /// 添加日志核心方法
        ///
        /// 负责：
        /// 1. 创建日志对象
        /// 2. 更新界面显示
        /// 3. 添加到等待保存列表
        /// </summary>
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


            // 添加到界面日志列表
            //
            // WPF绑定此集合，
            // 添加后自动刷新显示
            Logs.Add(log);



            // 添加到待保存缓存
            //
            // 等待后续批量写入文件
            pendingLogs.Add(log);

        }



        /// <summary>
        /// 保存缓存日志
        ///
        /// 将pendingLogs中的日志
        /// 一次性写入文件。
        ///
        /// 后续可以：
        /// 1. 定时调用
        /// 2. 程序关闭时调用
        /// </summary>
        public void SaveLogs()
        {

            // 没有日志无需保存
            if (pendingLogs.Count == 0)
            {
                return;
            }


            string fileName =
                DateTime.Now.ToString("yyyy-MM-dd")
                + ".log";


            string filePath =
                Path.Combine(
                    logDirectory,
                    fileName
                );


            StringBuilder builder = new StringBuilder();



            foreach (var log in pendingLogs)
            {
                builder.AppendLine(
                    $"{log.Time:yyyy-MM-dd HH:mm:ss} " +
                    $"[{log.Level}] " +
                    $"{log.Message}"
                );
            }



            // 一次性写入文件
            File.AppendAllText(
                filePath,
                builder.ToString()
            );



            // 清空已经保存的日志
            pendingLogs.Clear();

        }

    }
}