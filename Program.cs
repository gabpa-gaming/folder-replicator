using System.Threading.Tasks;
using CommandLine;

namespace folder_replicator.src
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    var programLoop = new FolderReplicator(options);
                    var res = programLoop.StartLoop();

                    if (res)
                    {
                        Console.WriteLine("Program completed successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Program did not complete successfully.");
                    }
                });
        }
    }

}
