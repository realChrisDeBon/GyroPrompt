using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPrompt.GyroScriptEditor
{
    public class GyroScriptEditor
    {

    }

    class _cursor
    {
        public int Row { get; set; }
        public int Col { get; set; }

        public _cursor(int row = 0, int col = 0)
        {
            Row = row;
            Col = col;
        }
    }

    class _buffer
    {

        List<string> lines = new List<string>();
        
        public _buffer(string Lines)
        {
            string[] foldedString = Lines.Split('\n');
            foreach(string str in foldedString)
            {
                lines.Add(str);
            }   
        }

        public void Render()
        {
            foreach(string str in lines)
            {
                Console.ForegroundColor = ConsoleColor.White;
                if (str.StartsWith(':') && !str.Contains(' '));
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                }
                if (str.StartsWith("NEW "))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                if (str.StartsWith("FILESYSTEM "))
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                }
                if (str.StartsWith("BLOCKCHAIN "))
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                }
                Console.WriteLine(str);
            }
        }
    }
}
