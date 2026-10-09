using PlantMaster.Common;
using PlantMaster.Infrastructure.Config;
using PlantMaster.Models;
using PlantMaster.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;


namespace PlantMaster.ViewModels
{

    /// <summary>
    /// 主窗口ViewModel
    ///
    /// 负责界面数据和命令
    /// 不负责配置解析和通信
    /// </summary>
    public class MainViewModel : ObservableObject
    {


        private const int MaximumDisplayedLogCount = 5000;


        //日志服务
        private readonly LogService logService;


        //配置管理
        private readonly ConfigManager configManager;


        //网关运行管理
        private readonly GatewayService gatewayService;


        //文件选择
        private readonly FileDialogService fileDialogService;


        // 创建ViewModel时的UI同步上下文
        private readonly SynchronizationContext uiSynchronizationContext;

        private readonly DispatcherTimer currentTimeTimer;

        private GatewayViewModel? selectedGateway;

        private string gatewaySearchText = string.Empty;

        private DateTime currentTime = DateTime.Now;




        /// <summary>
        /// 日志显示集合
        /// </summary>
        public ObservableCollection<LogItem> Logs
        {
            get;
        }




        /// <summary>
        /// 界面显示网关集合
        ///
        /// 注意：
        /// 这里保存的是ViewModel
        /// 不是配置对象
        /// </summary>
        public ObservableCollection<GatewayViewModel> Gateways
        {
            get;
        }


        public ICollectionView GatewaysView
        {
            get;
        }


        public GatewayViewModel? SelectedGateway
        {
            get
            {
                return selectedGateway;
            }

            set
            {
                if (ReferenceEquals(
                    selectedGateway,
                    value
                ))
                {
                    return;
                }


                selectedGateway = value;

                foreach (GatewayViewModel gateway
                    in Gateways)
                {
                    gateway.IsSelected =
                        ReferenceEquals(
                            gateway,
                            selectedGateway
                        );
                }


                OnPropertyChanged(
                    nameof(SelectedGateway)
                );
            }
        }


        public string GatewaySearchText
        {
            get
            {
                return gatewaySearchText;
            }

            set
            {
                if (gatewaySearchText == value)
                {
                    return;
                }


                gatewaySearchText =
                    value ?? string.Empty;

                OnPropertyChanged(
                    nameof(GatewaySearchText)
                );

                GatewaysView.Refresh();
            }
        }


        public int GatewayCount =>
            Gateways.Count;


        public int OnlineGatewayCount =>
            Gateways.Count(gateway => gateway.IsOnline);


        public int OfflineGatewayCount =>
            GatewayCount - OnlineGatewayCount;


        public string CurrentTime =>
            currentTime.ToString("yyyy-MM-dd HH:mm:ss");


        public string ApplicationVersion =>
            typeof(MainViewModel)
                .Assembly
                .GetName()
                .Version?
                .ToString(3)
            ?? "未设置";




        public ICommand ImportGatewayCommand
        {
            get;
        }


        public ICommand ConnectGatewayCommand
        {
            get;
        }


        public ICommand DisconnectGatewayCommand
        {
            get;
        }


        public ICommand DeleteGatewayCommand
        {
            get;
        }


        public ICommand DeleteSelectedGatewayCommand
        {
            get;
        }





        public MainViewModel(
            LogService logService,
            GatewayService gatewayService,
            FileDialogService fileDialogService,
            ConfigManager configManager)
        {

            this.logService =
                logService;


            this.gatewayService =
                gatewayService;


            this.fileDialogService =
                fileDialogService;


            this.configManager =
                configManager;


            uiSynchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(
                    "MainViewModel必须在UI线程创建"
                );


            Logs =
                new ObservableCollection<LogItem>();


            logService.LogAdded +=
                LogService_LogAdded;



            Gateways =
                new ObservableCollection<GatewayViewModel>();

            Gateways.CollectionChanged +=
                (_, _) => UpdateGatewaySummary();


            GatewaysView =
                CollectionViewSource.GetDefaultView(
                    Gateways
                );

            GatewaysView.Filter =
                FilterGateway;


            gatewayService.ConnectionStateChanged +=
                GatewayService_ConnectionStateChanged;



            ImportGatewayCommand =
                new RelayCommand(
                    ImportGateway
                );


            ConnectGatewayCommand =
                new AsyncRelayCommand(
                    ConnectGatewayAsync,
                    exception => HandleCommandException(
                        "连接网关",
                        exception
                    )
                );


            DisconnectGatewayCommand =
                new AsyncRelayCommand(
                    DisconnectGatewayAsync,
                    exception => HandleCommandException(
                        "断开网关",
                        exception
                    )
                );


            DeleteGatewayCommand =
                new AsyncRelayCommand(
                    DeleteGatewayAsync,
                    exception => HandleCommandException(
                        "删除网关",
                        exception
                    )
                );


            DeleteSelectedGatewayCommand =
                new AsyncRelayCommand(
                    DeleteSelectedGatewayAsync,
                    exception => HandleCommandException(
                        "批量删除网关",
                        exception
                    )
                );



            LoadGateways();


            currentTimeTimer =
                new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(1)
                };

            currentTimeTimer.Tick +=
                (_, _) =>
                {
                    currentTime = DateTime.Now;

                    OnPropertyChanged(
                        nameof(CurrentTime)
                    );
                };

            currentTimeTimer.Start();



            logService.Info(
                "主界面初始化完成"
            );

        }


        private bool FilterGateway(object item)
        {
            if (item is not GatewayViewModel gateway)
            {
                return false;
            }


            if (string.IsNullOrWhiteSpace(
                GatewaySearchText
            ))
            {
                return true;
            }


            string keyword =
                GatewaySearchText.Trim();

            return gateway.Setting.Name.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                )
                || gateway.Setting.Id.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                )
                || gateway.Setting.IpAddress.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                )
                || gateway.Setting.Port.ToString().Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                );
        }


        private void UpdateGatewaySummary()
        {
            OnPropertyChanged(
                nameof(GatewayCount)
            );

            OnPropertyChanged(
                nameof(OnlineGatewayCount)
            );

            OnPropertyChanged(
                nameof(OfflineGatewayCount)
            );
        }



        /// <summary>
        /// 将任意业务线程产生的日志切换到UI线程显示。
        /// </summary>
        private void LogService_LogAdded(LogItem log)
        {
            if (SynchronizationContext.Current
                == uiSynchronizationContext)
            {
                AddLogToView(log);
                return;
            }


            uiSynchronizationContext.Post(
                _ => AddLogToView(log),
                null
            );
        }


        private void AddLogToView(LogItem log)
        {
            Logs.Add(log);


            while (Logs.Count
                > MaximumDisplayedLogCount)
            {
                Logs.RemoveAt(0);
            }
        }







        /// <summary>
        /// 加载配置生成界面数据
        /// </summary>
        private void LoadGateways()
        {

            configManager.LoadGateways();



            foreach (GatewaySetting setting
                in configManager.Gateways)
            {

                Gateways.Add(
                    new GatewayViewModel(
                        setting,
                        gatewayService.IsOnline(setting.Id)
                    )
                );

            }


            SelectedGateway =
                Gateways.FirstOrDefault();



            logService.Info(
                $"加载网关完成:{Gateways.Count}"
            );

        }







        /// <summary>
        /// 导入配置
        /// </summary>
        private void ImportGateway()
        {

            string path =
                fileDialogService.OpenFile();



            if (string.IsNullOrEmpty(path))
            {

                logService.Info(
                    "取消导入配置"
                );

                return;

            }



            configManager.ImportGatewayConfig(
                path
            );



            RefreshGateways();

        }







        /// <summary>
        /// 刷新界面列表
        /// </summary>
        private void RefreshGateways()
        {

            string? selectedGatewayId =
                SelectedGateway?.Setting.Id;

            Gateways.Clear();



            foreach (GatewaySetting setting
                in configManager.Gateways)
            {

                Gateways.Add(
                    new GatewayViewModel(
                        setting,
                        gatewayService.IsOnline(setting.Id)
                    )
                );

            }


            SelectedGateway =
                Gateways.FirstOrDefault(
                    gateway => gateway.Setting.Id
                        == selectedGatewayId
                )
                ?? Gateways.FirstOrDefault();

            GatewaysView.Refresh();



            logService.Info(
                $"刷新网关完成:{Gateways.Count}"
            );

        }



        /// <summary>
        /// 将服务层的真实连接状态投影到界面状态。
        /// </summary>
        private void GatewayService_ConnectionStateChanged(
            object? sender,
            GatewayConnectionStateChangedEventArgs e)
        {
            void UpdateViewModel()
            {
                GatewayViewModel? gatewayViewModel =
                    Gateways.FirstOrDefault(
                        gateway => gateway.Setting.Id == e.GatewayId
                    );

                gatewayViewModel?.UpdateOnlineState(
                    e.IsOnline
                );

                UpdateGatewaySummary();
            }


            if (SynchronizationContext.Current == uiSynchronizationContext)
            {
                UpdateViewModel();
                return;
            }


            uiSynchronizationContext.Post(
                _ => UpdateViewModel(),
                null
            );
        }








        /// <summary>
        /// 连接选中的网关
        ///
        /// 连接逻辑交给GatewayService
        /// </summary>
        private async Task ConnectGatewayAsync()
        {
            GatewayViewModel[] selectedGateways =
                Gateways
                    .Where(gateway => gateway.IsSelected)
                    .ToArray();


            foreach (GatewayViewModel gatewayVM
                in selectedGateways)
            {
                gatewayService.AddGateway(
                    gatewayVM.Setting
                );
            }


            Task[] connectTasks =
                selectedGateways
                    .Select(gateway =>
                        gatewayService.ConnectAsync(
                            gateway.Setting.Id
                        ))
                    .ToArray();


            await Task.WhenAll(connectTasks);

        }







        /// <summary>
        /// 断开选中的网关
        /// </summary>
        private async Task DisconnectGatewayAsync()
        {
            Task[] disconnectTasks =
                Gateways
                    .Where(gateway => gateway.IsSelected)
                    .Select(gateway =>
                        gatewayService.DisconnectAsync(
                            gateway.Setting.Id
                        ))
                    .ToArray();


            await Task.WhenAll(disconnectTasks);

        }







        /// <summary>
        /// 删除单个网关
        ///
        /// 先释放运行资源
        /// 再删除配置
        /// 最后删除界面
        /// </summary>
        private async Task DeleteGatewayAsync(
            object? parameter)
        {

            if (parameter is not GatewayViewModel gatewayVM)
            {

                logService.Warn(
                    "删除失败:参数错误"
                );

                return;

            }



            string id =
                gatewayVM.Setting.Id;



            // 只有运行资源释放成功后，才继续删除配置和界面状态。
            // GatewayService异常会传播到AsyncRelayCommand边界。
            await gatewayService.RemoveGateway(
                id
            );



            configManager.RemoveGateway(
                id
            );



            Gateways.Remove(
                gatewayVM
            );


            if (ReferenceEquals(
                SelectedGateway,
                gatewayVM
            ))
            {
                SelectedGateway =
                    Gateways.FirstOrDefault();
            }



            logService.Info(
                $"删除网关:{id}"
            );

        }






        /*
         * 如果下层异步操作的完成结果，会影响上层后续逻辑，
         * 那么上层必须 await 它，否则会产生时序问题（可能出现竞态）。
         */
        /// <summary>
        /// 删除选中网关
        /// </summary>
        /// <summary>
        /// 删除所有选中的网关
        ///
        /// 流程:
        ///
        /// 找到选中网关
        ///
        /// ↓
        ///
        /// 释放运行资源(TCP连接)
        ///
        /// ↓
        ///
        /// 删除配置
        ///
        /// ↓
        ///
        /// 删除界面显示
        /// </summary>
        private async Task DeleteSelectedGatewayAsync(
            object? parameter)
        {

            var removeList =
                Gateways
                .Where(x => x.IsSelected)
                .ToList();



            if (removeList.Count == 0)
            {

                logService.Warn(
                    "没有选择网关"
                );

                return;

            }



            foreach (GatewayViewModel gatewayVM
                in removeList)
            {

                string id =
                    gatewayVM.Setting.Id;



                try
                {

                    // 先释放通信资源
                    //
                    // 等待TCP断开完成
                    await gatewayService.RemoveGateway(
                        id
                    );



                    // 删除配置文件数据
                    configManager.RemoveGateway(
                        id
                    );



                    // 删除界面数据
                    Gateways.Remove(
                        gatewayVM
                    );



                    logService.Info(
                        $"删除网关:{id}"
                    );

                }
                catch (Exception ex)
                {

                    logService.Error(
                        $"删除网关失败:{id} {ex.Message}"
                    );

                }

            }


            if (SelectedGateway == null
                || !Gateways.Contains(SelectedGateway))
            {
                SelectedGateway =
                    Gateways.FirstOrDefault();
            }



            logService.Info(
                $"批量删除完成:{removeList.Count}"
            );

        }




        private void HandleCommandException(
            string operationName,
            Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                logService.Warn(
                    $"{operationName}已取消:{exception.Message}"
                );

                return;
            }


            logService.Error(
                $"{operationName}失败:{exception.Message}"
            );
        }

    }

}
