
namespace FolderReplicator.Src
{
    public class Logger
    {
        public static Logger Instance { get; } = new Logger();
     
        public string LogFile { get; set; } = "replicator.log";

        public bool VerboseConsole { get; set; } = true;

        public void Log(string message)
        {
            if (VerboseConsole)
            {
                Console.WriteLine(message);
            }
            try
            {
                using var writer = new StreamWriter(LogFile, true);
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}