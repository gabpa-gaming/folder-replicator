using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace folder_replicator.src
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
    }
}