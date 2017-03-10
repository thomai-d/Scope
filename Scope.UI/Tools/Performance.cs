using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scope.Tools
{
    public static class Performance
    {
        public static IDisposable Trace(string name)
        {
            return new PerformanceTrace(name);
        }
    }

    public class PerformanceTrace : IDisposable
    {
        private readonly Stopwatch watch;
        private readonly string name;

        public PerformanceTrace(string name)
        {
            this.watch = Stopwatch.StartNew();
            this.name = name;
        }

        public void Dispose()
        {
            this.watch.Stop();
            Debug.WriteLine($"{name} took {this.watch.ElapsedMilliseconds}ms");
        }
    }
}
