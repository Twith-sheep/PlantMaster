using PlantMaster.Services;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PlantMaster.Communication
{
    /// <summary>
    /// TCP通信客户端
    ///
    /// 实现 ICommunicationClient
    ///
    /// 负责：
    /// 1. TCP连接
    /// 2. 数据发送
    /// 3. 数据接收
    ///
    /// 不负责：
    /// 1. PLC协议解析
    /// 2. 网关业务管理
    /// </summary>
    public class TcpCommunicationClient : ICommunicationClient
    {


        /// <summary>
        /// TCP客户端
        ///
        /// 负责底层Socket通信
        /// </summary>
        private TcpClient? tcpClient;



        /// <summary>
        /// TCP数据管道
        ///
        /// 通过 TcpClient.GetStream()
        /// 获取
        ///
        /// 用于发送和接收数据
        /// </summary>
        private NetworkStream? networkStream;



        /// <summary>
        /// 网关IP地址
        /// </summary>
        private readonly string ipAddress;



        /// <summary>
        /// 通信端口
        /// </summary>
        private readonly int port;



        /// <summary>
        /// 日志服务
        ///
        /// 记录：
        /// 连接成功
        /// 连接失败
        /// 通信异常
        /// </summary>
        private readonly LogService logService;



        /// <summary>
        /// 构造函数
        ///
        /// 保存通信参数
        ///
        /// 不立即连接
        /// </summary>
        public TcpCommunicationClient(
            string ipAddress,
            int port,
            LogService logService)
        {

            this.ipAddress = ipAddress;

            this.port = port;

            this.logService = logService;

        }



        /// <summary>
        /// 建立TCP连接
        ///
        /// 失败：
        /// 记录日志
        /// 不影响程序继续运行
        /// </summary>
        public async Task ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                TcpClient newClient =
                    new TcpClient();

                tcpClient = newClient;

                await newClient.ConnectAsync(
                    ipAddress,
                    port,
                    cancellationToken
                );

                networkStream =
                    newClient.GetStream();

                logService.Info(
                    $"TCP连接成功 {ipAddress}:{port}"
                );
            }
            catch (Exception ex)
            {
                networkStream?.Dispose();
                tcpClient?.Dispose();
                networkStream = null;
                tcpClient = null;

                logService.Error(
                    $"TCP连接失败 {ipAddress}:{port} {ex.Message}"
                );

                throw;
            }

        }




        /// <summary>
        /// 判断TCP是否连接
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return tcpClient != null
                    && tcpClient.Connected;
            }
        }




        /// <summary>
        /// 断开TCP连接
        /// </summary>
        public async Task DisconnectAsync()
        {

            try
            {

                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream = null;
                }


                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }


                logService.Info(
                    $"TCP断开 {ipAddress}:{port}"
                );

            }
            catch (Exception ex)
            {

                logService.Error(
                    $"TCP断开失败 {ex.Message}"
                );

            }


            await Task.CompletedTask;

        }





        /// <summary>
        /// 发送数据
        ///
        /// byte[]:
        /// 原始报文
        ///
        /// 没有连接：
        /// 直接取消发送
        /// </summary>
        public async Task SendAsync(
            byte[] request,
            CancellationToken cancellationToken = default)
        {

            if (!IsConnected)
            {

                logService.Warn(
                    "TCP未连接，取消发送"
                );


                throw new InvalidOperationException(
                    "TCP未连接，无法发送数据"
                );

            }



            try
            {

                await networkStream!.WriteAsync(
                    request.AsMemory(),
                    cancellationToken
                );

            }
            catch (Exception ex)
            {

                logService.Error(
                    $"TCP发送失败 {ex.Message}"
                );

                throw;

            }

        }




        /// <summary>
        /// 接收数据
        ///
        /// 返回设备回复报文
        ///
        /// 未连接返回null
        /// </summary>
        public async Task<byte[]> ReceiveAsync(
            CancellationToken cancellationToken = default)
        {

            if (!IsConnected)
            {

                logService.Warn(
                    "TCP未连接，取消接收"
                );


                throw new InvalidOperationException(
                    "TCP未连接，无法接收数据"
                );

            }



            try
            {

                byte[] buffer =
                    new byte[1024];



                int length =
                    await networkStream!.ReadAsync(
                        buffer.AsMemory(),
                        cancellationToken
                    );



                byte[] result =
                    new byte[length];



                Array.Copy(
                    buffer,
                    result,
                    length
                );



                return result;

            }
            catch (Exception ex)
            {

                logService.Error(
                    $"TCP接收失败 {ex.Message}"
                );


                throw;

            }

        }


    }
}
