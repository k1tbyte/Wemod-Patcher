using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WeModPatcher.Utils.Win32
{
    public static class Imports
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WaitForDebugEvent(ref DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcess(int dwProcessId);
        
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModules(
            IntPtr hProcess,
            IntPtr lphModule,
            uint cb,
            out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetModuleFileNameEx(
            IntPtr hProcess,
            IntPtr hModule,
            StringBuilder lpFilename,
            int nSize);
        
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

        
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static  extern bool CreateProcessA
        (
            String lpApplicationName,
            String lpCommandLine,
            IntPtr lpProcessAttributes, 
            IntPtr lpThreadAttributes,
            Boolean bInheritHandles, 
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            [In] ref StartupInfo lpStartupInfo, 
            out ProcessInformation lpProcessInformation
        );
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ContinueDebugEvent(uint dwProcessId, uint dwThreadId, uint dwContinueStatus);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct StartupInfo
        {
            public Int32 cb              ;
            public IntPtr lpReserved     ;
            public IntPtr lpDesktop      ;
            public IntPtr lpTitle        ;
            public Int32 dwX             ;
            public Int32 dwY             ;
            public Int32 dwXSize         ;
            public Int32 dwYSize         ;
            public Int32 dwXCountChars   ;
            public Int32 dwYCountChars   ;
            public Int32 dwFillAttribute ;
            public Int32 dwFlags         ;
            public Int16 wShowWindow     ;
            public Int16 cbReserved2     ;
            public IntPtr lpReserved2    ;
            public IntPtr hStdInput      ;
            public IntPtr hStdOutput     ;
            public IntPtr hStdError      ;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessId;
            public Int32 dwThreadId;
        }

        #region Debug event structures
        
        [StructLayout(LayoutKind.Explicit)]
        public struct DEBUG_EVENT
        {
            [FieldOffset(0)]
            public uint dwDebugEventCode;
            [FieldOffset(4)]
            public uint dwProcessId;
            [FieldOffset(8)]
            public uint dwThreadId;
            [FieldOffset(16)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
            public byte[] Union;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct EXCEPTION_DEBUG_INFO
        {
            public EXCEPTION_RECORD ExceptionRecord;
            public uint dwFirstChance;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct EXCEPTION_RECORD
        {
            public uint ExceptionCode;
            public uint ExceptionFlags;
            public IntPtr pExceptionRecord;
            public IntPtr ExceptionAddress;
            public uint NumberParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public IntPtr[] ExceptionInformation;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct CREATE_THREAD_DEBUG_INFO
        {
            public IntPtr hThread;
            public IntPtr lpThreadLocalBase;
            public IntPtr lpStartAddress;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct CREATE_PROCESS_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr hProcess;
            public IntPtr hThread;
            public IntPtr lpBaseOfImage;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpThreadLocalBase;
            public IntPtr lpStartAddress;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct EXIT_THREAD_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_PROCESS_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOAD_DLL_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr lpBaseOfDll;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNLOAD_DLL_DEBUG_INFO
        {
            public IntPtr lpBaseOfDll;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OUTPUT_DEBUG_STRING_INFO
        {
            public IntPtr lpDebugStringData;
            public ushort fUnicode;
            public ushort nDebugStringLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RIP_INFO
        {
            public uint dwError;
            public uint dwType;
        }
        
        
        public static T MapUnmanagedStructure<T>(byte[] debugInfo)
        {
            GCHandle handle = GCHandle.Alloc(debugInfo, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
        
        #endregion

        // Determining constants for debugging
        public const uint INFINITE = 0xFFFFFFFF;
        public const uint DEBUG_PROCESS = 0x00000001;
        public const uint DBG_CONTINUE = 0x00010002;
        public const uint CREATE_PROCESS_DEBUG_EVENT = 3;
        public const uint EXIT_PROCESS_DEBUG_EVENT = 5;
        public const uint EXCEPTION_DEBUG_EVENT = 1;
        public const uint LOAD_DLL_DEBUG_EVENT = 6;
        public const uint OUTPUT_DEBUG_STRING_EVENT = 8;
        public const uint EXCEPTION_BREAKPOINT = 0x80000003;
        public const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;

        // Constants for VirtualProtectex
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
    }
}