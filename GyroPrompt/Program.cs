namespace GyroPrompt
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser();
            parser.setenvironment();
            Console.Title = ("GyroPrompt");
            Console.WriteLine("GyroPrompt (C) Copyright 2023 GyroSoft Labs");
            while (true)
            {
                Console.Write("GyroPrompt > ");
                string command = Console.ReadLine();
                parser.parse(command);
            }
        }
    }
}