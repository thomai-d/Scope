using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TMD.MVVM
{
    public class RelayCommand<T> : ICommand
    {
        public RelayCommand(Action<T> executeAction)
            : this()
        {
            if (executeAction == null)
                throw new ArgumentNullException("executeAction");
            this.ExecuteAction = executeAction;
        }

        public RelayCommand(Action<T> executeAction, Func<T, bool> canExecuteFunc)
        {
            if (executeAction == null)
                throw new ArgumentNullException("executeAction");
            if (canExecuteFunc == null)
                throw new ArgumentNullException("canExecuteFunc");
            this.ExecuteAction = executeAction;
            this.CanExecuteFunc = canExecuteFunc;
        }

        protected RelayCommand()
        {
            this.ExecuteAction = p => { };
            this.CanExecuteFunc = p => true;
        }

        public event EventHandler CanExecuteChanged;

        protected Action<T> ExecuteAction { get; set; }

        protected Func<T, bool> CanExecuteFunc { get; set; }

        public virtual bool CanExecute(object parameter)
        {
            if (parameter is T)
                return this.CanExecuteFunc((T)parameter);
            return this.CanExecuteFunc(default(T));
        }

        public virtual void Execute(object parameter)
        {
            if (this.CanExecute(parameter))
            {
                var param = parameter is T
                    ? (T)parameter
                    : default(T);

                this.ExecuteAction(param);
            }
        }

        public void ReEvaluate()
        {
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action executeAction)
            : base(x => executeAction())
        {
        }

        public RelayCommand(Action executeAction, Func<bool> canExecuteFunc)
            : base(x => executeAction(), x => canExecuteFunc())
        {
        }
    }
}