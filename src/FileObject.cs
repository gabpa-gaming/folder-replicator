using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace folder_replicator.src
{

    public class FileObject : IComparable<FileObject>
    {
        public int CompareTo(FileObject? other)
        {
            if (other == null) return 1;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
        public string Name { get; }

        public long Size { get; }

        public DateTime LastModified { get; }

        public DateTime Created { get; }

        public bool IsDirectory { get; }
        public string Path { get; }
        public List<FileObject> Files { get; }

        public string ContentsHash { get; }


        public FileObject(string path, string metadataPath)
        {
            Path = path;
            IsDirectory = Directory.Exists(path);
            Name = System.IO.Path.GetFileName(path);
            if (IsDirectory)
            {
                Files = new List<FileObject>();
                foreach (var file in Directory.GetFiles(path))
                {
                    Files.Add(new FileObject(file, metadataPath));
                }
                foreach (var file in Directory.GetDirectories(path))
                {
                    Files.Add(new FileObject(file, metadataPath));
                }
                Files.Sort();
                ContentsHash = Helpers.ComputeSHA256(string.Join("", Files.Select(f => f.ContentsHash)));;
            }
            else
            {
                Files = null;
                Size = new System.IO.FileInfo(path).Length;
                LastModified = System.IO.File.GetLastWriteTime(path);
                Created = System.IO.File.GetCreationTime(path);
                ContentsHash = Helpers.ComputeSHA256(System.IO.File.ReadAllBytes(path));
            }
        }
        
        public List<FileObject> Flatten(FileObject root)
        {
            var list = new List<FileObject>();
            if (root.Files != null)
            {
                foreach (var f in root.Files)
                    list.AddRange(Flatten(f));
            }
            list.Add(root);
            return list;
        }
        
    }
}