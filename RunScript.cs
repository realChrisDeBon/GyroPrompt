using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace TinCan
{
    public class RunScript : Parser
    {
        public int CurrentLine { get; set; }
        public bool IsDirectional { get; set; }
        public void Initialize(string script)
        {
            if (File.Exists(script))
            {
                Console.WriteLine();
                Console.WriteLine("Initializing " + script);
                Run(script);
            }
            else
            {
                SendError("Specified file not found. Could not execute.");
            }
            
        }

        public void Run(string script)
        {
            
            CurrentLine = 0;
            List<string> Lines = System.IO.File.ReadAllLines(script).ToList<string>();
            while (CurrentLine < Lines.Count)
            {
                IsDirectional = false;
                if (Lines[CurrentLine].StartsWith("GOTO "))
                {
                    string[] string_array = Lines[CurrentLine].Split(' ', 2);
                    if (!string_array[1].All(char.IsDigit))
                    {
                        SendError("GOTO must point to specific line number.");
                    } else
                    {
                        IsDirectional = true;
                        int x = Convert.ToInt32(string_array[1]);
                        CurrentLine = x;
                    }
                }
                if (IsDirectional == false)

                    Parse(Lines[CurrentLine]);
                    CurrentLine++;
                }
            }
        }
}
