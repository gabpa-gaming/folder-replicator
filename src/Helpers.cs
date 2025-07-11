using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace folder_replicator.src
{
    public class Helpers
    {
        public static string ComputeSHA256(string input)
        {
            return ComputeSHA256(Encoding.UTF8.GetBytes(input));
        } 
        public static string ComputeSHA256(byte[] input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = input;
                byte[] hash = sha256.ComputeHash(bytes);
                System.Text.StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static Options ParseArgs(string[] args)
        {
            Options options = new Options();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => options = o);
            return options;
        }

        public static bool IsSubPath(string basePath, string testPath)
        {
            var relative = Path.GetRelativePath(basePath, testPath);
            return !relative.StartsWith("..") && !Path.IsPathRooted(relative);
        }

        public static int CountUnescapedSlashes(string path)
        {
            int count = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    if (i == 0 || path[i - 1] != '\\')
                        count++;
                }
            }
            return count;
        }
    }
}