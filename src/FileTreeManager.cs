
namespace folder_replicator.src
{
    public class FileTreeManager
    {
        private readonly string _rootPath;
        private readonly Dictionary<string, FileInfo> _files;

        public FileTreeManager(string rootPath)
        {
            _rootPath = Path.GetFullPath(rootPath);
            _files = new Dictionary<string, FileInfo>();
            ScanDirectory();
        }

        public Dictionary<string, FileInfo> GetFiles() => _files;

        public void ScanDirectory()
        {
            _files.Clear();
            if (Directory.Exists(_rootPath))
            {
                ScanRecursive(_rootPath);
            }
        }

        private void ScanRecursive(string currentPath)
        {
            var relativePath = Path.GetRelativePath(_rootPath, currentPath);
            if (relativePath == ".") relativePath = "";
            
            var fileInfo = new FileInfo(currentPath);
            _files[relativePath] = fileInfo;

            if (fileInfo.IsDirectory)
            {
                foreach (var entry in Directory.GetFileSystemEntries(currentPath))
                {
                    ScanRecursive(entry);
                }
            }
        }

        public void MoveFile(string fromRelativePath, string toRelativePath)
        {
            if (!_files.ContainsKey(fromRelativePath))
                throw new ArgumentException($"File not found: {fromRelativePath}");

            var fileInfo = _files[fromRelativePath];
            var fromFullPath = Path.Combine(_rootPath, fromRelativePath);
            var toFullPath = Path.Combine(_rootPath, toRelativePath);

            var parentDir = Directory.GetParent(toFullPath)?.FullName;
            if (parentDir != null && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            if (fileInfo.IsDirectory)
            {
                Directory.Move(fromFullPath, toFullPath);
            }
            else
            {
                File.Move(fromFullPath, toFullPath);
            }

            _files.Remove(fromRelativePath);
            fileInfo.FullPath = toFullPath;
            _files[toRelativePath] = fileInfo;

            if (fileInfo.IsDirectory)
            {
                UpdateChildPaths(fromRelativePath, toRelativePath);
            }
        }

        private void UpdateChildPaths(string oldParentPath, string newParentPath)
        {
            var childrenToUpdate = _files.Keys
                .Where(key => key.StartsWith(oldParentPath + Path.DirectorySeparatorChar))
                .ToList();

            foreach (var oldChildPath in childrenToUpdate)
            {
                var newChildPath = newParentPath + oldChildPath.Substring(oldParentPath.Length);
                var childInfo = _files[oldChildPath];
                
                _files.Remove(oldChildPath);
                childInfo.FullPath = Path.Combine(_rootPath, newChildPath);
                _files[newChildPath] = childInfo;
            }
        }

        public void CopyFile(string relativePath, string destinationRoot)
        {
            if (!_files.ContainsKey(relativePath))
                throw new ArgumentException($"File not found: {relativePath}");

            var fileInfo = _files[relativePath];
            var sourcePath = Path.Combine(_rootPath, relativePath);
            var destPath = Path.Combine(destinationRoot, relativePath);

            var parentDir = Directory.GetParent(destPath)?.FullName;
            if (parentDir != null && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            if (fileInfo.IsDirectory)
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                File.Copy(sourcePath, destPath, true);
            }
        }

        public void DeleteFile(string relativePath)
        {
            if (!_files.ContainsKey(relativePath))
                return;

            var fileInfo = _files[relativePath];
            var fullPath = Path.Combine(_rootPath, relativePath);

            if (fileInfo.IsDirectory)
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
            else
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }

            _files.Remove(relativePath);
        }
    }
}