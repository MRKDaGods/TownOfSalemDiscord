using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownOfSalemDiscord
{
    public static class Extensions
    {
        static Dictionary<object, ulong> ms_InstanceTables = new Dictionary<object, ulong>();
        static ulong ms_Global;

        public static ulong InstanceId(this object obj)
        {
            if (ms_InstanceTables.ContainsKey(obj))
                return ms_InstanceTables[obj];
            ulong longx = ms_Global++;
            ms_InstanceTables[obj] = longx;
            return longx;
        }
        public static void CopyTo<T>(this List<T> l, List<T> target)
        {
            target.Clear();
            foreach (T t in l)
                target.Add(t);
        }
    }
}
