using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WeModPatcher.ReactiveUICore
{
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;

        private long _isExecuting;

        public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute = null)
        {
            this._execute = execute;
            this._canExecute = canExecute ?? (o => true);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        private static void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
        
        public bool CanExecute(object parameter) => Interlocked.Read(ref _isExecuting) == 0 && _canExecute(parameter);

        public async void Execute(object parameter)
        {
            Interlocked.Exchange(ref _isExecuting, 1);
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter);
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }
    }
}