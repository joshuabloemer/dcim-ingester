using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DcimIngester
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public static int GWL_EXSTYLE = -20;
        public static int WS_EX_TOOLWINDOW = 0x00000080;


        [DllImport("shell32.dll")]
        public static extern uint SHChangeNotifyRegister(IntPtr hWnd, int fSources, int fEvents, uint wMsg, int cEntries, ref SHChangeNotifyEntry pShcne);

        [DllImport("shell32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SHChangeNotifyDeregister(uint ulId);

        [StructLayout(LayoutKind.Sequential)]
        public struct SHChangeNotifyEntry
        {
            public IntPtr pIdl;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fRecursive;
        }

        // Does not refer to an existing Win32 structure
        [StructLayout(LayoutKind.Sequential)]
        public struct SHNotifyWParam
        {
            public IntPtr dwItem1;
            public IntPtr dwItem2;
        }

        public static int SHCNF_IDLIST = 0x0000;
        public static int SHCNF_TYPE = 0x00FF;

        public static int SHCNE_DRIVEADD = 0x00000100;
        public static int SHCNE_DRIVEREMOVED = 0x00000080;
        public static int SHCNE_MEDIAINSERTED = 0x00000020;
        public static int SHCNE_MEDIAREMOVED = 0x00000040;


        [DllImport("shell32.dll")]
        public static extern int SHGetSpecialFolderLocation(IntPtr hWnd, int csidl, out IntPtr pPidl);

        public static int CSIDL_DESKTOP = 0x0000;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SHGetPathFromIDListW(IntPtr pidl, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder pszPath);
    }
}
