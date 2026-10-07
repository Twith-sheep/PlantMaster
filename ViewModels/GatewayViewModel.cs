using PlantMaster.Common;
using PlantMaster.Models;


namespace PlantMaster.ViewModels
{
    /// <summary>
    /// 网关界面模型。
    ///
    /// 负责：
    /// 1.提供配置显示
    /// 2.保存界面选择状态
    /// 3.投影网关在线状态
    ///
    /// 不负责TCP连接。
    /// </summary>
    public class GatewayViewModel : ObservableObject
    {
        private bool isSelected;

        private bool isOnline;


        /// <summary>
        /// 网关配置，来自ConfigManager。
        /// </summary>
        public GatewaySetting Setting
        {
            get;
        }


        /// <summary>
        /// 是否选择，供CheckBox绑定。
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }

            set
            {
                if (isSelected == value)
                {
                    return;
                }

                isSelected = value;

                OnPropertyChanged(
                    nameof(IsSelected)
                );
            }
        }


        /// <summary>
        /// 提供给界面绑定的网关在线状态。
        /// 真实状态由GatewayService管理，
        /// 此属性只是界面状态投影。
        /// </summary>
        public bool IsOnline
        {
            get
            {
                return isOnline;
            }

            private set
            {
                if (isOnline == value)
                {
                    return;
                }

                isOnline = value;

                OnPropertyChanged(
                    nameof(IsOnline)
                );
            }
        }


        public GatewayViewModel(
            GatewaySetting setting,
            bool isOnline = false)
        {
            Setting =
                setting;

            this.isOnline =
                isOnline;
        }


        /// <summary>
        /// 接收服务层发布的连接状态。
        /// </summary>
        internal void UpdateOnlineState(bool value)
        {
            IsOnline = value;
        }
    }
}
