using System;
using System.Collections.Generic;
using System.Text;

namespace PlantMaster.Models
{

    /// <summary>
    /// 网关配置
    ///
    /// 表示一个网关的静态信息
    ///
    /// 用于：
    /// 1.XML保存
    /// 2.配置读取
    ///
    /// 不负责：
    /// 1.连接
    /// 2.通信
    /// 3.运行状态
    /// </summary>
    public class GatewaySetting
    {

        /// <summary>
        /// 网关编号
        /// </summary>
        public string Id
        {
            get;
            set;
        }


        /// <summary>
        /// 网关名称
        /// </summary>
        public string Name
        {
            get;
            set;
        }


        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress
        {
            get;
            set;
        }


        /// <summary>
        /// TCP端口
        /// </summary>
        public int Port
        {
            get;
            set;
        }

    }
}