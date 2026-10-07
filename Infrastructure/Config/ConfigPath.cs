namespace PlantMaster.Infrastructure.Config
{

    /// <summary>
    /// 配置文件路径管理
    ///
    /// 作用：
    /// 统一管理程序所有配置文件路径
    ///
    /// 负责：
    /// 1.提供配置文件位置
    /// 2.避免路径散落在各个类中
    ///
    /// 不负责：
    /// 1.读取文件
    /// 2.保存文件
    /// 3.解析XML/JSON
    ///
    /// </summary>
    public static class ConfigPath
    {


        /// <summary>
        /// 配置文件根目录
        ///
        /// 当前默认：
        /// 程序目录下 Config 文件夹
        ///
        /// 示例：
        ///
        /// PlantMaster.exe
        ///
        /// Config
        ///     ├── Gateway.xml
        ///     ├── Plc.xml
        ///     └── Agv.xml
        ///
        /// </summary>
        public static string ConfigDirectory
        {
            get
            {
                return "Config";
            }
        }



        /// <summary>
        /// 网关配置文件路径
        ///
        /// 对应：
        /// Gateway.xml
        ///
        /// 保存：
        /// 网关IP
        /// 端口
        /// 通信参数
        ///
        /// </summary>
        public static string Gateway
        {
            get
            {
                return
                    $"{ConfigDirectory}/Gateway.xml";
            }
        }



        /// <summary>
        /// PLC配置文件路径
        ///
        /// 对应：
        /// Plc.xml
        ///
        /// 保存：
        /// PLC型号
        /// 地址
        /// 驱动类型
        ///
        /// </summary>
        public static string Plc
        {
            get
            {
                return
                    $"{ConfigDirectory}/Plc.xml";
            }
        }



        /// <summary>
        /// AGV配置文件路径
        ///
        /// 对应：
        /// Agv.xml
        ///
        /// 保存：
        /// AGV编号
        /// IP
        /// 任务参数
        ///
        /// </summary>
        public static string Agv
        {
            get
            {
                return
                    $"{ConfigDirectory}/Agv.xml";
            }
        }


    }

}