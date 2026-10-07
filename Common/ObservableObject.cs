using System.ComponentModel;

namespace PlantMaster.Common
{
    /// <summary>
    /// 可观察对象基类
    ///
    /// 实现 INotifyPropertyChanged
    ///
    /// 用于：
    /// 1. ViewModel通知界面刷新
    /// 2. Model属性变化通知
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {


        /// <summary>
        /// 属性变化事件
        ///
        /// 当属性值改变时通知WPF更新
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;



        /// <summary>
        /// 通知属性发生变化
        ///
        /// 参数：
        /// propertyName:
        /// 发生变化的属性名称
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }


    }
}