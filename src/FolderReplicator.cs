using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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
            var source = new FileObject(options.Source, options.LogFile);
            var destination = new FileObject(options.Destination, options.LogFile);

            var sourceFiles = source.Flatten(source);
            var destinationFiles = destination.Flatten(destination);

            var sourceDict = sourceFiles.ToDictionary(path => Path.GetRelativePath(options.Source, path.Path));
            var destinationDict = destinationFiles.ToDictionary(path => Path.GetRelativePath(options.Destination, path.Path));

            var copied = CopyAddedFiles(sourceDict, destinationDict);

            var removed = RemoveDeletedFiles(sourceDict, destinationDict);

            var changed = UpdateChangedFiles(sourceDict, destinationDict);

            var totalErrors = copied.Item1 + removed.Item1 + changed.Item1;

            if (options.Verbose)
            {
                Console.WriteLine($"Sync completed with {copied.Item2} added files, " +
                            $"{removed.Item2} deleted files, " +
                            $"{changed.Item2} updated files, " +
                            $"and {totalErrors} errors.");
            }

        }

        public Tuple<int, int> CopyAddedFiles(Dictionary<string, FileObject> sourceDict, Dictionary<string, FileObject> destinationDict)
        //// <summary> Returns a tuple with the number of errors and added files. </summary>
        {
            int errorCount = 0;
            int addedCount = 0;
            foreach (var relativePath in sourceDict.Keys.Except(destinationDict.Keys)
                .OrderBy(path => Helpers.CountUnescapedSlashes(path))) //Sort to ensure highest level directories are processed first
            {
                try
                {
                    if (sourceDict[relativePath].IsDirectory)
                    {
                        Logger.Instance.Log($"Directory was added: {relativePath}");
                        Directory.CreateDirectory(Path.Combine(options.Destination, relativePath));
                        Logger.Instance.Log($"Directory: {Path.Combine(options.Destination, relativePath)} was copied successfully.");
                        addedCount++;
                    }
                    else
                    {
                        Logger.Instance.Log($"File was added: {relativePath}");
                        var from = Path.Combine(options.Source, relativePath);
                        var to = Path.Combine(options.Destination, relativePath);
                        Logger.Instance.Log($"Copying file {from} to {to}...");
                        File.Copy(from, to, true);
                        Logger.Instance.Log($"File: {from} was copied successfully to {to}.");
                        addedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error copying file {relativePath}: {ex.Message}");
                    errorCount++;
                }
            }
            return Tuple.Create(errorCount, addedCount);
        }


        public Tuple<int, int> RemoveDeletedFiles(Dictionary<string, FileObject> sourceDict, Dictionary<string, FileObject> destinationDict)
        //// <summary> Returns a tuple with the number of errors and removed files. </summary>
        {
            int errorCount = 0;
            int deletedCount = 0;
            foreach (var relativePath in destinationDict.Keys.Except(sourceDict.Keys)
                .OrderByDescending(path => Helpers.CountUnescapedSlashes(path)))
            {
                Logger.Instance.Log($"File was deleted: {relativePath}");
                try
                {
                    if (destinationDict[relativePath].IsDirectory)
                    {
                        Directory.Delete(Path.Combine(options.Destination, relativePath), true);
                        Logger.Instance.Log($"Directory: {relativePath} was deleted successfully.");
                        deletedCount++;
                    }
                    else
                    {
                        File.Delete(Path.Combine(options.Destination, relativePath));
                        Logger.Instance.Log($"File: {relativePath} was deleted successfully.");
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log($"Error deleting file {relativePath}: {ex.Message}");
                    errorCount++;
                }
            }
            return Tuple.Create(errorCount, deletedCount);
        }

        public Tuple<int, int> UpdateChangedFiles(Dictionary<string, FileObject> sourceDict, Dictionary<string, FileObject> destinationDict)
        //// <summary> Returns a tuple with the number of errors and changed files. </summary>
        {
            int errorCount = 0;
            int updatedCount = 0;
            foreach (var relativePath in sourceDict.Keys.Intersect(destinationDict.Keys))
            {
                if (sourceDict[relativePath].ContentsHash != destinationDict[relativePath].ContentsHash)
                {
                    Logger.Instance.Log($"File was modified: {relativePath}");
                    try
                    {
                        var from = Path.Combine(options.Source, relativePath);
                        var to = Path.Combine(options.Destination, relativePath);
                        File.Copy(from, to, true);
                        Logger.Instance.Log($"File: {from} was updated successfully to {to}.");
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log($"Error updating file {relativePath}: {ex.Message}");
                        errorCount++;
                    }
                }
            }
            return Tuple.Create(errorCount, updatedCount);
        }

        
    }
}