using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace folder_replicator.src
{
    public class FileInfo
    {
        public string Name { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
        public bool IsDirectory { get; set; }
        public string FullPath { get; set; }

        public FileInfo(string fullPath)
        {
            FullPath = fullPath;
            Name = Path.GetFileName(fullPath);
            IsDirectory = Directory.Exists(fullPath);
            
            if (IsDirectory)
            {
                Hash = ComputeDirectoryHash(fullPath);
            }
            else
            {
                var info = new System.IO.FileInfo(fullPath);
                Size = info.Length;
                LastModified = info.LastWriteTime;
                Created = info.CreationTime;
                Hash = Helpers.ComputeSHA256(File.ReadAllBytes(fullPath));
            }
        }

        private string ComputeDirectoryHash(string dirPath)
        {
            var entries = Directory.GetFileSystemEntries(dirPath)
                .OrderBy(x => x)
                .Select(x => new FileInfo(x).Hash);
            return Helpers.ComputeSHA256(string.Join("", entries));
        }
    }
}