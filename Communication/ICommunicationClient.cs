using System;
using System.Threading;
using System.Threading.Tasks;


namespace PlantMaster.Communication
{
    /// <summary>
    /// 通信客户端接口。
    ///
    /// 只定义传输能力，不包含具体PLC协议。
    /// 完整请求响应事务由GatewayService进行串行调度。
    /// </summary>
    public interface ICommunicationClient : IDisposable
    {
        Task ConnectAsync(
            CancellationToken cancellationToken = default);

        Task DisconnectAsync();

        Task SendAsync(
            byte[] request,
            CancellationToken cancellationToken = default);

        Task<byte[]> ReceiveAsync(
            CancellationToken cancellationToken = default);
    }
}
