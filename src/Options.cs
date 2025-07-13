using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace FolderReplicator.Src
{
    public class Options
    {
        [Option('s', "source", Required = true, HelpText = "Source folder path to copy from.")]
        public string Source { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Destination folder path to copy to.")]
        public string Destination { get; set; }

        [Option('i', "interval", Required = false, HelpText = "How often syncs happen. (in minutes)", Default = 0.5)]
        public double Interval { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Verbose console output.", Default = true)]
        public bool Verbose { get; set; }

        [Option('l', "log", Required = false, HelpText = "Log file path.", Default = "replicator.log")]
        public string LogFile { get; set; }

        [Option('o', "once", Required = false, HelpText = "Runs the sync once and exits.", Default = false)]
        public bool Once { get; set; }

        public static Options ParseArgs(string[] args)
        {
            Options options = new Options();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    options = o; 
                });

                if (!options.ValidatePaths())
                {
                    throw new ArgumentException("Invalid paths provided.");
                }

                Logger.Instance.VerboseConsole = options.Verbose;
                Logger.Instance.LogFile = options.LogFile;

            return options;
        }

        public bool ValidatePaths()
        {

            if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Destination))
            {
                Console.WriteLine("Source and Destination paths must be provided.");
                return false;
            }

            if (!Directory.Exists(Source))
            {
                Console.WriteLine($"Source directory '{this.Source}' does not exist.");
                return false;
            }

            if (FileTreeManager.IsSubPath(Source, Destination))
            {
                Console.WriteLine($"Destination '{Destination}' cannot be a subpath of source '{Source}'.");
                return false;
            }

            if (FileTreeManager.IsSubPath(Destination, Source))
            {
                Console.WriteLine($"Source '{Source}' cannot be a subpath of destination '{Destination}'.");
                return false;
            }

            if (Directory.Exists(LogFile))
            {
                Console.WriteLine($"Log file path '{LogFile}' is a directory.");
                return false;
            }

            if (Path.Exists(LogFile))
            {
                Console.WriteLine($"Log file path '{LogFile}' doesn't exist.");
                return false;
            }
            


            if (FileTreeManager.IsSubPath(Destination, LogFile) || FileTreeManager.IsSubPath(Source, LogFile))
            {
                Console.WriteLine($"Log file '{LogFile}' cannot be a subpath of destination '{Destination}' or source '{Source}'.");
                return false;
            }

            if (!Directory.Exists(Destination))
            {
                try
                {
                    Directory.CreateDirectory(Destination);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create destination directory '{Destination}': {ex.Message}");
                    return false;
                }
            }
            return true;
        }
    }
}