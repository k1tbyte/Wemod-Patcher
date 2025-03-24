using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WeModPatcher.Models;
using WeModPatcher.Utils;
using WeModPatcher.Utils.Win32;
using WeModPatcher.View.MainWindow;

namespace WeModPatcher.Core
{

    public class RuntimePatcher
    {
        private readonly string _exePath;

        public RuntimePatcher(string exePath)
        {
            _exePath = exePath;
        }
        
        
        public void StartProcess()
        {
            if(string.IsNullOrEmpty(_exePath))
            {
                throw new Exception("Path is not specified");
            }
            
            KillWeMod();
            var startupInfo = new Imports.StartupInfo { cb = Marshal.SizeOf(typeof(Imports.StartupInfo)) };
            if(!Imports.CreateProcessA(_exePath, 
                   null, 
                   IntPtr.Zero, 
                   IntPtr.Zero, 
                   false, Imports.DEBUG_PROCESS, IntPtr.Zero, 
                   null, ref startupInfo, out var processInfo))
            {
                throw new Exception("Failed to create process, error code: " + Marshal.GetLastWin32Error());
            }
            
            var debugEvent = new Imports.DEBUG_EVENT();
            var processIds = new List<uint>();
            while (Imports.WaitForDebugEvent(ref debugEvent, 1000))
            {
                var code = debugEvent.dwDebugEventCode;
                if (code == Imports.CREATE_PROCESS_DEBUG_EVENT)
                {
                    processIds.Add(debugEvent.dwProcessId);
                }
                else if (code == Imports.EXCEPTION_DEBUG_EVENT)
                {
                    var exceptionInfo = Imports.MapUnmanagedStructure<Imports.EXCEPTION_DEBUG_INFO>(debugEvent.Union);
                    
                    if (exceptionInfo.ExceptionRecord.ExceptionCode == Imports.EXCEPTION_BREAKPOINT)
                    {
                        var process = Process.GetProcessById((int)debugEvent.dwProcessId);
                        var address = MemoryUtils.ScanVirtualMemory(
                            process.Handle,
                            process.Modules[0].BaseAddress, 
                            process.Modules[0].ModuleMemorySize, 
                            Constants.ExePatchSignature.Sequence, Constants.ExePatchSignature.Mask
                        );
                        
                        if (address != IntPtr.Zero)
                        {
                            MemoryUtils.SafeWriteVirtualMemory(
                                process.Handle, 
                                address + Constants.ExePatchSignature.Offset,
                                Constants.ExePatchSignature.PatchBytes
                            );
                            
                            /*byte[] patchedBytes = new byte[32];
                            if (Imports.ReadProcessMemory(process.Handle, address, patchedBytes, patchedBytes.Length, out int bytesRead))
                            {
                                Console.WriteLine("Bytes after patching: ");
                                for (int i = 0; i < bytesRead; i++)
                                {
                                    Console.Write($"{patchedBytes[i]:X2} ");
                                }
                                Console.WriteLine();
                            }*/
                        }
                    }
                }
                
                Imports.ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, Imports.DBG_CONTINUE);
            }

            foreach (var processId in processIds)
            {
                Imports.DebugActiveProcessStop(processId);
            }
            
            Imports.CloseHandle(processInfo.hProcess);
        }
                
        public static void Patch(PatchConfig config, Action<string, ELogType> logger)
        {
            if (config.Path == null)
            {
                throw new Exception("Path is not specified");
            }
            
            var parent = Directory.GetParent(config.Path)?.FullName ?? config.Path;
            var latestPath = Extensions.FindLatestWeMod(parent) ?? config.Path;

            if (!Extensions.CheckWeModPath(latestPath))
            {
                throw new Exception("Invalid WeMod path");
            }
            
            if(!File.Exists(Path.Combine(latestPath, "resources", "app.asar.backup")))
            {
                config.PatchMethod = EPatchProcessMethod.None;
                new StaticPatcher(latestPath, logger, config).Patch();
            }
            
            new RuntimePatcher(Path.Combine(latestPath, "WeMod.exe"))
                .StartProcess();
        }
        
        
        
        public static void KillWeMod()
        {
            Process[] processes = Process.GetProcessesByName("WeMod");
            for (int i = 0; processes.Length > i || i < 5; i++)
            {
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // ignored
                    }
                }
                
                processes = Process.GetProcessesByName("WeMod");
                Thread.Sleep(250);
            }
            
            if (processes.Length > 0)
            {
                throw new Exception("Failed to kill WeMod");
            }
        }
    }
}