using System.Runtime.InteropServices;

namespace AsarSharp.Utils
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymLinkFlag dwFlags);

        public enum SymLinkFlag
        {
            File = 0,
            Directory = 1,
            AllowUnprivilegedCreate = 2
        }
    }
}