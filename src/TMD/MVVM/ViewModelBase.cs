using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TMD.Extensions;

namespace TMD.MVVM
{
    public class ViewModelBase : NotifyPropertyChanged
    {
        public event EventHandler CloseRequested;

        private RelayCommand[] commands = { };

        public RelayCommand CloseCommand { get; }

        public ViewModelBase()
        {
            this.CloseCommand = new RelayCommand(() => this.OnCloseRequested());
        }

        public virtual void OnViewLoaded()
        {
            // Initialize commands for InvalidateCommands()
            this.commands = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.GetValue(this) as RelayCommand)
                .Where(p => p != null)
                .ToArray();
        }

        public virtual void OnViewUnloaded()
        {
        }

        protected void OnCloseRequested()
        {
            this.CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        protected void InvalidateCommands()
        {
            this.commands.ForEach(c => c.ReEvaluate());
        }
    }
}
