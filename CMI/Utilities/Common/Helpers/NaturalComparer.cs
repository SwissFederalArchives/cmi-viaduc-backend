using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace CMI.Utilities.Common.Helpers
{
    public class NaturalComparer : IComparer<string>, IComparer
    {
        public static NaturalComparer Instance { get; } = new NaturalComparer();

        public int Compare(object x, object y)
        {
            return Compare(x.ToString(), y.ToString());
        }

        public int Compare(string x, string y)
        {
            if (x == null)
            {
                return y == null ? 0 : -1;
            }

            return y == null ? 1 : SafeNativeMethods.StrCmpLogicalW(x, y);
        }

        public static int NaturalCompare(string x, string y)
        {
            if (x == null)
            {
                return y == null ? 0 : -1;
            }

            return y == null ? 1 : SafeNativeMethods.StrCmpLogicalW(x, y);
        }

        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }
    }
}