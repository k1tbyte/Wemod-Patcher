using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using AsarSharp;
using WeModPatcher.ReactiveCore;
using WeModPatcher.Utils;
using WeModPatcher.View.Popups;
using Application = System.Windows.Application;

namespace WeModPatcher.View.MainWindow
{

    public class MainWindowVm : ObservableObject
    {
        private readonly MainWindow _view;
        public ObservableCollection<LogEntry> LogList { get; } = new ObservableCollection<LogEntry>();
        private static Updater _updater = new Updater();
        
        private string _weModPath;
        
        public string WeModPath
        {
            get => _weModPath;
            set
            {
                SetProperty(ref _weModPath, value);
                if (value == null) return;
                
                Log($"WeMod directory found at '{_weModPath}'", ELogType.Success);
                if (File.Exists(Path.Combine(_weModPath, "resources", "app.asar.backup")))
                {
                    Log("WeMod already patched. If you want to patch again, please restore the backup first.", ELogType.Warn);
                    IsPatchEnabled = false;
                    AlreadyPatched = true;
                    return;
                }
                Log("Ready for patching.", ELogType.Info);
                IsPatchEnabled = true;
            }
        }

        private bool _isPatchEnabled;

        public bool IsPatchEnabled
        {
            get => _isPatchEnabled;
            set => SetProperty(ref _isPatchEnabled, value);
        }
        
        private bool _alreadyPatched;
        public bool AlreadyPatched
        {
            get => _alreadyPatched;
            set => SetProperty(ref _alreadyPatched, value);
        }
        
        private bool _isUpdateAvailable;
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetProperty(ref _isUpdateAvailable, value);
        }
        
        public RelayCommand SetFolderPathCommand { get; }
        public RelayCommand ApplyPatchCommand { get; }
        public RelayCommand RestoreBackupCommand { get; }
        public AsyncRelayCommand UpdateCommand { get; }
        
        private static bool CheckWeModPath(string root)
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
        
        public string FindWeModDirectory()
        {
            string localAppDataPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");

            string defaultDir = Path.Combine(localAppDataPath ?? "", "WeMod");
            
            if (!Directory.Exists(defaultDir))
            {
                return null;
            }
            
            var appFolders = Directory.EnumerateDirectories(defaultDir)
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
        
        private void OnFolderPathSelection(object obj)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                dialog.Description = "Select the WeMod directory";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() != DialogResult.OK) return;
                string selectedPath = dialog.SelectedPath;
                string fileName = Path.GetFileName(selectedPath);

                if (CheckWeModPath(selectedPath))
                {
                    WeModPath = selectedPath;
                    return;
                }

                LogList.Add(new LogEntry
                {
                    LogType = ELogType.Error,
                    Message = $"The selected folder '{fileName}' is not a valid WeMod directory."
                });
            }
        }

        private void OnBackupRestoring(object param)
        {
            var backupPath = Path.Combine(WeModPath, "resources", "app.asar.backup");
            if (!File.Exists(backupPath))
            {
                Log("Backup not found. Please dont delete it manually", ELogType.Error);
                return;
            }
            
            try
            {
                using (File.Open(backupPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }
            catch
            {
                Log("Backup file is locked. Please close the WeMod and try again.", ELogType.Error);
                return;
            }
            
            File.Copy(backupPath, Path.Combine(WeModPath, "resources", "app.asar"), true);
            File.Delete(backupPath);
            Log("Backup restored successfully.", ELogType.Success);
            AlreadyPatched = false;
            IsPatchEnabled = true;
        }

        private void OnPatching(object param)
        {
            if (WeModPath == null)
            {
                Log("Can't be done. Please specify the directory first.", ELogType.Warn);
                return;
            }
            
            MainWindow.Instance.OpenPopup(new PatchVectorsPopup(async config =>
            {
                MainWindow.Instance.ClosePopup();
                IsPatchEnabled = false;
                await Task.Run(() => new Patcher(WeModPath, Log, config).Patch());
                IsPatchEnabled = true;
            }), "What are we gonna patch?");
        }

        private void Log(string message, ELogType logType)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                message = $"[{logType.ToString().ToUpper()}] {message}";

                var entry = new LogEntry
                {
                    LogType = logType,
                    Message = message
                };
                LogList.Add(entry);
                _view.LogList.ScrollIntoView(entry);
            });
        }

        private async Task OnUpdate(object param)
        {
            await Task.Run(async () =>
            {
                try
                {
                    await _updater.Update();
                }
                catch (Exception e)
                {
                    Log($"Failed to update: {e.Message}", ELogType.Error);
                    return;
                }
                
                Log("WeModPatcher updated successfully. Restarting...", ELogType.Success);
            });
        }
        
        public MainWindowVm(MainWindow view)
        {
            Task.Run(async () => IsUpdateAvailable = await _updater.CheckForUpdates());
            _view = view;
            SetFolderPathCommand = new RelayCommand(OnFolderPathSelection);
            ApplyPatchCommand = new RelayCommand(OnPatching);
            RestoreBackupCommand = new RelayCommand(OnBackupRestoring);
            UpdateCommand = new AsyncRelayCommand(OnUpdate);
            
            WeModPath = FindWeModDirectory();
            if (WeModPath == null)
            {
                Log("WeMod directory not found.", ELogType.Error);
            }
        }
    }
}