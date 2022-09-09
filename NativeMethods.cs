using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DcimIngester
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr _GetWindowLongPtr(IntPtr hWnd, int nIndex);

        public static int GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return _GetWindowLongPtr(hWnd, nIndex).ToInt32();
            else return GetWindowLong(hWnd, nIndex);
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr _SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static int SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            if (IntPtr.Size == 8)
                return _SetWindowLongPtr(hWnd, nIndex, new IntPtr(dwNewLong)).ToInt32();
            else return SetWindowLong(hWnd, nIndex, dwNewLong);
        }

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;


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

        [StructLayout(LayoutKind.Sequential)]
        public struct SHNotifyStruct
        {
            public IntPtr dwItem1;
            public IntPtr dwItem2;
        }

        public const int SHCNF_IDLIST = 0x0000;
        public const int SHCNF_TYPE = 0x00FF;

        public const int SHCNE_DRIVEADD = 0x00000100;
        public const int SHCNE_DRIVEREMOVED = 0x00000080;
        public const int SHCNE_MEDIAINSERTED = 0x00000020;
        public const int SHCNE_MEDIAREMOVED = 0x00000040;


        [DllImport("shell32.dll")]
        public static extern int SHGetKnownFolderIDList([MarshalAs(UnmanagedType.LPStruct)] Guid rFid, uint dwFlags, IntPtr hToken, out IntPtr pPidl);

        public static readonly Guid FOLDERID_DESKTOP = new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SHGetPathFromIDListW(IntPtr pidl, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder pszPath);
    }
}
