using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TMD.MVVM
{
    public class ViewModelBase : NotifyPropertyChanged
    {
        public virtual void OnViewLoaded()
        {
        }

        public virtual void OnViewUnloaded()
        {
        }
    }
}
