using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scope.Interface.Probe
{
    public abstract class CommandBase
    {
        protected readonly ProbeConnection Probe;

        protected CommandBase(ProbeConnection probe)
        {
            this.Probe = probe;
        }

        public abstract Task Execute();

        public virtual void Cancel()
        {
            throw new NotSupportedException();
        }
    }
}
