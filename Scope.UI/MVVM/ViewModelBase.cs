using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Scope.MVVM
{
    public class ViewModelBase : NPCBase
    {
        public virtual void OnViewLoaded()
        {
        }

        public virtual void OnViewUnloaded()
        {
        }
    }
}
