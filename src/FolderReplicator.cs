using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using BsDiff;
using CommandLine;
using Microsoft.VisualBasic;

namespace folder_replicator.src
{
    public class FolderReplicator
    {
        public Options options { get; set; }

        public FolderReplicator(Options options)
        {
            this.options = options;
            this.options.Source = Path.GetFullPath(options.Source);
            this.options.Destination = Path.GetFullPath(options.Destination);
            this.options.LogFile = Path.GetFullPath(options.LogFile);
            Logger.Instance.VerboseConsole = this.options.Verbose;
        }

        public FolderReplicator(string[] args) : this(Helpers.ParseArgs(args)) { }

        public bool StartLoop()
        {
            if (options.Verbose)
            {
                Console.WriteLine("Starting Folder Replicator...");
            }

            bool valid = ValidatePaths();

            while (valid)
            {
                if (options.Once && options.Verbose)
                {
                    Console.WriteLine("Once option is enabled. Syncing once...");
                }
                if (options.Verbose)
                {
                    Console.WriteLine($"Syncing from {options.Source} to {options.Destination}...");
                }

                Replicate();

                if (options.Once)
                {
                    Console.WriteLine("Exiting...");

                    break;
                }
                if (options.Verbose)
                {
                    Console.WriteLine($"Waiting for {options.Interval} minutes before the next sync...");
                }
                Thread.Sleep((int)(options.Interval * 60000.0d));
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
            return valid;
        }

        public bool ValidatePaths()
        {
            var sourcePath = options.Source;
            var destinationPath = options.Destination;

            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
            {
                Console.WriteLine("Source and Destination paths must be provided.");
                return false;
            }

            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine($"Source directory '{sourcePath}' does not exist.");
                return false;
            }

            if (Helpers.IsSubPath(sourcePath, destinationPath))
            {
                Console.WriteLine($"Destination '{destinationPath}' cannot be a subpath of source '{sourcePath}'.");
                return false;
            }

            if (Helpers.IsSubPath(destinationPath, sourcePath))
            {
                Console.WriteLine($"Source '{sourcePath}' cannot be a subpath of destination '{destinationPath}'.");
                return false;
            }

            if (Helpers.IsSubPath(destinationPath, options.LogFile) || Helpers.IsSubPath(sourcePath, options.LogFile))
            {
                Console.WriteLine($"Log file '{options.LogFile}' cannot be a subpath of destination '{destinationPath}' or source '{sourcePath}'.");
                return false;
            }

            if (!Directory.Exists(destinationPath))
            {
                try
                {
                    Directory.CreateDirectory(destinationPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create destination directory '{destinationPath}': {ex.Message}");
                    return false;
                }
            }
            return true;
        }

        public void Replicate()
        {
            var sourceTree = new FileTreeManager(options.Source);
            var destinationTree = new FileTreeManager(options.Destination);

            var movedRenamed = FindAndMoveRenamedFiles(sourceTree, destinationTree);
            var copied = CopyAddedFiles(sourceTree, destinationTree);
            var removed = RemoveDeletedFiles(sourceTree, destinationTree);
            var changed = UpdateChangedFiles(sourceTree, destinationTree);

            var totalErrors = movedRenamed.Item1 + copied.Item1 + removed.Item1 + changed.Item1;

            if (options.Verbose)
            {
                Console.WriteLine($"Sync completed with {movedRenamed.Item2} renamed/moved files, " +
                            $"{copied.Item2} added files, " +
                            $"{removed.Item2} deleted files, " +
                            $"{changed.Item2} updated files, " +
                            $"and {totalErrors} errors.");
            }
        }

        private Tuple<int, int> FindAndMoveRenamedFiles(FileTreeManager sourceTree, FileTreeManager destinationTree)
        {
            int errorCount = 0;
            int movedCount = 0;

            var sourceFiles = sourceTree.GetFiles();
            var destinationFiles = destinationTree.GetFiles();

            foreach (var (relativePath, sourceFile) in sourceFiles.OrderBy(pair => Helpers.CountUnescapedSlashes(pair.Key)))
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
                        var oldPath = Path.GetRelativePath(options.Destination, matchingFile.FullPath);

                        if (oldPath != relativePath)
                        {
                            Logger.Instance.Log($"File was renamed/moved: {oldPath} to {relativePath}");
                            destinationTree.MoveFile(oldPath, relativePath);
                            Logger.Instance.Log($"File was moved successfully: {oldPath} to {relativePath}");
                            movedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error while renaming or moving a file: {ex.Message}");
                    errorCount++;
                }
            }

            return Tuple.Create(errorCount, movedCount);
        }

        private Tuple<int, int> CopyAddedFiles(FileTreeManager sourceTree, FileTreeManager destinationTree)
        {
            int errorCount = 0;
            int addedCount = 0;

            var sourceFiles = sourceTree.GetFiles();
            var destinationFiles = destinationTree.GetFiles();

            var filesToAdd = sourceFiles.Keys.Except(destinationFiles.Keys)
                .OrderBy(path => Helpers.CountUnescapedSlashes(path));

            foreach (var relativePath in filesToAdd)
            {
                try
                {
                    sourceTree.CopyFile(relativePath, options.Destination);
                    addedCount++;
                    
                    var fileType = sourceFiles[relativePath].IsDirectory ? "Directory" : "File";
                    Logger.Instance.Log($"{fileType} was added: {relativePath}");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error copying file {relativePath}: {ex.Message}");
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
                .OrderByDescending(path => Helpers.CountUnescapedSlashes(path));

            foreach (var relativePath in filesToDelete)
            {
                try
                {
                    destinationTree.DeleteFile(relativePath);
                    deletedCount++;
                    
                    var fileType = destinationFiles.ContainsKey(relativePath) && destinationFiles[relativePath].IsDirectory ? "Directory" : "File";
                    Logger.Instance.Log($"{fileType} was deleted: {relativePath}");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error deleting file {relativePath}: {ex.Message}");
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
                        sourceTree.CopyFile(relativePath, options.Destination);
                        updatedCount++;
                        Logger.Instance.Log($"File was modified: {relativePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error updating file {relativePath}: {ex.Message}");
                    errorCount++;
                }
            }

            return Tuple.Create(errorCount, updatedCount);
        }
    }
}