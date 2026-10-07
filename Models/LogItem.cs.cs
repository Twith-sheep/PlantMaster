using PlantMaster.Common;
using System;

namespace PlantMaster.Models
{
    /// <summary>
    /// 日志数据模型
    ///
    /// 保存一条日志的信息
    /// 用于界面显示和文件保存
    /// </summary>
    public class LogItem : ObservableObject
    {


        /// <summary>
        /// 日志产生时间
        ///
        /// 记录事件发生的时间
        /// </summary>
        public DateTime Time { get; set; }



        /// <summary>
        /// 日志等级
        ///
        /// 例如：
        /// INFO  普通信息
        /// WARN  警告
        /// ERROR 错误
        /// </summary>
        public string Level { get; set; }



        /// <summary>
        /// 日志内容
        ///
        /// 记录具体事件
        /// 例如：
        /// 网关连接成功
        /// 读取D100=25
        /// </summary>
        public string Message { get; set; }


    }
}