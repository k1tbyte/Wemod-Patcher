using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AsarSharp;
using WeModPatcher.Models;
using WeModPatcher.View.MainWindow;

namespace WeModPatcher.Utils
{
    public class Patcher
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
        
        // ...
        // test eax, eax (0x85 for	r/m16/32/64)
        // jnz      short loc_1403A4DD2 (Integrity check failed)
        // call    near ptr funk_1445527E0
        // ...
        private const string PatchSignature = "E8 ?? ?? ?? ?? ?? C0 75 ?? F6 C3 01 74 ?? 48 89 F9 E8 ?? ?? ?? ??";
        private static readonly byte[] PatchBytes = { 0x31 };
        private const int PatchOffset = 0x5;
        
        private readonly string _weModRootFolder;
        private readonly Action<string, ELogType> _logger;
        private readonly HashSet<EPatchType> _config;
        private readonly string _asarPath;
        private readonly string _backupPath;
        private readonly string _unpackedPath;
        private int _sumOfPatches = 0;

        public Patcher(string weModRootFolder, Action<string, ELogType> logger, HashSet<EPatchType> config)
        {
            _weModRootFolder = weModRootFolder;
            _logger = logger;
            _config = config;

            _asarPath = Path.Combine(weModRootFolder, "resources", "app.asar");
            _unpackedPath = Path.Combine(weModRootFolder, "resources", "app.asar.unpacked");
            _backupPath = Path.Combine(weModRootFolder, "resources", "app.asar.backup");
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
            
            var requestedPatches = _config.ToList();
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

        private async Task PatchPE()
        {
            _logger("[PATCHER] Patching PE...", ELogType.Info);
            var pePath = Path.Combine(_weModRootFolder, "WeMod.exe");
            var patchResult = await PatternScanner.PatchBySignature(pePath, PatchSignature, PatchBytes, PatchOffset);
            if(patchResult == -1)
            {
                _logger("[PATCHER] Failed to patch PE", ELogType.Error);
                return;
            }
            _logger(patchResult == 0 ? "[PATCHER] PE already patched!" : "[PATCHER] PE patched successfully!", ELogType.Success);
        }
        
        public async Task Patch()
        {
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
                _logger("[PATCHER] app.asar not found!", ELogType.Error);
                return;
            }

            try
            {
                _logger("[PATCHER] Extracting app.asar...", ELogType.Info);
                AsarExtractor.ExtractAll(_asarPath, _unpackedPath);
            }
            catch (Exception e)
            {
                _logger($"[PATCHER] Failed to unpack app.asar: {e.Message}", ELogType.Error);
                return;
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
                _logger($"[PATCHER] Failed to pack app.asar: {e.Message}", ELogType.Error);
                return;
            }
            
          //  await PatchPE();
            
            _logger("[PATCHER] Done!", ELogType.Success);
        }
    }
}