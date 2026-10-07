using Microsoft.Win32;

namespace PlantMaster.Services
{

    /// <summary>
    /// 文件选择服务
    ///
    /// 负责：
    /// 1. 打开文件选择窗口
    /// 2. 获取用户选择的文件路径
    ///
    /// 不负责：
    /// 1. 文件读取
    /// 2. XML解析
    /// 3. TXT解析
    /// 4. 配置业务 
    /// </summary>
    public class FileDialogService
    {


        /// <summary>
        /// 打开文件选择窗口
        ///
        /// 返回：
        /// 用户选择的文件完整路径
        ///
        /// 如果用户取消：
        /// 返回null
        /// </summary>
        public string OpenFile()
        {


            OpenFileDialog dialog =
                new OpenFileDialog();



            // 文件过滤
            //
            // 目前支持：
            // xml
            // txt
            //
            // 后续可以增加
            // json等
            dialog.Filter =
                "配置文件|*.xml;*.txt|所有文件|*.*";



            bool? result =
                dialog.ShowDialog();



            if (result == true)
            {

                return dialog.FileName;

            }



            return null;

        }


    }

}