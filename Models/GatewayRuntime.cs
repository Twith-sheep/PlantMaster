using PlantMaster.Communication;
using System.Threading;


namespace PlantMaster.Models
{
    /// <summary>
    /// 网关运行状态。
    ///
    /// 每个运行对象拥有独立的通信锁，
    /// 保证同一网关的完整通信事务串行执行。
    /// </summary>
    public class GatewayRuntime
    {
        private int isStopping;


        /// <summary>
        /// 网关配置。
        /// </summary>
        public GatewaySetting Setting
        {
            get;
        }


        /// <summary>
        /// 通信客户端仅允许在程序集内部由网关服务调度。
        /// </summary>
        internal ICommunicationClient Client
        {
            get;
        }


        /// <summary>
        /// 每个网关独立的异步通信锁。
        /// </summary>
        internal SemaphoreSlim CommunicationSemaphore
        {
            get;
        }


        /// <summary>
        /// 当前连接状态。
        /// </summary>
        public bool IsOnline
        {
            get;
            internal set;
        }


        /// <summary>
        /// 网关是否正在删除或关闭。
        /// </summary>
        internal bool IsStopping
        {
            get
            {
                return Volatile.Read(ref isStopping) == 1;
            }
        }


        public GatewayRuntime(
            GatewaySetting setting,
            ICommunicationClient client)
        {
            Setting = setting;
            Client = client;
            CommunicationSemaphore = new SemaphoreSlim(1, 1);
            IsOnline = false;
        }


        /// <summary>
        /// 原子地将网关标记为停止状态。
        /// </summary>
        internal bool TryBeginStopping()
        {
            return Interlocked.CompareExchange(
                ref isStopping,
                1,
                0
            ) == 0;
        }
    }
}
