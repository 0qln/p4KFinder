using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p4KFinder
{
    internal static class Extension
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            foreach(var item in values)
            {
                set.Add(item);
            }
        }
    }
}
