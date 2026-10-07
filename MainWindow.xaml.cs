using PlantMaster.ViewModels;
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
        public MainWindow()
        {
            InitializeComponent();



            // 获取程序全局服务
            //
            // 服务在App.xaml.cs中创建
            App app =
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
        }
    }
}