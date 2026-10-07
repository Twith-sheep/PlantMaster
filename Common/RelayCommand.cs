using System;
using System.Windows.Input;

namespace PlantMaster.Common
{
    /// <summary>
    /// 通用命令类
    ///
    /// 用于连接：
    ///
    /// Button
    ///      |
    ///      ↓
    /// ViewModel方法
    ///
    /// 避免每个按钮都写Click事件
    /// </summary>
    public class RelayCommand : ICommand
    {


        /// <summary>
        /// 要执行的方法
        /// </summary>
        private readonly Action execute;

        private readonly Action<object> executeWithParameter;




        /// <summary>
        /// 判断按钮是否可以点击
        ///
        /// 例如：
        /// 没连接时禁止断开按钮
        /// </summary>
        private readonly Func<bool> canExecute;




        /// <summary>
        /// 无参构造函数
        ///
        /// execute:
        /// 点击后执行什么
        ///
        /// canExecute:
        /// 是否允许执行
        /// </summary>
        public RelayCommand(
            Action execute,
            Func<bool> canExecute = null)
        {

            this.execute = execute;

            this.canExecute = canExecute;

        }

        /// <summary>
        /// 有参构造函数
        ///
        /// execute:
        /// 点击后执行什么
        ///
        /// canExecute:
        /// 是否允许执行
        /// </summary>
        public RelayCommand(
            Action<object> executeWithParameter,
            Func<bool> canExecute = null)
        {

            this.executeWithParameter = executeWithParameter;

            this.canExecute = canExecute;

        }



        /// <summary>
        /// 判断当前能不能执行
        /// </summary>
        public bool CanExecute(object parameter)
        {

            if (canExecute == null)
            {
                return true;
            }


            return canExecute();

        }





        /// <summary>
        /// WPF按钮最终调用这里
        /// </summary>
        public void Execute(object parameter)
        {

            if (executeWithParameter != null)
            {
                executeWithParameter(parameter);
            }
            else
            {
                execute();
            }

        }


        /// <summary>
        /// 通知界面刷新按钮状态
        ///
        /// 例如：
        /// 连接成功后
        /// 让断开按钮可用
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }


    }
}