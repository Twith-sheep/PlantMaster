using PlantMaster.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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

        private bool isLogCollapsed;

        private GridLength expandedLogHeight = new(220);


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
            StateChanged += MainWindow_StateChanged;
        }


        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizedState();
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }


        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        private void MaximizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ToggleMaximizedState();
        }


        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        private void MainWindow_StateChanged(
            object? sender,
            EventArgs e)
        {
            MaximizeButton.Content =
                WindowState == WindowState.Maximized
                    ? "❐"
                    : "□";
        }


        private void ToggleMaximizedState()
        {
            WindowState =
                WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
        }


        private void CollapseLogButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!isLogCollapsed)
            {
                if (LogRow.ActualHeight > 0)
                {
                    expandedLogHeight =
                        new GridLength(LogRow.ActualHeight);
                }

                LogPanel.Visibility = Visibility.Collapsed;
                LogSplitter.Visibility = Visibility.Collapsed;
                LogRow.MinHeight = 0;
                LogRow.Height = new GridLength(0);
                CollapseLogButton.Content = "⌄";
                CollapseLogButton.ToolTip = "展开运行日志";
            }
            else
            {
                LogPanel.Visibility = Visibility.Visible;
                LogSplitter.Visibility = Visibility.Visible;
                LogRow.MinHeight = 120;
                LogRow.Height = expandedLogHeight;
                CollapseLogButton.Content = "⌃";
                CollapseLogButton.ToolTip = "折叠运行日志";
            }

            isLogCollapsed = !isLogCollapsed;
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
