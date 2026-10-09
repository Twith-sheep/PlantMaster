using PlantMaster.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace PlantMaster
{
    /// <summary>
    /// 主窗口
    ///
    /// 负责：
    /// 1.创建MainViewModel
    /// 2.设置DataContext
    ///
    /// 不负责：
    /// 1.业务逻辑
    /// 2.文件操作
    /// 3.网关通信
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly App app;

        private bool isClosing;

        private bool allowClose;


        public MainWindow()
        {
            InitializeComponent();



            // 获取程序全局服务
            //
            // 服务在App.xaml.cs中创建
            app =
                (App)Application.Current;



            /*
             * MainViewModel依赖：
             *
             * LogService
             * ConfigService
             * GatewayService
             * FileDialogService
             *
             * 这里负责把它们组合起来
             */

            DataContext =
                new MainViewModel(
                    app.LogService,
                    app.GatewayService,
                    app.FileDialogService,
                    app.ConfigManager
                );


            Closing += MainWindow_Closing;
        }


        /// <summary>
        /// 普通窗口关闭时先异步释放网关，再最终保存日志。
        /// 第二次Closing只负责真正关闭窗口。
        /// </summary>
        private async void MainWindow_Closing(
            object? sender,
            CancelEventArgs e)
        {
            if (allowClose
                || app.IsShutdownCompleted)
            {
                return;
            }


            e.Cancel = true;

            if (isClosing)
            {
                return;
            }


            isClosing = true;

            try
            {
                await app.ShutdownApplicationAsync();
            }
            finally
            {
                allowClose = true;
                isClosing = false;

                Close();
            }
        }
    }
}
