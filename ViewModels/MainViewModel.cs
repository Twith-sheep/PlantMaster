using PlantMaster.Common;
using PlantMaster.Infrastructure.Config;
using PlantMaster.Models;
using PlantMaster.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;


namespace PlantMaster.ViewModels
{

    /// <summary>
    /// 主窗口ViewModel
    ///
    /// 负责界面数据和命令
    /// 不负责配置解析和通信
    /// </summary>
    public class MainViewModel
    {


        //日志服务
        private readonly LogService logService;


        //配置管理
        private readonly ConfigManager configManager;


        //网关运行管理
        private readonly GatewayService gatewayService;


        //文件选择
        private readonly FileDialogService fileDialogService;




        /// <summary>
        /// 日志显示集合
        /// </summary>
        public ObservableCollection<LogItem> Logs
        {
            get
            {
                return logService.Logs;
            }
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



            Gateways =
                new ObservableCollection<GatewayViewModel>();



            ImportGatewayCommand =
                new RelayCommand(
                    ImportGateway
                );


            ConnectGatewayCommand =
                new RelayCommand(
                    ConnectGateway
                );


            DisconnectGatewayCommand =
                new RelayCommand(
                    DisconnectGateway
                );


            DeleteGatewayCommand =
                new RelayCommand(
                    DeleteGateway
                );


            DeleteSelectedGatewayCommand =
                new RelayCommand(
                    DeleteSelectedGateway
                );



            LoadGateways();



            logService.Info(
                "主界面初始化完成"
            );

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
                    new GatewayViewModel(setting)
                );

            }



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

            Gateways.Clear();



            foreach (GatewaySetting setting
                in configManager.Gateways)
            {

                Gateways.Add(
                    new GatewayViewModel(setting)
                );

            }



            logService.Info(
                $"刷新网关完成:{Gateways.Count}"
            );

        }








        /// <summary>
        /// 连接选中的网关
        ///
        /// 连接逻辑交给GatewayService
        /// </summary>
        private async void ConnectGateway()
        {

            foreach (GatewayViewModel gatewayVM
                in Gateways)
            {

                if (!gatewayVM.IsSelected)
                {
                    continue;
                }

                //这里没管道啊
                // 创建运行对象和通信管道
                gatewayService.AddGateway(
                    gatewayVM.Setting
                );


                await gatewayService.ConnectAsync(
                    gatewayVM.Setting.Id
                );

            }

        }







        /// <summary>
        /// 断开选中的网关
        /// </summary>
        private void DisconnectGateway()
        {

            foreach (GatewayViewModel gatewayVM
                in Gateways)
            {

                if (!gatewayVM.IsSelected)
                {
                    continue;
                }



                gatewayService.DisconnectAsync(
                    gatewayVM.Setting.Id
                );

            }

        }







        /// <summary>
        /// 删除单个网关
        ///
        /// 先释放运行资源
        /// 再删除配置
        /// 最后删除界面
        /// </summary>
        private async void DeleteGateway(
    object parameter)
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



            try
            {

                //先释放运行资源
                await gatewayService.RemoveGateway(
                    id
                );



                //删除配置
                configManager.RemoveGateway(
                    id
                );



                //删除界面数据
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
                    $"删除失败:{ex.Message}"
                );

            }

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
        private async void DeleteSelectedGateway(
            object parameter)
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



            logService.Info(
                $"批量删除完成:{removeList.Count}"
            );

        }

    }

}