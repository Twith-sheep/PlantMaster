using PlantMaster.Communication;
using PlantMaster.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace PlantMaster.Services
{

    /// <summary>
    /// 网关运行管理服务
    ///
    /// 负责：
    /// 1.管理运行中的网关
    /// 2.管理通信对象
    /// 3.连接和断开
    /// 4.更新运行状态
    ///
    /// 不负责：
    /// 1.配置文件
    /// 2.XML读取
    /// 3.界面显示
    /// </summary>
    public class GatewayService
    {


        /// <summary>
        /// 网关运行集合
        ///
        /// Key:
        /// 网关Id
        ///
        /// Value:
        /// 运行对象
        /// </summary>
        private readonly Dictionary<string, GatewayRuntime> runtimes;



        private readonly LogService logService;





        public GatewayService(
            LogService logService)
        {

            runtimes =
                new Dictionary<string, GatewayRuntime>();


            this.logService =
                logService;

        }







        /// <summary>
        /// 添加运行网关
        ///
        /// 根据配置创建运行对象
        ///
        /// GatewaySetting
        ///        |
        ///        ↓
        /// GatewayRuntime
        /// </summary>
        public void AddGateway(
            GatewaySetting setting)
        {

            if (setting == null)
            {
                return;
            }



            if (runtimes.ContainsKey(setting.Id))
            {

                logService.Warn(
                    $"网关已存在:{setting.Id}"
                );

                return;

            }




            ICommunicationClient client =
                new TcpCommunicationClient(
                    setting.IpAddress,
                    setting.Port,
                    logService
                );



            GatewayRuntime runtime =
                new GatewayRuntime(
                    setting,
                    client
                );



            runtimes.Add(
                setting.Id,
                runtime
            );



            logService.Info(
                $"创建网关运行对象:{setting.Id}"
            );

        }









        /// <summary>
        /// 获取运行网关
        /// </summary>
        private GatewayRuntime GetRuntime(
            string id)
        {

            if (runtimes.TryGetValue(
                id,
                out GatewayRuntime runtime))
            {

                return runtime;

            }


            return null;

        }









        /// <summary>
        /// 连接网关
        /// </summary>
        public async Task ConnectAsync(
            string id)
        {

            GatewayRuntime runtime =
                GetRuntime(id);



            if (runtime == null)
            {

                logService.Warn(
                    $"不存在运行网关:{id}"
                );

                return;

            }




            try
            {

                await runtime.Client
                    .ConnectAsync();



                runtime.IsOnline =
                    true;



                logService.Info(
                    $"网关连接成功:{id}"
                );

            }
            catch (Exception ex)
            {

                runtime.IsOnline =
                    false;


                logService.Error(
                    $"网关连接失败:{id} {ex.Message}"
                );

            }

        }








        /// <summary>
        /// 断开网关
        /// </summary>
        public async Task DisconnectAsync(
            string id)
        {


            GatewayRuntime runtime =
                GetRuntime(id);



            if (runtime == null)
            {
                return;
            }



            await runtime.Client
                .DisconnectAsync();



            runtime.IsOnline =
                false;



            logService.Info(
                $"网关断开:{id}"
            );

        }








        /// <summary>
        /// 删除网关运行对象
        ///
        /// 删除前释放通信资源
        /// </summary>
        public async Task RemoveGateway(
            string id)
        {

            GatewayRuntime runtime =
                GetRuntime(id);



            if (runtime == null)
            {
                return;
            }



            await runtime.Client
                .DisconnectAsync();



            runtimes.Remove(id);



            logService.Info(
                $"释放网关资源:{id}"
            );

        }


    }

}