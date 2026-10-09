using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace PlantMaster.Common
{
    /// <summary>
    /// 轻量异步命令。
    ///
    /// ICommand要求Execute返回void，因此异步边界集中在本类中。
    /// 所有异步异常都会交给调用方提供的异常处理器，
    /// 同一命令执行期间不会被重复触发。
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> execute;

        private readonly Predicate<object?>? canExecute;

        private readonly Action<Exception> exceptionHandler;

        private int isExecuting;


        public AsyncRelayCommand(
            Func<Task> execute,
            Action<Exception> exceptionHandler,
            Func<bool>? canExecute = null)
            : this(
                _ => execute(),
                exceptionHandler,
                canExecute == null
                    ? null
                    : _ => canExecute())
        {
            ArgumentNullException.ThrowIfNull(execute);
        }


        public AsyncRelayCommand(
            Func<object?, Task> execute,
            Action<Exception> exceptionHandler,
            Predicate<object?>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(execute);
            ArgumentNullException.ThrowIfNull(exceptionHandler);

            this.execute = execute;
            this.exceptionHandler = exceptionHandler;
            this.canExecute = canExecute;
        }


        public bool CanExecute(object? parameter)
        {
            return Volatile.Read(ref isExecuting) == 0
                && (canExecute?.Invoke(parameter) ?? true);
        }


        public async void Execute(object? parameter)
        {
            try
            {
                if (!CanExecute(parameter)
                    || Interlocked.CompareExchange(
                        ref isExecuting,
                        1,
                        0
                    ) != 0)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return;
            }


            RaiseCanExecuteChanged();


            try
            {
                await execute(parameter);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                Volatile.Write(
                    ref isExecuting,
                    0
                );

                RaiseCanExecuteChanged();
            }
        }


        private void HandleException(Exception exception)
        {
            try
            {
                exceptionHandler(exception);
            }
            catch (Exception handlerException)
            {
                // ICommand的void边界不能再次抛出异常，
                // 否则会进入WPF Dispatcher并导致程序退出。
                Debug.WriteLine(
                    $"异步命令异常处理器执行失败:{handlerException}"
                );
            }
        }


        private void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(
                this,
                EventArgs.Empty
            );

            CommandManager.InvalidateRequerySuggested();
        }


        public event EventHandler? CanExecuteChanged;
    }
}
