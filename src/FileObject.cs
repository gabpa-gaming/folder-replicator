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

        private FileObject? ParentFile;

        private string _path;

        public string Path
        {
            get
            {
                if (ParentFile == null)
                    return _path;
                return System.IO.Path.Combine(ParentFile.Path, Name);
            }
            private set
            {
                _path = value;
            }
        }
        public List<FileObject> Files { get; }


        private string _contentsHash;
    
        public string ContentsHash {
            get {
                
                return _contentsHash;
            }
            set
            {
                _contentsHash = value;
            }
        }


        public FileObject(string path, FileObject? parent)
        {
            Path = path;
            IsDirectory = Directory.Exists(path);
            Name = System.IO.Path.GetFileName(path);
            if (IsDirectory)
            {
                Files = new List<FileObject>();
                foreach (var file in Directory.GetFiles(path))
                {
                    Files.Add(new FileObject(file, parent));
                }
                foreach (var file in Directory.GetDirectories(path))
                {
                    Files.Add(new FileObject(file, parent));
                }
                Files.Sort();
                ContentsHash = Helpers.ComputeSHA256(string.Join("", Files.Select(f => f.ContentsHash))); ;
            }
            else
            {
                Size = new System.IO.FileInfo(path).Length;
                LastModified = System.IO.File.GetLastWriteTime(path);
                Created = System.IO.File.GetCreationTime(path);
                ContentsHash = Helpers.ComputeSHA256(System.IO.File.ReadAllBytes(path));
            }
            ParentFile = parent;
        }
        

        public List<FileObject> Flatten()
        {
            var list = new List<FileObject>();
            if (this.Files != null)
            {
                foreach (var f in this.Files)
                    list.AddRange(f.Flatten());
            }
            list.Add(this);
            return list;
        }
        
        public void Move(string newPath)
        {
            if (IsDirectory)
            {
                System.IO.Directory.Move(Path, newPath);
                Path = newPath;
            }
            else
            {

                System.IO.File.Move(Path, newPath);
                Path = newPath;
            }
        }
    }
}