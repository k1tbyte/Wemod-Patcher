using System;
using System.Collections.Generic;
using System.IO;
using AsarSharp.Integrity;
using AsarSharp.Utils;

namespace AsarSharp.AsarFileSystem
{
    public class Filesystem
    {
        private readonly string _src;
        private FilesystemEntry _header;
        private int _headerSize;
        private long _offset;

        private const uint UINT32_MAX = 0xFFFFFFFF; // 2^32 - 1

        public Filesystem(string src)
        {
            _src = Path.GetFullPath(src);
            _header = new FilesystemEntry { Files = new Dictionary<string, FilesystemEntry>(StringComparer.Ordinal) };
            _headerSize = 0;
            _offset = 0;
        }

        public string GetRootPath()
        {
            return _src;
        }

        public FilesystemEntry GetHeader()
        {
            return _header;
        }

        public int GetHeaderSize()
        {
            return _headerSize;
        }

        public void SetHeader(FilesystemEntry header, int headerSize)
        {
            _header = header;
            _headerSize = headerSize;
        }

        public FilesystemEntry SearchNodeFromDirectory(string p)
        {
            FilesystemEntry json = _header;
            
            // Normalize path delimiters to system delimiters
            p = p.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            
            string[] dirs = p.Split(Path.DirectorySeparatorChar);
            
            foreach (string dir in dirs)
            {
                if (dir == "." || string.IsNullOrEmpty(dir)) continue;
                
                if (json.IsDirectory)
                {
                    if (!json.Files.ContainsKey(dir))
                    {
                        json.Files[dir] = new FilesystemEntry { Files = new Dictionary<string, FilesystemEntry>(StringComparer.Ordinal) };
                    }
                    json = json.Files[dir];
                }
                else
                {
                    throw new Exception($"Unexpected directory state while traversing: {p}");
                }
            }
            
            return json;
        }
        
        public List<string> ListFiles(bool isPack = false)
        {
            var files = new List<string>();

            FillFilesFromMetadata("/", _header);
            return files;

            void FillFilesFromMetadata(string basePath, FilesystemEntry metadata)
            {
                if (!metadata.IsDirectory)
                {
                    return;
                }

                foreach (var entry in metadata.Files)
                {
                    string childPath = entry.Key;
                    FilesystemEntry childMetadata = entry.Value;
                    string fullPath = Path.Combine(basePath, childPath).Replace('\\', '/');
                    
                    string packState = 
                        childMetadata.Unpacked == true ? "unpack" : "pack  ";
                    
                    files.Add(isPack ? $"{packState} : {fullPath}" : fullPath);
                    FillFilesFromMetadata(fullPath, childMetadata);
                }
            }
        }

        public FilesystemEntry GetNode(string p, bool followLinks = true)
        {
            // Normalize path delimiters
            p = p.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            
            FilesystemEntry node = SearchNodeFromDirectory(Extensions.GetDirectoryName(p));
            string name = Path.GetFileName(p);
            
            // Process symbolic links
            if (node.IsLink && followLinks)
            {
                return GetNode(Path.Combine(node.Link, name));
            }
            
            if (!string.IsNullOrEmpty(name))
            {
                if (node.IsDirectory && node.Files.TryGetValue(name, out var entry))
                {
                    return entry;
                }
                return null;
            }

            return node;
        }

        public FilesystemEntry GetFile(string p, bool followLinks = true)
        {
            FilesystemEntry info = GetNode(p, followLinks);

            if (info == null)
            {
                throw new Exception($"\"{p}\" was not found in this archive");
            }

            // If followLinks=false, do not allow symbolic links (TODO)
            if (info.IsLink && followLinks)
            {
                return GetFile(info.Link, followLinks);
            }
            
            return info;
        }
        
        public static string ReadLink(string path)
        {
            throw new NotImplementedException();
            return Path.GetFileName(path);
            // TODO , NOT IMPLEMENTED
        }
        
        
        
        #region Writing
        
        public FilesystemEntry SearchNodeFromPath(string p)
        {
            p = Extensions.GetRelativePath(_src, p);
            
            if (string.IsNullOrEmpty(p))
            {
                return _header;
            }
            
            var name = Path.GetFileName(p);
            var node = SearchNodeFromDirectory(Extensions.GetDirectoryName(p));
            
            if (node.Files == null)
            {
                node.Files = new Dictionary<string, FilesystemEntry>();
            }
            
            if (!node.Files.ContainsKey(name))
            {
                node.Files[name] = new FilesystemEntry();
            }
            
            return node.Files[name];
        }
        
        public void InsertDirectory(string p, bool unpack)
        {
            FilesystemEntry node = SearchNodeFromPath(p);
            node.Files = node.Files ?? new Dictionary<string, FilesystemEntry>();
            node.Unpacked = unpack;
        }
        
        public void InsertFile(string path, bool shouldUnpack, CrawledFileType file)
        {
            var dirName = Path.GetDirectoryName(path);
            var dirNode = SearchNodeFromPath(dirName);
            var node = SearchNodeFromPath(path);

            long size = 0;
            if (file.Stat is FileInfo fileInfo)
            {
                size = fileInfo.Length;
            }
            else
            {
                throw new Exception($"{path}: stat is not a file");
            }
            
            if (shouldUnpack || dirNode.Unpacked == true)
            {
                node.Size = size;
                node.Unpacked = true;
                node.Integrity =  IntegrityHelper.GetFileIntegrity(path);
                return;
            }

            // Check that the file size does not exceed UINT32_MAX
            if (size > UINT32_MAX)
            {
                throw new Exception($"{path}: file size cannot be larger than 4.2GB");
            }

            node.Size = size;
            node.Offset = _offset.ToString();
            node.Integrity = IntegrityHelper.GetFileIntegrity(path);
            if (!Extensions.IsWindowsPlatform() && (file.Stat.Attributes & FileAttributes.Hidden) != 0)
            {
                node.Executable = true;
            }
            _offset += size;
        }
        
        #endregion
    }
}