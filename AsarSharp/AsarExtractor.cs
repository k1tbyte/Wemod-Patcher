using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AsarSharp.AsarFileSystem;
using AsarSharp.Utils;

namespace AsarSharp
{
    public class AsarExtractor
    {
        public static void ExtractAll(string archivePath, string dest)
        {
            var filesystem = Disk.ReadFilesystemSync(archivePath);
            var filenames = filesystem.ListFiles();

            // under windows just extract links as regular files
            bool followLinks = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // create destination directory
            Directory.CreateDirectory(dest);

            var extractionErrors = new List<Exception>();
            foreach (var fullPath in filenames)
            {
                try
                {
                    // Remove leading slash
                    var filename = fullPath.Substring(1);
                    var destFilename = Path.Combine(dest, filename);
                    var file = filesystem.GetFile(filename, followLinks);

                    // Check that the file is not written outside the specified destination folder
                    string relativePath = Extensions.GetRelativePath(dest, destFilename);
                    if (relativePath.StartsWith(".."))
                    {
                        throw new InvalidOperationException($"{fullPath}: file \"{destFilename}\" writes out of the package");
                    }

                    if (file.IsDirectory)
                    {
                        // it's a directory, create it and continue with the next entry
                        Directory.CreateDirectory(destFilename);
                    }
                    // TODO (LINK NOT SUPPORTED)
                    else if (file.IsLink)
                    {
                        // it's a symlink, create a symlink
                        var linkSrcPath = Extensions.GetDirectoryName(Path.Combine(dest, file.Link));
                        var linkDestPath = Extensions.GetDirectoryName(destFilename);
                        var relativeLinkPath = Extensions.GetRelativePath(linkDestPath, linkSrcPath);

                        // try to delete output file, because we can't overwrite a link
                        try
                        {
                            File.Delete(destFilename);
                        }
                        catch {
                            // Ignore errors during file link deletion
                        }

                        var linkTo = Path.Combine(relativeLinkPath, Path.GetFileName(file.Link));
                        
                        if (Extensions.GetRelativePath(dest, linkSrcPath).StartsWith(".."))
                        {
                            throw new InvalidOperationException(
                                $"{fullPath}: file \"{file.Link}\" links out of the package to \"{linkSrcPath}\"");
                        }

                        // On Windows, creating symlinks requires additional permissions or enabling Developer Mode,
                        // so just copy the contents of the file
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            var targetPath = Path.Combine(linkSrcPath, Path.GetFileName(file.Link));
                            if (Directory.Exists(targetPath))
                            {
                                Directory.CreateDirectory(destFilename);
                                Extensions.CopyDirectory(targetPath, destFilename);
                            }
                            else if (File.Exists(targetPath))
                            {
                                Directory.CreateDirectory(Extensions.GetDirectoryName(destFilename));
                                File.Copy(targetPath, destFilename, true);
                            }
                        }
                        else
                        {
                            // On Unix systems we use symlinks
                            Directory.CreateDirectory(Extensions.GetDirectoryName(destFilename));
                            Extensions.CreateSymbolicLink(linkTo, destFilename);
                        }
                    }
                    else if (file.IsFile)
                    {
                        // it's a file, try to extract it
                        try
                        {
                            byte[] content;
     
                            content = Disk.ReadFileSync(filesystem, filename, file);
                            
                            File.WriteAllBytes(destFilename, content);
                            
                            if (file.Executable == true && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                Extensions.SetUnixFilePermission(destFilename, "755");
                            }
                        }
                        catch (Exception e)
                        {
                            extractionErrors.Add(e);
                        }
                    }
                }
                catch (Exception ex)
                {
                    extractionErrors.Add(ex);
                }
            }

            if (extractionErrors.Count > 0)
            {
                throw new AggregateException(
                    "Unable to extract some files:\n\n" +
                    string.Join("\n\n", extractionErrors.Select(e => e.ToString())),
                    extractionErrors);
            }
        }
    }
}