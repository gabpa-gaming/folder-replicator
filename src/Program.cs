using CommandLine;

namespace FolderReplicator.Src
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            try
            {
                var options = Options.ParseArgs(args);
                var programLoop = new FolderReplicator(options);
                programLoop.StartLoop(); 
                Console.WriteLine("Program completed successfully.");   
            }
            catch (Exception ex)
            {
                Console.WriteLine("Program did not complete successfully.");
            }
        
        }
    }

}
