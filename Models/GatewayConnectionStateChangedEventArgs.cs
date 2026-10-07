using System;

namespace PlantMaster.Models
{
    /// <summary>
    /// 网关连接状态变化事件参数。
    /// </summary>
    public sealed class GatewayConnectionStateChangedEventArgs : EventArgs
    {
        public string GatewayId { get; }

        public bool IsOnline { get; }

        public GatewayConnectionStateChangedEventArgs(
            string gatewayId,
            bool isOnline)
        {
            GatewayId = gatewayId;
            IsOnline = isOnline;
        }
    }
}
