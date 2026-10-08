using PlantMaster.Infrastructure.Config;
using PlantMaster.Infrastructure.Config.Interfaces;
using PlantMaster.Infrastructure.Config.Readers;
using PlantMaster.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace PlantMaster
{
    /// <summary>
    /// 程序入口
    ///
    /// 负责：
    /// 1.创建全局服务对象
    /// 2.管理程序生命周期中的公共依赖
    ///
    /// 不负责：
    /// 1.业务逻辑
    /// 2.界面逻辑
    /// </summary>
    public partial class App : Application
    {


        /// <summary>
        /// 日志服务
        ///
        /// 整个程序共用一个实例
        ///
        /// 负责：
        /// 1.产生日志
        /// 2.提供日志集合
        /// </summary>
        public LogService LogService
        {
            get;
        }



        /// <summary>
        /// 日志保存服务
        ///
        /// 定时将日志写入文件
        /// </summary>
        public LogSaveService LogSaveService
        {
            get;
        }



        /// <summary>
        /// 配置管理器
        ///
        /// 负责：
        /// 1.管理运行中的配置数据
        /// 2.加载保存配置
        /// 3.调用配置读取器
        /// </summary>
        public ConfigManager ConfigManager
        {
            get;
        }


        /// <summary>
        /// 网关服务
        ///
        /// 负责：
        /// 1.管理网关连接
        /// 2.维护网关状态
        /// 3.调用通信模块
        /// 4.记录网关运行日志
        /// </summary>
        public GatewayService GatewayService
        {
            get;
        }



        /// <summary>
        /// 文件选择服务
        ///
        /// 负责：
        /// 1.打开文件选择窗口
        /// 2.获取用户选择的文件路径
        ///
        /// 不负责：
        /// 1.读取文件内容
        /// 2.XML解析
        /// 3.TXT解析
        /// </summary>
        public FileDialogService FileDialogService
        {
            get;
        }





        /// <summary>
        /// App构造函数
        ///
        /// 程序启动时创建所有服务
        ///
        /// 这里是整个程序的组合根
        /// </summary>
        public App()
        {


            // 创建日志服务
            //
            // 整个程序只使用这一个实例
            LogService =
                new LogService();





            // 创建日志保存服务
            //
            // 使用同一个LogService
            //
            // 不重新创建日志对象
            LogSaveService =
                new LogSaveService(
                    LogService
                );


            // 创建XML配置读取器
            //
            // 负责：
            // XML文件 <--> 对象
            IConfigReader configReader =
                new XmlConfigReader();



            // 创建配置管理器
            //
            // 负责：
            // 管理程序运行中的配置
            ConfigManager =
                new ConfigManager(
                    configReader,
                    LogService
                );



            // 创建网关管理服务
            //
            // 负责网关连接和状态管理
            //
            // 使用同一个日志服务
            GatewayService =
                new GatewayService(
                    LogService
                );





            // 创建文件选择服务
            //
            // 负责调用Windows文件选择窗口
            //
            // 不负责文件读取解析
            FileDialogService =
                new FileDialogService();





            // 启动日志定时保存
            LogSaveService.Start();

        }



        /// <summary>
        /// 程序退出前停止所有网关，
        /// 避免正在通信时直接释放TCP连接。
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                using CancellationTokenSource shutdownSource =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(15)
                    );

                GatewayService
                    .ShutdownAsync(shutdownSource.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                LogService.Error(
                    $"程序关闭网关失败:{ex.Message}"
                );
            }


            try
            {
                using CancellationTokenSource logShutdownSource =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(15)
                    );

                LogSaveService
                    .StopAsync(logShutdownSource.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                // 最终刷盘失败时仍保留内存批次，
                // 同时输出诊断信息，避免递归调用文件日志。
                Debug.WriteLine(
                    $"程序关闭日志最终刷盘失败:{ex}"
                );
            }


            base.OnExit(e);
        }


    }
}
