using System;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using Microsoft.Win32;
using System.Runtime.InteropServices;

using GyroPrompt.BootSystem;

namespace GyroPromptNameSpace
{
    public class Program
    { 
        public static void Main(string[] args)
        {
            InitializeWindows SystemsCheck = new InitializeWindows();
            bool ClearToProceed = SystemsCheck.SystemsCheck();
            if (ClearToProceed == false) { Console.Write("Press any key to continue..."); Console.ReadKey(); Environment.Exit(0); }
            
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Parser parser = new Parser();
            parser.InitiateEnvironmentalVariables();
            Console.ForegroundColor = ConsoleColor.White;
            parser.textColor = ConsoleColor.White;
            bool Operating = true;
            Console.Title = ("GyroPrompt Console");
            Console.WriteLine("GyroPrompt Scripting Language.\n");
            parser.LoadGyro();
            if (args.Length == 1)
            {
                string path = args[0];
                if (File.Exists(path))
                {
                    try
                    {
                        parser.Run(path);
                    }
                    catch { Console.WriteLine("Unables to execute " + path + ", please make sure the file isn't opened or being used!"); }
                }
            } else if (args.Length > 1)
            {
                Console.WriteLine("GyroPrompt instance initiated with invalid argument count.\nCan only utilize valid script as argument.\n");
                Console.Write("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(0);
            }

            string Input;
            while (Operating == true)
            {
                if (parser.AddPrompt == true) {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("GyroPrompt Script > ");
                    Console.ForegroundColor = parser.textColor;
                }
                var UserInput = Console.ReadLine();
                Input = UserInput.ToString();
                parser.Parse(Input);
            }
        }
    }
}