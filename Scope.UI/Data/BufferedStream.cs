using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scope.Data
{
    public class BufferedStream<T> : IBufferedStream<T>
    {
        private readonly T[] data;
        private int idx;

        public BufferedStream(int size)
        {
            this.data = new T[size];
            this.Size = size;
        }

        public int Size { get; }

        public void Push(params T[] items)
        {
            lock (this.data)
            {
                foreach (var item in items)
                {
                    this.data[this.idx++] = item;
                    if (this.idx == this.data.Length)
                        this.idx = 0;
                }
            }
        }

        public T[] Last(int items)
        {
            if (items > data.Length)
                throw new InvalidOperationException($"Requested {items} items, but buffer size is only {this.data.Length}");

            var result = new T[items];

            lock (this.data)
            {
                var i = this.idx;
                for (var n = 0; n < items; n++)
                {
                    if (i-- == 0)
                        i = data.Length - 1;

                    result[n] = this.data[i];
                }
            }

            return result;
        }

        public void Clear()
        {
            for (int n = 0; n < data.Length; n++)
                data[n] = default(T);
        }
    }
}
