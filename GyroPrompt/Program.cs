
namespace GyroPrompt
{

    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize console
            Parser parser = new Parser();
            parser.setenvironment();
            Console.Title = ("GyroPrompt");
            Console.WriteLine("GyroPrompt (C) Copyright 2023 GyroSoft Labs");

            // If we have a file path, execute as script
            if (args.Length == 1)
            {
                string path = args[0];
                if (File.Exists(path))
                {
                    try
                    {
                        parser.run(path);
                    }
                    catch (Exception error) { Console.WriteLine("Unable to execute " + path + ", please make sure the file isn't opened or being used!)"); }
                }
            } 
            else if (args.Length > 2)
            {
                Console.WriteLine("GyroPrompt instance initiated with invalid argument count.\nCan only utilize valid script as argument.\n");
                Console.Write("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(0);
            }

            parser.beginInputLoop(); // Begin the input loop
        }
    }
}
