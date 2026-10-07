using PlantMaster.Communication;


namespace PlantMaster.Models
{

    /// <summary>
    /// 网关运行状态
    ///
    /// 保存：
    /// 1.当前网关配置
    /// 2.通信对象
    /// 3.在线状态
    ///
    /// 不保存：
    /// XML配置
    /// </summary>
    public class GatewayRuntime
    {


        /// <summary>
        /// 网关配置
        ///
        /// 用于知道：
        /// IP
        /// 端口
        /// Id
        /// </summary>
        public GatewaySetting Setting
        {
            get;
        }



        /// <summary>
        /// 通信客户端
        ///
        /// 负责TCP通信
        /// </summary>
        public ICommunicationClient Client
        {
            get;
        }



        /// <summary>
        /// 当前连接状态
        /// </summary>
        public bool IsOnline
        {
            get;
            set;
        }



        /// <summary>
        /// 创建运行对象
        ///
        /// 配置 + 通信对象
        /// 组成一个运行中的网关
        /// </summary>
        public GatewayRuntime(
            GatewaySetting setting,
            ICommunicationClient client)
        {

            Setting =
                setting;


            Client =
                client;


            IsOnline =
                false;

        }

    }

}