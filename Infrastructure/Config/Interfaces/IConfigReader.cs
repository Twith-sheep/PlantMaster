namespace PlantMaster.Infrastructure.Config.Interfaces
{
    /// <summary>
    /// 配置读取器接口
    ///
    /// 作用：
    /// 定义所有配置读取方式的统一规范
    ///
    /// 例如：
    ///
    /// XML配置
    /// JSON配置
    /// 数据库存储配置
    ///
    /// 都需要实现这个接口
    ///
    /// 注意：
    /// 这里只定义规则
    /// 不负责具体读取
    /// </summary>
    public interface IConfigReader
    {


        /// <summary>
        /// 读取配置
        ///
        /// 文件
        /// ↓
        /// C#对象
        ///
        /// 示例：
        ///
        /// Gateway.xml
        /// ↓
        /// GatewayConfig
        ///
        /// Plc.json
        /// ↓
        /// PlcConfig
        /// </summary>
        /// 
        /// <typeparam name="T">
        /// 配置对象类型
        ///
        /// 例如：
        /// GatewayConfig
        /// PlcConfig
        /// AgvConfig
        /// </typeparam>
        /// 
        /// <param name="path">
        /// 配置文件路径
        ///
        /// 例如：
        /// Config/Gateway.xml
        /// </param>
        /// 
        /// <returns>
        /// 转换后的配置对象
        /// </returns>
        T Read<T>(string path);



        /// <summary>
        /// 保存配置
        ///
        /// C#对象
        /// ↓
        /// 配置文件
        ///
        /// 示例：
        ///
        /// GatewayConfig
        /// ↓
        /// Gateway.xml
        /// </summary>
        /// 
        /// <typeparam name="T">
        /// 配置对象类型
        /// </typeparam>
        /// 
        /// <param name="path">
        /// 保存路径
        /// </param>
        /// 
        /// <param name="data">
        /// 要保存的数据对象
        /// </param>
        void Save<T>(
            string path,
            T data);


    }
}