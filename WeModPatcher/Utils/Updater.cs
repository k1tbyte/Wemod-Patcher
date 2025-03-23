using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json;

namespace WeModPatcher.Utils
{
    public class GitHubRelease
    {
        public class AssetsType
        {
            public string Name { get; set; }
            
            [JsonProperty("browser_download_url")]
            public string Url { get; set; }
        }
        
        [JsonProperty("tag_name")]
        public string TagName { get; set; }
        
        [JsonProperty("assets")]
        public AssetsType[] Assets { get; set; }

    }
    
    public class Updater
    {
        private GitHubRelease _release = null;
        private static readonly HttpClient _httpClient = new HttpClient()
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "GitHub-Updater" }
            }
        };
        
        private static readonly string ApiUrl = $"https://api.github.com/repos/{Constants.Owner}/{Constants.RepoName}/releases/latest";
        public async Task<bool> CheckForUpdates()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var response = await _httpClient.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();
                _release = JsonConvert.DeserializeObject<GitHubRelease>(await response.Content.ReadAsStringAsync());

                if (_release == null)
                {
                    return false;
                }
            
                var latestVersion = new Version(_release.TagName);
            
                return latestVersion > currentVersion;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task Update()
        {
            if (_release == null)
            {
                throw new Exception("No release found");
            }
            
            var asset = _release.Assets.FirstOrDefault(o => o.Name.EndsWith(".exe"));
            if(asset == null)
            {
                throw new Exception("No asset found");
            }
            
            // download to temp
            var downloadPath = Path.Combine(Path.GetTempPath(), asset.Name);
            
            using(var response = await _httpClient.GetAsync(asset.Url))
            using(var fileStream = File.Create(downloadPath))
            {
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(fileStream);
            }
            
            ApplyUpdate(downloadPath);
        }
        
        
        
        private static void ApplyUpdate(string filePath)
        {
            try
            {
                var currentExecutable = Assembly.GetExecutingAssembly().Location;
                
                var psCommand = $"Start-Sleep -Seconds 2; " +
                                $"Copy-Item -Path '{filePath}' -Destination '{currentExecutable}' -Force; " +
                                $"Remove-Item -Path '{filePath}' -Force; " +
                                $"Start-Sleep -Seconds 1; " +
                                $"Start-Process -FilePath '{currentExecutable}';";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-WindowStyle Hidden -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
                
                Task.Delay(500).ContinueWith(_ => 
                {
                    App.Shutdown();
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Update failed: {ex.Message}");
            }
        }
        
    }
}