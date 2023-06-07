
using GyroPrompt.Basic_Functions;
using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GyroPrompt
{
    public class Parser
    {
        /// <summary>
        /// Initialize some of the basic function objects and lists. These will likely be used frequently.
        /// 
        /// </summary>

        public List<LocalVariable> local_variables = new List<LocalVariable>();
        public List<object> environmental_variables = new List<object>();
        public Calculate calculate = new Calculate();

        /// <summary>
        /// Below are environmental variables. These are meant for the users to be able to interact with the console settings and modify the environment.
        /// 
        /// </summary>
        public struct ConsoleInfo {
            public ConsoleColor status_forecolor;
            public ConsoleColor status_backcolor;
        }
        public ConsoleInfo grabConsoleStatus(ConsoleColor _foreground, ConsoleColor _background)
        {
            ConsoleInfo info = new ConsoleInfo();
            info.status_forecolor = _foreground;
            info.status_backcolor = _background;
            return info;
        }
        IDictionary<string, ConsoleColor> keyConsoleColor = new Dictionary<string, ConsoleColor>();
        public void setConsoleStatus(ConsoleInfo _consoleinfo)
        {
            Console.ForegroundColor = _consoleinfo.status_forecolor;
            Console.BackgroundColor = _consoleinfo.status_backcolor;
        }

        public int CursorX = Console.CursorLeft;
        public int CursorX_
        {
            get { return CursorX; }
            set { CursorX = value; Console.CursorLeft = value; }
        }
        public int CursorY = Console.CursorTop;
        public int CursorY_
        {
            get { return CursorY; }
            set { CursorY = value; Console.CursorTop = value; }
        }
        public int WindowHeight = Console.WindowHeight;
        public int WindowHeight_
        {
            get { return WindowHeight; }
            set { WindowHeight = value; if (Console.BufferHeight < value) { Console.BufferHeight = (value * 2); } Console.WindowHeight = value; Console.SetWindowSize(WindowWidth_, WindowHeight_); }
        }
        public int WindowWidth = Console.WindowWidth;
        public int WindowWidth_
        {
            get { return WindowWidth; }
            set { WindowWidth = value; if (Console.BufferWidth < value) { Console.BufferWidth = (value * 2); } Console.WindowWidth = value; Console.SetWindowSize(WindowWidth_, WindowHeight_); }
        }
        public ConsoleColor foreColor = ConsoleColor.White;
        public ConsoleColor foreColor_
        {
            get { return foreColor; }
            set { foreColor = value; Console.ForegroundColor = value; }
        }
        public ConsoleColor backColor = ConsoleColor.Black;
        public ConsoleColor backColor_
        {
            get { return backColor; }
            set { backColor = value; Console.BackgroundColor = value; }
        }
        public void setenvironment()
        {
            environmental_variables.Add(CursorX_);
            environmental_variables.Add(CursorY_);
            environmental_variables.Add(WindowWidth_);
            environmental_variables.Add(WindowHeight_);
            environmental_variables.Add(foreColor_);
            environmental_variables.Add(backColor_);
            keyConsoleColor.Add("Black", ConsoleColor.Black);
            keyConsoleColor.Add("DarkBlue", ConsoleColor.DarkBlue);
            keyConsoleColor.Add("DarkGreen", ConsoleColor.DarkGreen);
            keyConsoleColor.Add("DarkCyan", ConsoleColor.DarkCyan);
            keyConsoleColor.Add("DarkRed", ConsoleColor.DarkRed);
            keyConsoleColor.Add("DarkMagenta", ConsoleColor.DarkMagenta);
            keyConsoleColor.Add("DarkYellow", ConsoleColor.DarkYellow);
            keyConsoleColor.Add("Gray", ConsoleColor.Gray);
            keyConsoleColor.Add("DarkGray", ConsoleColor.DarkGray);
            keyConsoleColor.Add("Blue", ConsoleColor.Blue);
            keyConsoleColor.Add("Green", ConsoleColor.Green);
            keyConsoleColor.Add("Cyan", ConsoleColor.Cyan);
            keyConsoleColor.Add("Red", ConsoleColor.Red);
            keyConsoleColor.Add("Magenta", ConsoleColor.Magenta);
            keyConsoleColor.Add("Yellow", ConsoleColor.Yellow);
            keyConsoleColor.Add("White", ConsoleColor.White);
        }

        public void parse(string input)
        {
            
            bool valid_command = false;
            string[] split_input = input.Split(" ");

            // Detect a print statement
            if (split_input[0].Equals("print", StringComparison.OrdinalIgnoreCase))
            {
                // create a new string combining all elements in split array, except for the first, then pass it to print command
                string input_to_print = "";
                foreach (string s in split_input.Skip(1))
                {
                    input_to_print = input_to_print + s + " ";
                }
                print(input_to_print);
            }
            if (split_input[0].Equals("println", StringComparison.OrdinalIgnoreCase))
            {
                // create a new string combining all elements in split array, except for the first, then pass it to print command
                string input_to_print = "";
                foreach (string s in split_input.Skip(1))
                {
                    input_to_print = input_to_print + s + " ";
                }
                print(input_to_print);
                Console.WriteLine(); // end with new line
            }
            // Detect a new variable declaration
            if (split_input[0].Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                bool no_issues = true;
                if (split_input.Length != 4)
                {
                    Console.WriteLine("Incorrect formatting to declare integer.");
                    no_issues = false;
                } else
                {
                    bool valid_name = ContainsOnlyLettersAndNumbers(split_input[1]);
                    if (valid_name == true)
                    {
                        if (split_input[2] == "=") { no_issues = true;  } else {
                            Console.WriteLine("Incorrect formatting to declare integer.");
                            no_issues = false; }
                    } else
                    {
                        Console.WriteLine("Variable names may only contain letters and numbers.");
                        no_issues = false;
                    }

                    bool proper_value = IsNumeric(split_input[3]);
                    if (proper_value == true)
                    {
                        bool name_check = LocalVariableExists(split_input[1]);
                        if (name_check == true) { Console.WriteLine($"{split_input[1]} variable exists."); no_issues = false; }
                        if (no_issues == true) { 
                        // Syntax checks out, we proceed to declare the variable
                        IntegerVariable new_int = new IntegerVariable();
                        new_int.Name = split_input[1];
                        new_int.int_value = Int32.Parse(split_input[3]);
                        new_int.Type = VariableType.Int;
                        local_variables.Add(new_int);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Incorrect formatting to declare integer. Integer cannot take value: {split_input[3]}");
                    }
                }
            }
            if (split_input[0].Equals("string", StringComparison.OrdinalIgnoreCase))
            {
                bool no_issues = true;
                if (split_input.Length <= 3)
                {
                    Console.WriteLine("Incorrect formatting to declare string.");
                    no_issues = false;
                }
                else
                {
                    bool valid_name = ContainsOnlyLettersAndNumbers(split_input[1]);
                    if (valid_name == true)
                    {
                        if (split_input[2] == "=") { no_issues = true; }
                        else
                        {
                            Console.WriteLine("Incorrect formatting to declare string.");
                            no_issues = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Variable names may only contain letters and numbers.");
                        no_issues = false;
                    }

                    bool name_check = LocalVariableExists(split_input[1]);
                    if (name_check == false)
                    {
                        // Recombine string
                        string a = "";
                        foreach (string s in split_input.Skip(3))
                        {
                            a += s + " ";
                        }
                        if (no_issues == true)
                        {
                            // Syntax checks out, we proceed to declare the variable
                            StringVariable new_string = new StringVariable();
                            new_string.Name = split_input[1];
                            new_string.Value = a;
                            new_string.Type = VariableType.String;
                            local_variables.Add(new_string);
                        }
                    } else
                    {
                        Console.WriteLine($"{split_input[1]} variable exists.");
                        no_issues = false;
                    }
                }
            }
            if (split_input[0].Equals("float", StringComparison.OrdinalIgnoreCase))
            {
                bool no_issues = true;
                if (split_input.Length != 4)
                {
                    Console.WriteLine("Incorrect formatting to declare float.");
                    no_issues = false;
                }
                else
                {
                    bool valid_name = ContainsOnlyLettersAndNumbers(split_input[1]);
                    if (valid_name == true)
                    {
                        if (split_input[2] == "=") { no_issues = true; }
                        else
                        {
                            Console.WriteLine("Incorrect formatting to declare float.");
                            no_issues = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Variable names may only contain letters and numbers.");
                        no_issues = false;
                    }

                    bool name_check = LocalVariableExists(split_input[1]);
                    bool float_check = float.TryParse(split_input[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                    if (!float_check) {
                        Console.WriteLine($"Incorrect formatting to declare float. Float cannot take value: {split_input[3]}");
                    }
                    if (name_check == false)
                    {

                        if (no_issues == true)
                        {
                            // Syntax checks out, we proceed to declare the variable
                            FloatVariable new_float = new FloatVariable();
                            new_float.Name = split_input[1];
                            new_float.float_value = float.Parse(split_input[3], CultureInfo.InvariantCulture.NumberFormat);
                            new_float.Type = VariableType.Float;
                            local_variables.Add(new_float);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{split_input[1]} variable exists.");
                        no_issues = false;
                    }
                }
            }
            // Detect a variable set
            if (split_input[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                string var_name = split_input[1];
                bool name_found = false;
                if (split_input.Length >= 3)
                {
                    foreach (LocalVariable var in local_variables)
                    {
                        if (var.Name == var_name)
                        {
                            name_found = true;
                            if (split_input[2] == "=")
                            {
                                string a = "";
                                if (split_input.Length > 3)
                                {
                                    // Recombine the string if necessary

                                    foreach (string s in  split_input.Skip(3))
                                    {
                                        a += s + " ";
                                    }
                                }
                                switch(var.Type)
                                {
                                    case VariableType.String:
                                        var.Value = SetVariableValue(a);
                                        break;
                                    case VariableType.Int:
                                        string placeholder = SetVariableValue(a);
                                        string b = ConvertNumericalVariable(placeholder);
                                        bool isnumber = IsNumeric(b);
                                        if (isnumber == true)
                                        {
                                            var.Value = b;
                                        } else
                                        {
                                            Console.WriteLine($"Output is not valid integer: {b}");
                                        }
                                        break;
                                    case VariableType.Float:
                                        string placeholder2 = SetVariableValue(a);
                                        string b_ = ConvertNumericalVariable(placeholder2);
                                        bool isfloat = float.TryParse(b_, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                                        if (isfloat == true)
                                        {
                                            var.Value = b_;
                                        } else
                                        {
                                            Console.WriteLine($"Output is not valid float: {b_}");
                                        }
                                        break;
                                    
                                }
                             
                            }
                            else
                            {
                                Console.WriteLine("Invalid formatting to set variable value.");
                            }
                        }
                    }
                    if (name_found == false)
                    {
                        Console.WriteLine($"Could not locate variable {var_name}.");
                    }
                }
            }
            // Detect environemntal variable modification
            if (split_input[0].Equals("environment", StringComparison.OrdinalIgnoreCase))
            {
                if (split_input.Length > 3 && split_input.Length < 6)
                {
                    if (split_input[1].Equals("set", StringComparison.OrdinalIgnoreCase))
                    {
                        if (split_input.Length == 4)
                        {
                            string var_name = split_input[2].ToLower();
                            switch (var_name)
                            {
                                case "height":
                                    string _num = SetVariableValue(split_input[3]);
                                    bool _valid = IsNumeric(_num);
                                    if (_valid == true)
                                    {
                                        WindowHeight_ = (Int32.Parse(_num));
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid input: {_num}");
                                    }
                                    break;
                                case "width":
                                    string _num1 = SetVariableValue(split_input[3]);
                                    bool _valid1 = IsNumeric(_num1);
                                    if (_valid1 == true)
                                    {
                                        WindowWidth_ = (Int32.Parse(_num1));
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid input: {_num1}");
                                    }
                                    break;
                                case "cursorx":
                                    string _num2 = SetVariableValue(split_input[3]);
                                    bool _valid2 = IsNumeric(_num2);
                                    if (_valid2 == true)
                                    {
                                        try
                                        {
                                            CursorX_ = (Int32.Parse(_num2));
                                        } catch (ArgumentOutOfRangeException ex)
                                        {
                                            Console.WriteLine("Cursor out of bounds.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid input: {_num2}");
                                    }
                                    break;
                                case "cursory":
                                    string _num3 = SetVariableValue(split_input[3]);
                                    bool _valid3 = IsNumeric(_num3);
                                    if (_valid3 == true)
                                    {
                                        try
                                        {
                                            CursorY_ = (Int32.Parse(_num3));
                                        } catch (ArgumentOutOfRangeException ex)
                                        {
                                            Console.WriteLine("Cursor out of bounds.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid input: {_num3}");
                                    }
                                    break;
                                case "background":
                                    if (keyConsoleColor.ContainsKey(split_input[3]))
                                    {
                                        ConsoleColor color = keyConsoleColor[split_input[3]];
                                        backColor_ = color;
                                    } else
                                    {
                                        Console.WriteLine($"Color not found: {split_input[3]}");
                                    }
                                    break;
                                case "foreground":
                                    if (keyConsoleColor.ContainsKey(split_input[3]))
                                    {
                                        ConsoleColor color = keyConsoleColor[split_input[3]];
                                        foreColor_ = color;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Color not found: {split_input[3]}");
                                    }
                                    break;
                                default:
                                    Console.WriteLine($"{var_name} is invalid environmental variable.");
                                    break;
                            }
                        } else
                        {
                            Console.WriteLine("Invalid format.");
                        }
                    }
                } else
                {
                    Console.WriteLine("Invalid format to modify environment.");
                }
            }
        }

        public string SetVariableValue(string input)
        {
            string a = "";
            bool capturing = false;
            StringBuilder capturedText = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (currentChar == '[')
                {
                    capturing = true;
                    continue;
                }
                else if (currentChar == ']')
                {
                    capturing = false;
                    ProcessCapturedText(capturedText.ToString());
                    capturedText.Clear();
                    continue;
                }

                if (capturing)
                {
                    capturedText.Append(currentChar);

                }

                if (capturing == false) { a = a + currentChar; }

            }

            void ProcessCapturedText(string capturedText)
            {
                // Check for variable names first
                foreach (LocalVariable var in local_variables)
                {
                    if (var.Name == capturedText)
                    {
                        a = a + var.Value;
                        
                    }
                }
                // Then check for any equations to calculate
                if (capturedText.StartsWith("Calculate:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 10);
                    string b_ = ConvertNumericalVariable(_placeholder);
                    string b = calculate.calculate_string(b_);
                    a = a + b;
                }
                if (capturedText.Equals("\\n", StringComparison.OrdinalIgnoreCase)) { a = a + "\n"; }

            }


            return a;
        }
        public void print(string input)
        {
            // Grab current state of console
            ConsoleInfo info = new ConsoleInfo();
            info.status_forecolor = foreColor_;
            info.status_backcolor = backColor_;

            bool capturing = false;
            StringBuilder capturedText = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (currentChar == '[')
                {
                    capturing = true;
                    continue;
                }
                else if (currentChar == ']')
                {
                    capturing = false;
                    ProcessCapturedText(capturedText.ToString());
                    capturedText.Clear();
                    continue;
                }

                if (capturing)
                {
                    capturedText.Append(currentChar);

                }

                if (capturing == false) { Console.Write(currentChar); }

            }

            void ProcessCapturedText(string capturedText)
            {
                // Check for variable names first
                foreach (LocalVariable var in local_variables)
                {
                    if (var.Name == capturedText)
                    {
                        Console.Write(var.Value);
                    }
                }
                // Then check for any equations to calculate
                if (capturedText.StartsWith("Calculate:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 10);
                    string a = ConvertNumericalVariable(_placeholder);
                    Console.Write(calculate.calculate_string(a));
                }
                if (capturedText.Equals("\\n", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(); }
                // Check for the foreground color
                if (capturedText.StartsWith("Color:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 6);
                    if (keyConsoleColor.ContainsKey(_placeholder))
                    {
                        ConsoleColor color = keyConsoleColor[_placeholder];
                        foreColor_ = color;
                    }
                    else
                    {
                        Console.WriteLine($"Color not found: {_placeholder}");
                    }
                }
                // Check for the background color
                if (capturedText.StartsWith("Background:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 11);
                    if (keyConsoleColor.ContainsKey(_placeholder))
                    {
                        ConsoleColor color = keyConsoleColor[_placeholder];
                        backColor_ = color;
                    }
                    else
                    {
                        Console.WriteLine($"Color not found: {_placeholder}");
                    }

                }
               
            }
            setConsoleStatus(info); // Resets the colors prior to the print statement's execution
        }
        public bool LocalVariableExists(string name)
        {
            bool exists = false;
            foreach (LocalVariable var in local_variables)
            {
                if (var.Name == name) { exists = true; break; }
            }

            return exists;
        }
        public string ConvertNumericalVariable(string input)
        {
            // Locate all bracket integers and floats within input and replace with variable's value
            // variables nested within [ ] will need to be referred to with { }
            string a = "";
            StringBuilder capturedText = new StringBuilder();
            bool capturing = false;
            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (currentChar == '{')
                {
                    capturing = true;
                    continue;
                }
                else if (currentChar == '}')
                {
                    capturing = false;
                    ProcessCapturedText(capturedText.ToString());
                    capturedText.Clear();
                    continue;
                }

                if (capturing)
                {
                    capturedText.Append(currentChar);

                }

                if (capturing == false) { a = a + currentChar; }

            }

            void ProcessCapturedText(string capturedText)
            {
                // Check for variable names first
                foreach (LocalVariable var in local_variables)
                {
                    if (var.Name == capturedText && var.Type != VariableType.String)
                    {
                        a += var.Value;
                    }
                   
                }

            }
            return a;
        }
        public static bool ContainsOnlyLettersAndNumbers(string input)
        {
            Regex regex = new Regex("^[a-zA-Z0-9]+$");
            return regex.IsMatch(input);
        }
        public static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }
    }
}