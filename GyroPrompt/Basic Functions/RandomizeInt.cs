using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Functions
{
    public class RandomizeInt
    {
        public string randomizeInt(string a_, string b_)
        {
            int a = Int32.Parse(a_);
            int b = Int32.Parse(b_);
            Random random = new Random();
            int randomNumber = random.Next(a, b + 1);
            return randomNumber.ToString();
        }
    }
}