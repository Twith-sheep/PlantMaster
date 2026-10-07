using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlantMaster.Services
{
    /// <summary>
    /// 日志保存服务
    ///
    /// 负责：
    /// 定时调用LogService保存日志
    ///
    /// 注意：
    /// 不负责生成日志
    /// 不负责管理日志集合
    /// 只负责定时任务
    /// </summary>
    public class LogSaveService
    {
        //给方法定时的 写入日志这个方法定时

        /// <summary>
        /// 日志服务
        ///
        /// 保存服务通过它获取待保存日志
        /// </summary>
        private readonly LogService logService;



        /// <summary>
        /// 后台运行任务
        ///
        /// 用于持续执行定时保存
        /// </summary>
        private Task saveTask;



        /// <summary>
        /// 控制后台任务停止
        ///
        /// 程序关闭时取消任务
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;



        /// <summary>
        /// 创建日志保存服务
        /// </summary>
        /// <param name="logService">
        /// 已存在的日志服务
        /// </param>
        public LogSaveService(LogService logService)
        {
            
            this.logService = logService;

            cancellationTokenSource =
                new CancellationTokenSource();
        }



        /// <summary>
        /// 启动日志定时保存
        ///
        /// 默认：
        /// 每5秒保存一次日志
        /// </summary>
        public void Start()
        {

            saveTask = Task.Run(async () =>
            {

                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {


                    // 等待5秒
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        cancellationTokenSource.Token
                    );



                    // 批量保存日志
                    logService.SaveLogs();

                }


            });

        }



        /// <summary>
        /// 停止日志保存任务
        ///
        /// 程序退出时调用
        /// 防止后台任务继续运行
        /// </summary>
        public async Task Stop()
        {

            cancellationTokenSource.Cancel();

            await saveTask;
        }


    }
}