using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scope.Data
{
    /// <summary>
    /// Interface for at stream of {T}.
    /// </summary>
    public interface IBufferedStream<T>
    {
        void Push(params T[] items);

        T[] Last(int items);

        int Size { get; }

        void Clear();
    }
}
