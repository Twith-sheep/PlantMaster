using System.Threading.Tasks;

namespace PlantMaster.Communication
{
    /// <summary>
    /// 通信客户端接口
    ///
    /// 定义所有通信方式必须具备的基本能力
    ///
    /// 不关心具体通信方式：
    ///
    /// TCP
    /// 串口
    /// RS485
    /// 无线
    ///
    /// 只规定：
    /// 连接
    /// 断开
    /// 发送
    /// 接收
    /// </summary>
    public interface ICommunicationClient
    {


        /// <summary>
        /// 建立通信连接
        ///
        /// 例如：
        /// TCP连接服务器
        /// 打开串口
        /// </summary>
         Task ConnectAsync();



        /// <summary>
        /// 断开通信连接
        ///
        /// 释放通信资源
        /// </summary>
        Task DisconnectAsync();



        /// <summary>
        /// 发送数据
        ///
        /// 参数：
        /// byte[] request
        ///
        /// 原始通信数据
        /// </summary>
        Task SendAsync(byte[] request);



        /// <summary>
        /// 接收数据
        ///
        /// 返回：
        /// 设备返回的原始数据
        /// </summary>
        Task<byte[]> ReceiveAsync();



    }
}