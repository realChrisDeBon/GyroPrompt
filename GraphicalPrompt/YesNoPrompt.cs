using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPrompt.GraphicalPrompt
{
    class YesNoPrompt
    {
        public int Draw(string message)
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
                Console.Write("< yes > < no >");
                Console.CursorLeft = Console.CursorLeft - 12;
                bool HasResponded = false;
                bool Response = true;
                int z = Console.CursorLeft = x2 + 5; ;
                while (HasResponded == false)
                {
                    Console.CursorLeft = x2 + 3;
                    Console.Write("< yes > < no >");
                    Console.CursorLeft = z;
                    bool IsNav = false;
                    bool Reverse = true;
                    var a = Console.ReadKey();
                    switch(a.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            Reverse = false;
                            if (Response == true)
                            {
                                Console.CursorLeft--;
                            } else if (Response == false)
                            {
                                Response = true;
                                Console.CursorLeft = x2 + 5;
                            }
                            break;
                        case ConsoleKey.RightArrow:
                            Reverse = false;
                            if (Response == true)
                            {
                                Response = false;
                                Console.CursorLeft = x2 + 13;
                            } else if (Response == false)
                            {
                                Console.CursorLeft = Console.CursorLeft - 1;
                            }
                            break;
                        case ConsoleKey.Enter:
                            Reverse = false;
                            HasResponded = true;
                            break;
                    }
                    if (Reverse == true) { Console.CursorLeft = Console.CursorLeft - 1; } else 
                    {
                        try
                        {
                            z = Console.CursorLeft;
                        }
                        catch { }
                    }

                }
                
                Console.CursorTop = Console.CursorTop + 3;

                Console.BackgroundColor = grab1;
                Console.ForegroundColor = grab2;
                if (Response == false) { return 0; } else { return 1; }
            }
            else
            {
                Console.WriteLine("GyroPrompt Script ‼ Message to print contains more character than length of window.\n");
                return ('0');
            }
        }

    }
}
