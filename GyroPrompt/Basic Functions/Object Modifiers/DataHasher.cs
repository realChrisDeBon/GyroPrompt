using System.Text;
using System.Security.Cryptography;

namespace GyroPrompt.Basic_Functions.Object_Modifiers
{
    public class DataHasher
    {
        public string CalculateHash(object input)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes($"{input}");
            byte[] outputBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(outputBytes);
        }

        public string CalculateHash512(object input)
        {
            SHA512 sha512 = SHA512.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes($"{input}");
            byte[] outputBytes = sha512.ComputeHash(inputBytes);
            return Convert.ToBase64String(outputBytes);
        }

        public string CalculateHash384(object input)
        {
            SHA384 sha384 = SHA384.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes($"{input}");
            byte[] outputBytes = sha384.ComputeHash(inputBytes);
            return Convert.ToBase64String(outputBytes);
        }
    }
}