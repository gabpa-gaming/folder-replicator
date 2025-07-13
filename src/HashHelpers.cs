using System.Security.Cryptography;
using System.Text;

namespace FolderReplicator.Src
{
    public class HashHelpers
    {
        public static string ComputeSHA256(string input)
        {
            return ComputeSHA256(Encoding.UTF8.GetBytes(input));
        } 
        public static string ComputeSHA256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}