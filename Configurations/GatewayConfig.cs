using PlantMaster.Models;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PlantMaster.Configurations
{
    /// <summary>
    /// 网关配置模型
    ///
    /// 用于对应 Gateway.xml 文件结构
    ///
    /// XML结构：
    ///
    /// <Gateways>
    ///
    ///     <Gateway>
    ///         <Id>GW001</Id>
    ///         <Name>xxx</Name>
    ///         <IpAddress>192.168.1.100</IpAddress>
    ///         <Port>502</Port>
    ///     </Gateway>
    ///
    /// </Gateways>
    ///
    ///
    /// 注意：
    /// 这个类只负责描述XML结构
    ///
    /// 不负责：
    /// 1.读取文件
    /// 2.保存文件
    /// 3.连接网关
    ///
    /// 这些由ConfigService负责
    /// </summary>
    [XmlRoot("Gateways")]
    public class GatewayConfig
    {


        /// <summary>
        /// 网关集合
        ///
        /// 对应XML中的多个：
        ///
        /// <Gateway>
        /// 
        /// </Gateway>
        ///
        /// 反序列化后：
        ///
        /// List里面每一个元素就是一个Gateway对象
        /// </summary>
        [XmlElement("Gateway")]
        public List<GatewaySetting> Gateways
        {
            get;
            set;
        }


        /// <summary>
        /// 构造函数
        ///
        /// 初始化集合
        ///
        /// 防止使用时：
        /// config.Gateways.Add()
        ///
        /// 出现null异常
        /// </summary>
        public GatewayConfig()
        {
            Gateways =
                new List<GatewaySetting>();
        }


    }
}