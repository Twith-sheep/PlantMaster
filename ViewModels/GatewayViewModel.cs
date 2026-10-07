using PlantMaster.Models;


namespace PlantMaster.ViewModels
{

    /// <summary>
    /// 网关界面模型
    ///
    /// 负责：
    /// 1.提供配置显示
    /// 2.保存界面选择状态
    ///
    /// 不负责：
    /// TCP连接
    /// </summary>
    public class GatewayViewModel
    {


        /// <summary>
        /// 网关配置
        ///
        /// 来自ConfigManager
        /// </summary>
        public GatewaySetting Setting
        {
            get;
        }



        /// <summary>
        /// 是否选择
        ///
        /// 给CheckBox绑定
        /// </summary>
        public bool IsSelected
        {
            get;
            set;
        }



        /// <summary>
        /// 构造
        ///
        /// 接收配置对象
        /// 不创建新的配置
        /// </summary>
        public GatewayViewModel(
            GatewaySetting setting)
        {

            Setting =
                setting;

        }


    }

}