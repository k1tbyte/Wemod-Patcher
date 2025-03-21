using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AsarSharp.AsarFileSystem;
using AsarSharp.Utils;

namespace AsarSharp
{

    public class CreateOptions
    {
        public Regex Unpack { get; set; }
    }

    public class AsarCreator
    {
        private readonly string _folderPath;
        private readonly string _destPath;
        private readonly CreateOptions _options;
        private List<string> _filenames = new List<string>();
        private Dictionary<string, CrawledFileType> _metadata = new Dictionary<string, CrawledFileType>();

        public AsarCreator(string folderPath, string destPath, CreateOptions options)
        {
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _destPath = destPath ?? throw new ArgumentNullException(nameof(destPath));
            _options = options;
        }
        
        public void CreatePackageWithOptions()
        {
            var result =  FileSystemCrawler.CrawlFileSystem(_folderPath);
            _filenames = result.filenames;
            _metadata = result.metadata;
            CreatePackageFromFiles();
        }


        public void CreatePackageFromFiles()
        {
            var filesystem = new Filesystem(_folderPath);
            var files = new List<Disk.BasicFileInfo>();
            
            var filenamesSorted = _filenames.ToList();
            
            foreach (var filename in filenamesSorted)
            {
                HandleFile(filesystem, filename, files);
            }

            InsertsDone(filesystem, files);
        }
        
        
        
        private void HandleFile(Filesystem filesystem, string filename, List<Disk.BasicFileInfo> files)
        {
            if (!_metadata.ContainsKey(filename))
            {
                var fileType = FileSystemCrawler.DetermineFileType(filename);
                _metadata[filename] = fileType ?? throw new Exception("Unknown file type for file: " + filename);
            }
            var file = _metadata[filename];

            switch (file.Type)
            {
                case FileType.Directory:
                    filesystem.InsertDirectory(filename, false);
                    break;
                case FileType.File:
                    var shouldUnpack = ShouldUnpackPath(Extensions.GetRelativePath(_folderPath, Path.GetDirectoryName(filename)));
                    files.Add(new Disk.BasicFileInfo { Filename = filename, Unpack = shouldUnpack });
                    filesystem.InsertFile(filename, shouldUnpack, file);
                    break;
                case FileType.Link:
                    throw new NotImplementedException();
            }
        }

        private bool ShouldUnpackPath(string relativePath)
        {
            return _options.Unpack?.IsMatch(relativePath) == true;
        }

        private void InsertsDone(Filesystem filesystem, List<Disk.BasicFileInfo> files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_destPath) ?? throw new InvalidOperationException());
            Disk.WriteFileSystem(_destPath, filesystem, new Disk.FilesystemFilesAndLinks { Files = files, Links = null }, _metadata);
        }

    }
}