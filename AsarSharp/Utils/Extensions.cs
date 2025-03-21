using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AsarSharp.Utils
{
    internal static class Extensions
    {
        public static string GetRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo))
                throw new ArgumentNullException(nameof(relativeTo));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var fullRelativeTo = Path.GetFullPath(relativeTo);
            var fullPath = Path.GetFullPath(path);

            if (string.Equals(fullRelativeTo, fullPath, StringComparison.OrdinalIgnoreCase))
                return "";

            var relativeToUri = new Uri(fullRelativeTo.EndsWith(Path.DirectorySeparatorChar.ToString()) 
                ? fullRelativeTo 
                : fullRelativeTo + Path.DirectorySeparatorChar);
            var pathUri = new Uri(fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !File.Exists(fullPath)
                ? fullPath 
                : fullPath + (Directory.Exists(fullPath) ? Path.DirectorySeparatorChar.ToString() : ""));

            var relativeUri = relativeToUri.MakeRelativeUri(pathUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString())
                .Replace('/', Path.DirectorySeparatorChar);

            return relativePath.TrimEnd(Path.DirectorySeparatorChar);
        }
        
        public static string GetDirectoryName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return ".";

            string result = Path.GetDirectoryName(path);
            
            // If the result is an empty string, return “.” as in Node.js
            if (string.IsNullOrEmpty(result))
                return ".";
                
            return result;
        }
        
        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get all files in the source directory
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Recursively copy all subdirectories
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }
        
        public static string GetBasePath(string dir)
        {
            // Look for the last path delimiter before any pattern
            int wildcardIndex = dir.IndexOfAny(new[] { '*', '?' });
            if (wildcardIndex == -1)
            {
                return dir;
            }
    
            int lastSeparatorIndex = dir.LastIndexOf(Path.DirectorySeparatorChar, wildcardIndex);
            if (lastSeparatorIndex == -1)
            {
                return ".";
            }
    
            return dir.Substring(0, lastSeparatorIndex);
        }
        
        public static void SetUnixFilePermission(string filePath, string permission)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            // Use chmod
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"{permission} \"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
        
        
        public static void CreateSymbolicLink(string linkTarget, string linkPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, creating symlinks requires special privileges,
                // so on many systems it simply won't work without administrator privileges
                NativeMethods.CreateSymbolicLink(linkPath, linkTarget,
                    Directory.Exists(linkTarget)
                        ? NativeMethods.SymLinkFlag.Directory
                        : NativeMethods.SymLinkFlag.File);
                return;
            }

            // In Unix systems we use the corresponding system call
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ln",
                    Arguments = $"-s \"{linkTarget}\" \"{linkPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
        
        
        public static bool IsWindowsPlatform()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
    }
}