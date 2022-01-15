using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Timers;
using GyroPromptNameSpace;

namespace GyroPrompt.Functions
{
    public class TimedFunction : Parser
    {
        public List<string> FunctionCommands = new List<string>();
        public int Interval { get; set; }
        public string Name { get; set; }
        public bool Active = false;
        public const bool Running = true;
        public int Counter = 0;

        public TimedFunction(int interval, string name)
        {
            Interval = interval;
            Name = name;
        }

        public void Add(string command) { FunctionCommands.Add(command);}
        public void Start() { Active = true; }
        public void Stop() { Active = false; Counter = 0; }
            
        public void Initiate()
        {
            Thread thread = (new Thread(Execute));
            //thread.Start();
        }

        public void Execute()
        {
            while (Running == true) { 
                while (Active == true)
                {
                    Thread.Sleep(Interval * 1000);
                    //Console.WriteLine();
                    foreach (string command in FunctionCommands)
                    {
                        Parse(command);
                        Thread.Sleep(200);
                    }
                }
            }
        }

    }
}
