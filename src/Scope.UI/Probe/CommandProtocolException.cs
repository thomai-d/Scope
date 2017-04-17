using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMD.Extensions;

namespace Scope.Interface.Probe
{
    public class CommandProtocolException : Exception
    {
        public CommandProtocolException(string step, string message) : base($"[{step}]: {message}")
        {
        }

        public CommandProtocolException(string step, string message, byte[] byteDump) : base($"[{step}]: {message}\n\n{byteDump.Dump()}") 
        {
        }
    }
}
