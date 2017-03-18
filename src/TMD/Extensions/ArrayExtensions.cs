using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMD.Extensions
{
    public static class ArrayExtensions
    {
        public static bool EndsWith<T>(this T[] arr, T[] exp)
        {
            if (arr.Length < exp.Length)
                return false;

            for (int n = 1; n <= exp.Length; n++)
            {
                if (!arr[arr.Length - n].Equals(exp[exp.Length - n]))
                    return false;
            }

            return true;
        }

        public static string Dump(this byte[] arr)
        {
            var str = new StringBuilder();
            for (int n = 0; n < arr.Length; n++)
            {
                str.Append(arr[n].ToString("x2"));
                if (n < arr.Length)
                {
                    if (n % 8 == 7)
                        str.AppendLine();
                    else
                        str.Append(" ");
                }
            }
            return str.ToString();
        }
    }
}
