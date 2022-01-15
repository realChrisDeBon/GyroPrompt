using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPrompt.GraphicalPrompt
{
    class OKMessage
    {

        public void Draw(string message)
        {
            ConsoleColor grab1 = Console.BackgroundColor;
            ConsoleColor grab2 = Console.ForegroundColor;
            int x = (Console.WindowWidth / 2);
            int x2 = (Console.WindowWidth / 4);
            int x3 = (x - 2);
            int y = 3;
            int y1 = 0;
            string[] corners = { "╔", "╗", "╚", "╝", "║" };           

            if (message.Length <= x2)
            {

                Console.BackgroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.Black;
                while (y >= y1)
                {
                    Console.CursorLeft = x2;
                    if (y1 == 0) { Console.Write(corners[0]); } else if (y1 == y) { Console.Write(corners[2]); } else { Console.Write(corners[4]); }

                    int a = 0;
                    int b = (x - 2);
                    if (y1 == 0 || y1 == y)
                    {
                        while (a < b)
                        {
                            Console.Write("═");
                            a++;
                        }
                    }
                    else
                    {
                        while (a < b)
                        {
                            Console.Write(" ");
                            a++;
                        }
                    }

                    if (y1 == 0) { Console.Write(corners[1]); } else if (y1 == y) { Console.Write(corners[3]); } else { Console.Write(corners[4]); }
                    Console.Write('\n');
                    y1++;
                }
                Console.CursorLeft = x2 + 1;
                Console.CursorTop = (Console.CursorTop - 3);
                Console.WriteLine(message);
                Console.CursorLeft = x2 + 3;
                Console.Write("< ok >");
                Console.CursorLeft = Console.CursorLeft - 4;
                var G = Console.ReadKey();

                if (G.Key != ConsoleKey.Enter)
                {
                    Console.CursorLeft = Console.CursorLeft - 1;
                    Console.Write("o");
                }
                Console.WriteLine();
                Console.CursorLeft = 0;
                Console.CursorTop = Console.CursorTop + 3;

                Console.BackgroundColor = grab1;
                Console.ForegroundColor = grab2;
            } else
            {
                Console.WriteLine("GyroPrompt Script ‼ Message to print contains more character than length of window.\n");
            }
        }
    }
}
