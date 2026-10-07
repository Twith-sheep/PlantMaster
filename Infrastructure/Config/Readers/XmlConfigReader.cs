using PlantMaster.Infrastructure.Config.Interfaces;
using System.IO;
using System.Xml.Serialization;

namespace PlantMaster.Infrastructure.Config.Readers
{

    /// <summary>
    /// XML配置读取器
    ///
    /// 负责：
    ///
    /// XML文件
    ///     ↕
    /// C#对象
    ///
    /// 例如：
    ///
    /// Gateway.xml
    ///     ↓
    /// GatewayConfig
    ///
    /// 不负责：
    ///
    /// 1.配置管理
    /// 2.业务逻辑
    /// 3.网关连接
    ///
    /// </summary>
    public class XmlConfigReader : IConfigReader
    {


        /// <summary>
        /// 读取XML配置
        ///
        /// 文件
        /// ↓
        /// 对象
        ///
        /// </summary>
        public T Read<T>(string path)
        {


            XmlSerializer serializer =
                new XmlSerializer(typeof(T));



            using (FileStream stream =
                new FileStream(
                    path,
                    FileMode.Open))
            {

                return (T)serializer.Deserialize(stream);

            }

        }





        /// <summary>
        /// 保存XML配置
        ///
        /// 对象
        /// ↓
        /// XML文件
        ///
        /// </summary>
        public void Save<T>(
            string path,
            T data)
        {


            XmlSerializer serializer =
                new XmlSerializer(typeof(T));



            string directory =
                Path.GetDirectoryName(path);



            if (!Directory.Exists(directory))
            {

                Directory.CreateDirectory(directory);

            }



            using (FileStream stream =
                new FileStream(
                    path,
                    FileMode.Create))
            {

                serializer.Serialize(
                    stream,
                    data);

            }


        }


    }

}