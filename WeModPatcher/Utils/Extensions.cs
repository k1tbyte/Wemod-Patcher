using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WeModPatcher.Utils
{
    public static class Extensions
    {
        public static bool CheckWeModPath(string root)
        {
            try
            {
                return File.Exists(Path.Combine(root, "WeMod.exe")) && 
                       File.Exists(Path.Combine(root, "resources", "app.asar"));
            }
            catch
            {
                return false;
            }
        }
        
        public static string FindWeModDirectory()
        {
            string localAppDataPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");

            string defaultDir = Path.Combine(localAppDataPath ?? "", "WeMod");
            
            if (!Directory.Exists(defaultDir))
            {
                return null;
            }
            
            return FindLatestWeMod(defaultDir);
        }
        
        public static string Base64Decode(string base64EncodedData) 
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        
        public static string Base64Encode(string plainText) 
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string FindLatestWeMod(string root)
        {
            var appFolders = Directory.EnumerateDirectories(root)
                .Select(folderPath => new DirectoryInfo(folderPath))
                .Where(dirInfo => Regex.IsMatch(dirInfo.Name, @"^app-\w+"))
                .Select(dirInfo => new
                {
                    Name = dirInfo.Name,
                    Path = dirInfo.FullName,
                    LastModified = dirInfo.LastWriteTime
                })
                .OrderByDescending(item => item.LastModified)
                .ToList();

            return (
                from folder 
                    in appFolders 
                where CheckWeModPath(folder.Path) 
                select folder.Path
            ).FirstOrDefault();
        }
    }
}