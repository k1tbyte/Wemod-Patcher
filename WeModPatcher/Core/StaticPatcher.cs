using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AsarSharp;
using Newtonsoft.Json;
using WeModPatcher.Models;
using WeModPatcher.Utils;
using WeModPatcher.View.MainWindow;
using Application = System.Windows.Application;

namespace WeModPatcher.Core
{
    public class StaticPatcher
    {
        private class PatchEntry
        {
            public Regex Target { get; set; }
            public string Patch { get; set; }
            public bool Applied { get; set; }
            public bool SingleMatch { get; set; } = true;
            public bool DynamicFieldResolve { get; set; }
        }

        private static readonly Dictionary<EPatchType, PatchEntry> Patches = new Dictionary<EPatchType, PatchEntry>()
        {
            {
                EPatchType.ActivatePro,
                new PatchEntry
                {
                    DynamicFieldResolve = true,
                    Target = new Regex(@"getUserAccount\(\)\{.*?return\s+this\.#\w+\.fetch\(\{.*?\}\)\}", RegexOptions.Singleline),
                    Patch = "getUserAccount(){return this.#<fetch_field_name>.fetch({endpoint:\"/v3/account\",method:\"GET\",name:\"/v3/account\",collectMetrics:0}).then(response=>{response.subscription={period:\"yearly\",state:\"active\"};response.flags=78;return response;})}"
                }
            },
            {
                EPatchType.DisableUpdates,
                new PatchEntry
                {
                    Target = new Regex(@"registerHandler\(""ACTION_CHECK_FOR_UPDATE"".*?\)\)\)\)", RegexOptions.Singleline),
                    Patch = "registerHandler(\"ACTION_CHECK_FOR_UPDATE\",(e=>expectUpdateFeedUrl(e,(e=>null)))"
                }
            }
        };
        
        private readonly string _weModRootFolder;
        private readonly Action<string, ELogType> _logger;
        private readonly PatchConfig _config;
        private readonly string _asarPath;
        private readonly string _backupPath;
        private readonly string _unpackedPath;
        private int _sumOfPatches = 0;
        private readonly string _exePath;

        public StaticPatcher(string weModRootFolder, Action<string, ELogType> logger, PatchConfig config)
        {
            _weModRootFolder = weModRootFolder;
            _logger = logger;
            _config = config;

            _asarPath = Path.Combine(weModRootFolder, "resources", "app.asar");
            _unpackedPath = Path.Combine(weModRootFolder, "resources", "app.asar.unpacked");
            _backupPath = Path.Combine(weModRootFolder, "resources", "app.asar.backup");
            _exePath = Path.Combine(_weModRootFolder, "WeMod.exe");
        }
        
        private static string GetFetchFieldName(string targetFunction)
        {
            var fetchMatch = Regex.Match(targetFunction, @"return\s+this\.#(\w+)\.fetch");
            return fetchMatch.Success ? fetchMatch.Groups[1].Value : null;
        }

        private void ApplyJsPatch(string fileName, string js, PatchEntry patch, EPatchType patchType)
        {
            if (patch.Applied)
            {
                return;
            }
            
            var matches = patch.Target.Matches(js);
            if (matches.Count == 0)
            {
                return;
            }
            
            if(matches.Count > 1 && patch.SingleMatch)
            {
                throw new Exception(
                    $"[PATCHER] [{patchType}] Patch failed. Multiple target functions found. Looks like the version is not supported");
            }

            if (patch.DynamicFieldResolve)
            {
                string fetchFieldName = GetFetchFieldName(matches[0].Value);
                if (string.IsNullOrEmpty(fetchFieldName))
                {
                    throw new Exception($"[PATCHER] [{patchType}] Fetch field name not found");
                }
                
                patch.Patch = patch.Patch.Replace("<fetch_field_name>", fetchFieldName);
            }
            
            _logger($"[PATCHER] [{patchType}] Found target function in: " + Path.GetFileName(fileName), ELogType.Info);

            
            File.WriteAllText(fileName, patch.Target.Replace(js, patch.Patch));
            _logger($"[PATCHER] [{patchType}] Patch applied", ELogType.Success);
            patch.Applied = true;
            _sumOfPatches -= (int)patchType;
        }

        private void PatchAsar()
        {
            var items = Directory.EnumerateFiles(_unpackedPath)
                .Where(file => !Directory.Exists(file) && Regex.IsMatch(Path.GetFileName(file), @"^app-\w+|index\.js"))
                .ToList();

            if (!items.Any())
            {
                throw new Exception("[PATCHER] No app bundle found");
            }
            
            var requestedPatches = _config.PatchTypes.ToList();
            requestedPatches.ForEach(patch => _sumOfPatches += (int)patch);
            foreach (var item in items)
            {
                if (_sumOfPatches <= 0)
                {
                    break;
                }
                
                string data = File.ReadAllText(item);
                foreach (var entry in requestedPatches)
                {
                    ApplyJsPatch(item, data, Patches[entry], entry);
                }
            }
        }

        private void PatchPe()
        {
            _logger("[PATCHER] Patching PE...", ELogType.Info);
            var patchResult = MemoryUtils.PatchFile(_exePath,Constants.ExePatchSignature, Constants.ExePatchSignature.PatchBytes);
            if(patchResult == -1)
            {
                _logger("[PATCHER] Failed to patch PE", ELogType.Error);
                return;
            }
            _logger(patchResult == 0 ? "[PATCHER] PE already patched!" : "[PATCHER] PE patched successfully!", ELogType.Success);
        }
        
        private void CreateShortcut()
        {
            // invoke file dialog save file

            var fileDialog = new SaveFileDialog()
            {
                CheckPathExists = true,
                AddExtension = true,
                SupportMultiDottedExtensions = false,
                FileName = "WeMod",
            };
            
            if(fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            _config.Path = _weModRootFolder;

            var json = JsonConvert.SerializeObject(_config, Formatting.None);
            Utils.Win32.Shortcut.CreateShortcut(
                fileName: fileDialog.FileName + ".lnk",
                targetPath: Assembly.GetExecutingAssembly().Location,
                arguments: Extensions.Base64Encode(json),
                workingDirectory:  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                description: null,
                iconPath: _exePath
            );
            
            _logger("[PATCHER] The shortcut has been created, now you should only run WeMod through this shortcut", ELogType.Success);
        }
        
        public void Patch()
        {
            RuntimePatcher.KillWeMod();
            if (!File.Exists(_backupPath))
            {
                _logger("[PATCHER] Creating backup...", ELogType.Info);
                File.Copy(_asarPath, _backupPath);
            }
            else
            {
                _logger("[PATCHER] Backup already exists", ELogType.Warn);
            }

            if(!File.Exists(_asarPath))
            {
                throw new Exception("app.asar not found");
            }

            try
            {
                _logger("[PATCHER] Extracting app.asar...", ELogType.Info);
                AsarExtractor.ExtractAll(_asarPath, _unpackedPath);
            }
            catch (Exception e)
            {
                throw new Exception($"[PATCHER] Failed to unpack app.asar: {e.Message}");
            }
            
            PatchAsar();

            try
            {
                new AsarCreator(_unpackedPath, _asarPath, new CreateOptions
                {
                    Unpack = new Regex(@"^static\\unpacked.*$")
                }).CreatePackageWithOptions();
            }
            catch (Exception e)
            {
                throw new Exception($"[PATCHER] Failed to pack app.asar: {e.Message}");
            }

            if (_config.PatchMethod == EPatchProcessMethod.Static)
            {
                PatchPe();
            }
            else if(_config.PatchMethod == EPatchProcessMethod.Runtime)
            {
                Application.Current.Dispatcher.Invoke(CreateShortcut);
            }
            
            _logger("[PATCHER] Done!", ELogType.Success);
        }
    }
}