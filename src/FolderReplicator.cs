namespace FolderReplicator.Src
{
    public class FolderReplicator
    {
        public Options Options { get; set; }

        public FolderReplicator(Options options)
        {
            Options = options;
        }

        public FolderReplicator(string[] args) : this(Options.ParseArgs(args)) { }

        public void StartLoop()
        {
            if (Options.Verbose)
            {
                Console.WriteLine("Starting Folder Replicator...");
            }

            while (true)
            {
                if (Options.Once && Options.Verbose)
                {
                    Console.WriteLine("Once option is enabled. Syncing once...");
                }
                if (Options.Verbose)
                {
                    Console.WriteLine($"Syncing from {Options.Source} to {Options.Destination}...");
                }

                Replicate();

                if (Options.Once)
                {
                    Console.WriteLine("Exiting...");

                    break;
                }
                if (Options.Verbose)
                {
                    Console.WriteLine($"Waiting for {Options.Interval} minutes before the next sync...");
                }
                Thread.Sleep((int)(Options.Interval * 60000.0d));
            }

            Console.WriteLine("Folder Replicator is finished. Press any key to exit...");
            try
            {
                Console.ReadKey();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("No console input available. Exiting...");
            }
        }

        

        public void Replicate()
        {
            var sourceTree = new FileTreeManager(Options.Source);
            var destinationTree = new FileTreeManager(Options.Destination);

            var movedRenamed = RenameAndMoveFiles(sourceTree, destinationTree);
            var copied = CopyAddedFiles(sourceTree, destinationTree);
            var removed = RemoveDeletedFiles(sourceTree, destinationTree);
            var changed = UpdateChangedFiles(sourceTree, destinationTree);

            var totalErrors = movedRenamed.Item1 + copied.Item1 + removed.Item1 + changed.Item1;

            if (Options.Verbose)
            {
                Console.WriteLine($"Sync completed with {movedRenamed.Item2} renamed/moved files, " +
                    $"{copied.Item2} added files, " +
                    $"{removed.Item2} deleted files, " +
                    $"{changed.Item2} updated files, " +
                    $"and {totalErrors} errors.");
            }
        }

        private Tuple<int, int> RenameAndMoveFiles(FileTreeManager sourceTree, FileTreeManager destinationTree)
        {
            int errorCount = 0;
            int movedOrRenamedCount = 0;

            var sourceFiles = sourceTree.GetFiles();
            var destinationFiles = destinationTree.GetFiles();

            foreach (var (relativePath, sourceFile) in sourceFiles.OrderBy(pair => FileTreeManager.CountUnescapedSlashes(pair.Key)))
            {
                try
                {
                    if (destinationFiles.ContainsKey(relativePath))
                        continue;

                    var matchingFiles = destinationFiles.Values
                        .Where(destFile => destFile.Hash == sourceFile.Hash && destFile.IsDirectory == sourceFile.IsDirectory)
                        .ToList();

                    if (matchingFiles.Count == 1)
                    {
                        var matchingFile = matchingFiles[0];
                        var oldPath = Path.GetRelativePath(Options.Destination, matchingFile.FullPath);

                        if (oldPath != relativePath)
                        {
                            Logger.Instance.Log($"File was renamed/moved: {oldPath} to {relativePath}");
                            destinationTree.MoveFile(oldPath, relativePath);
                            Logger.Instance.Log($"File was moved successfully in destination: {oldPath} to {relativePath}");
                            movedOrRenamedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error while renaming or moving a file at destination: {ex.Message}");
                    errorCount++;
                }
            }

            return Tuple.Create(errorCount, movedOrRenamedCount);
        }

        private Tuple<int, int> CopyAddedFiles(FileTreeManager sourceTree, FileTreeManager destinationTree)
        {
            int errorCount = 0;
            int addedCount = 0;

            var sourceFiles = sourceTree.GetFiles();
            var destinationFiles = destinationTree.GetFiles();

            var filesToAdd = sourceFiles.Keys.Except(destinationFiles.Keys)
                .OrderBy(path => FileTreeManager.CountUnescapedSlashes(path));

            foreach (var relativePath in filesToAdd)
            {
                var fileType = sourceFiles[relativePath].IsDirectory ? "Directory" : "File";
                try
                {
                    Logger.Instance.Log($"{fileType} was added: {relativePath}");
                    sourceTree.CopyFile(relativePath, Options.Destination);
                    Logger.Instance.Log($"{fileType} was copied successfully: {relativePath}");
                    addedCount++;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error copying {fileType} at {relativePath}: {ex.Message}");
                    errorCount++;
                }
            }

            return Tuple.Create(errorCount, addedCount);
        }

        private Tuple<int, int> RemoveDeletedFiles(FileTreeManager sourceTree, FileTreeManager destinationTree)
        {
            int errorCount = 0;
            int deletedCount = 0;

            var sourceFiles = sourceTree.GetFiles();
            var destinationFiles = destinationTree.GetFiles();

            var filesToDelete = destinationFiles.Keys.Except(sourceFiles.Keys)
                .OrderByDescending(path => FileTreeManager.CountUnescapedSlashes(path));

            foreach (var relativePath in filesToDelete)
            {
                var fileType = destinationFiles.ContainsKey(relativePath) && destinationFiles[relativePath].IsDirectory ? "Directory" : "File";
                try
                {
                    Logger.Instance.Log($"{fileType} was deleted: {relativePath}");
                    destinationTree.DeleteFile(relativePath);
                    Logger.Instance.Log($"{fileType} has been deleted successfully from destination: {relativePath}");
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error deleting {fileType} at {relativePath}: {ex.Message}");
                    errorCount++;
                }
            }

            return Tuple.Create(errorCount, deletedCount);
        }

        private Tuple<int, int> UpdateChangedFiles(FileTreeManager sourceTree, FileTreeManager destinationTree)
        {
            int errorCount = 0;
            int updatedCount = 0;

            var sourceFiles = sourceTree.GetFiles();
            var destinationFiles = destinationTree.GetFiles();

            var commonFiles = sourceFiles.Keys.Intersect(destinationFiles.Keys);

            foreach (var relativePath in commonFiles)
            {
                try
                {
                    var sourceFile = sourceFiles[relativePath];
                    var destinationFile = destinationFiles[relativePath];

                    if (!sourceFile.IsDirectory && sourceFile.Hash != destinationFile.Hash)
                    {
                        Logger.Instance.Log($"File was modified: {relativePath}");
                        sourceTree.CopyFile(relativePath, Options.Destination);
                        Logger.Instance.Log($"File was modified successfully in destination: {relativePath}");
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error updating file in destination {relativePath}: {ex.Message}");
                    errorCount++;
                }
            }

            return Tuple.Create(errorCount, updatedCount);
        }

    }
}