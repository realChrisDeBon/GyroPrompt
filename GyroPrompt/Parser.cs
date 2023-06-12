
using GyroPrompt.Basic_Functions;
using GyroPrompt.Basic_Objects.Variables;
using GyroPrompt.Compiler;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using System.Data.SqlTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using GyroPrompt.Basic_Objects.Component;

namespace GyroPrompt
{
    public class Parser
    {
        /// <summary>
        /// Initialize some of the basic function objects and lists. These will likely be used frequently.
        /// Some of these are also variables necessary for the smooth flow of code execution.
        /// </summary>
        
        public List<LocalVariable> local_variables = new List<LocalVariable>();
        public List<object> environmental_variables = new List<object>();
        public Calculate calculate = new Calculate();
        public RandomizeInt randomizer = new RandomizeInt();
        public ConditionChecker condition_checker = new ConditionChecker();
        
        public bool running_script = false; // Used for determining if a script is being ran
        public int current_line = 0; // Used for reading scripts
        ScriptCompiler compiler = new ScriptCompiler(); // UNDER CONSTRUCTION!

        /// <summary>
        /// Below are environmental variables. These are meant for the users to be able to interact with the console settings and modify the environment.
        /// The ConsoleInfo struct/method and keyConsoleKey IDictionary enable easier manipulation of console colors and to save current settings to be recalled.
        /// All the proceeding variables are meant to enable the users to reference them through 'set environment variable_ value'
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

        IDictionary<string, object> environmentalVars = new Dictionary<string, object>();
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
        public int ScriptDelay = 500;
        public int ScriptDelay_
        {
            get { return ScriptDelay; }
            set { ScriptDelay = value; }
        }

        // Some basic initializations for the environment
        public void setenvironment()
        {
            environmental_variables.Add(CursorX_);
            environmental_variables.Add(CursorY_);
            environmental_variables.Add(WindowWidth_);
            environmental_variables.Add(WindowHeight_);
            environmental_variables.Add(foreColor_);
            environmental_variables.Add(backColor_);
            environmental_variables.Add(ScriptDelay_);

            environmentalVars.Add("CursorX", CursorX_);
            environmentalVars.Add("CursorY", CursorY_);
            environmentalVars.Add("WindowWidth", WindowWidth_);
            environmentalVars.Add("WindowHeight", WindowHeight_);
            environmentalVars.Add("Forecolor", foreColor_);
            environmentalVars.Add("Backcolor", backColor_);
            environmentalVars.Add("ScriptDelay", ScriptDelay_);

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

            condition_checker.LoadOperations();
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
                                case "windowheight":
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
                                case "windowwidth":
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
                                case "backcolor":
                                    if (keyConsoleColor.ContainsKey(split_input[3]))
                                    {
                                        ConsoleColor color = keyConsoleColor[split_input[3]];
                                        backColor_ = color;
                                    } else
                                    {
                                        Console.WriteLine($"Color not found: {split_input[3]}");
                                    }
                                    break;
                                case "forecolor":
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
                                case "scriptdelay":
                                    string _num4 = SetVariableValue(split_input[3]);
                                    bool _valid4 = IsNumeric(_num4);
                                    if (_valid4 == true)
                                    {
                                        try
                                        {
                                            ScriptDelay = (Int32.Parse(_num4));
                                        }
                                        catch (ArgumentOutOfRangeException ex)
                                        {
                                            Console.WriteLine("Error passing value.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid input: {_num4}");
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
            
            ///<summary>
            /// Conditional statements and loops that will allow for the execution of code
            /// if a specific condition is true or false, or until a specific condition
            /// becomes true or false.
            /// Example for If:
            /// If variable = [variable2] then println correct|set variable = 0
            /// If variable's value = variable2's value, then the commands that follow will execute. Multiple commands can be ran when separated by a | pipe
            /// Example for While:
            /// </summary>
            if (split_input[0].Equals("if", StringComparison.OrdinalIgnoreCase))
            {
                string first_value = split_input[1];
                bool first_value_exists = LocalVariableExists(first_value);
                if (first_value_exists == true)
                {


                    OperatorTypes operator_ = new OperatorTypes();
                    string operator_type = split_input[2];
                    if (condition_checker.operationsDictionary.ContainsKey(operator_type)) {
                        operator_ = condition_checker.operationsDictionary[operator_type];
                        string condition_ = ""; // we're going to recompile each string until we hit a 'Then' statement
                        string proceeding_commands = "";
                        int x = 3; // This will mark when we switch from the condition to the proceeding commands
                        foreach (string b in split_input.Skip(3))
                        {
                            if (b.Equals("then", StringComparison.OrdinalIgnoreCase))
                            {
                                x++;
                                break;
                            }
                            else
                            {
                                condition_ += b + " ";
                                x++;
                            }
                        }
                        foreach (string c in split_input.Skip(x))
                        {
                            proceeding_commands += c + " ";
                            
                        }
                        //DEBUG: Console.WriteLine($"Debug: {proceeding_commands}");
                        string conditon_checkvariables = SetVariableValue(condition_).Trim();
                        //DEBUG: Console.WriteLine($"Debug: {conditon_checkvariables}");
                        string var_val = GrabVariableValue(first_value);
                        //DEBUG: Console.WriteLine($"Going to check if '{var_val}' is {operator_.ToString()} to '{conditon_checkvariables}'");
                        bool condition_is_met = false;
                        if ((operator_ == OperatorTypes.EqualTo) || (operator_ == OperatorTypes.NotEqualTo))
                        {
                            condition_is_met = condition_checker.ConditionChecked(operator_, var_val, conditon_checkvariables);
                        } else
                        {
                            // We must ensure var_cal and condition_checkvariables are numerical
                            bool a_ = IsNumeric(var_val);
                            bool b_ = IsNumeric(conditon_checkvariables);
                            if ((a_ == true) && (b_ == true))
                            {
                                condition_is_met = condition_checker.ConditionChecked(operator_, var_val, conditon_checkvariables);

                            }
                        }
                        if (condition_is_met == true)
                        {
                            string[] commands_to_execute = proceeding_commands.Split('|');
                            try
                            {
                                foreach(string command in commands_to_execute)
                                {
                                    try
                                    {
                                        parse(command);
                                    } catch
                                    {
                                        Console.WriteLine($"Error running command {command}.");
                                    }
                                }
                            } catch
                            {
                                Console.WriteLine("There was an error processing list of commands.");
                            }
                        }
                        
                    } else
                    {
                        Console.WriteLine($"{operator_type} is not a valid operator.");
                    }
                } else
                {
                    Console.WriteLine($"{first_value} does not exist.");
                }
            }

            ///<summary>
            /// Script specific commands that will only execute if running_script = true
            /// These commands only have an impact on the flow of a script file and not on
            /// code that is executed from the prompt manually.
            /// </summary>
            if (split_input[0].Equals("goto", StringComparison.OrdinalIgnoreCase))
            {
                if (running_script == true)
                {
                    if (split_input.Length == 2)
                    {
                        bool valid = IsNumeric(split_input[1]);
                        if (valid)
                        {
                            int a = Int32.Parse(split_input[1]);
                            current_line = a;
                        }
                    }
                    else { Console.WriteLine("Invalid format for line number."); }
                } else
                {
                    Console.WriteLine("Can only goto line number when running a script.");
                }
            }
            if (split_input[0].Equals("pause", StringComparison.OrdinalIgnoreCase))
            {
                if (running_script == true)
                {
                    if (split_input.Length == 2)
                    {
                        string a_ = ConvertNumericalVariable(split_input[1]);
                        bool valid = IsNumeric(a_);
                        int b_ = Int32.Parse(split_input[1]);
                        if (valid)
                        {
                            Thread.Sleep(b_);

                        } else
                        {
                            Console.WriteLine($"Invalid input: {a_}.");
                        }
                    } else
                    {
                        Console.WriteLine("Invalid format to pause.");
                    }
                }
                else
                {
                    Console.WriteLine("Can only pause when running a script.");
                }
            }

             if (split_input[0].Equals("compile", StringComparison.OrdinalIgnoreCase)) { compiler.Compile(); }
        }
        public void run(string script)
        {
            // Create backup of current environmental variables and local variables
            List<LocalVariable> local_variables_backup = new List<LocalVariable>();
            List<object> environmental_variables_backup = new List<object>();
            local_variables_backup = local_variables;
            environmental_variables_backup = environmental_variables;

            // Create backup of current console settings
            ConsoleInfo info = new ConsoleInfo();
            info.status_forecolor = foreColor_;
            info.status_backcolor = backColor_;

            running_script = true; // Tell parser we are actively running a script
            current_line = 0; // Begin at 0
            
            
            List<string> Lines = System.IO.File.ReadAllLines(script).ToList<string>(); // Create a list of string so the file can be read line-by-line
            int max_lines = Lines.Count();
            while(current_line < max_lines)
            {
                parse(Lines[current_line]);
                current_line++;
                Thread.Sleep(ScriptDelay);
            }

            // Revert to pre-script settings
            local_variables.Clear();
            environmental_variables.Clear();
            local_variables = local_variables_backup;
            environmental_variables = environmental_variables_backup;
            setConsoleStatus(info);

            running_script = false; // Tell parser we are not actively running a script
            current_line = 0; // Redundant reset
        }
        // Returns a string where all variables encapsulated in square brackets [ ] are converted to their value
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
                // Then check for a randomizer
                if (capturedText.StartsWith("RandomizeInt:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 13);
                    if (_placeholder.Contains(','))
                    {
                        string run_vars = ConvertNumericalVariable(_placeholder);
                        string[] value_ = run_vars.Split(',');
                        if (value_.Length == 2)
                        {
                            bool first_valid = IsNumeric(value_[0]);
                            bool second_valid = IsNumeric(value_[1]);
                            if (first_valid && second_valid)
                            {
                                int a_ = Int32.Parse(value_[0]);
                                int b_ = Int32.Parse(value_[1]);
                                if (a_ < b_)
                                {
                                    string random_int = randomizer.randomizeInt(value_[0], value_[1]);
                                    a = a + random_int;
                                }
                                else
                                {
                                    Console.WriteLine("First value should be greater than second value.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid numerical value.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Must have single comma separated values to define range.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Must have comma separated values to define range.");
                    }

                }
                if (capturedText.Equals("\\n", StringComparison.OrdinalIgnoreCase)) { a = a + "\n"; }

            }


            return a;
        }
        // Print message 'input' to console
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
                // Then check for a randomizer
                if (capturedText.StartsWith("RandomizeInt:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 13);
                    if (_placeholder.Contains(','))
                    {
                        string run_vars = ConvertNumericalVariable(_placeholder);
                        string[] value_ = run_vars.Split(',');
                        if (value_.Length == 2)
                        {
                            bool first_valid = IsNumeric(value_[0]);
                            bool second_valid = IsNumeric(value_[1]);
                            if (first_valid && second_valid)
                            {
                                int a_ = Int32.Parse(value_[0]);
                                int b_ = Int32.Parse(value_[1]);
                                if (a_ < b_)
                                {
                                    string random_int = randomizer.randomizeInt(value_[0], value_[1]);
                                    Console.Write(random_int);
                                } else
                                {
                                    Console.WriteLine("First value should be greater than second value.");
                                }
                            } else
                            {
                                Console.WriteLine("Invalid numerical value.");
                            }
                        } else
                        {
                            Console.WriteLine("Must have single comma separated values to define range.");
                        }
                    } else
                    {
                        Console.WriteLine("Must have comma separated values to define range.");
                    }

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
        // Check if variable 'name' exists
        public bool LocalVariableExists(string name)
        {
            bool exists = false;
            foreach (LocalVariable var in local_variables)
            {
                if (var.Name == name) { exists = true; break; }
            }
            if (environmentalVars.ContainsKey(name))
            {
                exists = true;
            }

            return exists;
        }
        // Returns string containing value of variable 'name'
        public string GrabVariableValue(string name)
        {
            string a = "";
            foreach (LocalVariable var in local_variables)
            {
                if (var.Name == name) { a = var.Value; break; }
            }
            if (environmentalVars.ContainsKey(name))
            {
                a = environmentalVars[name].ToString();
            }

            return a;
        }
        // Checks for all numerical values, including mathematical equations, and converts them to their number value
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
        // Checks if string 'input' contains only alphanumeric characters
        public static bool ContainsOnlyLettersAndNumbers(string input)
        {
            Regex regex = new Regex("^[a-zA-Z0-9]+$");
            return regex.IsMatch(input);
        }
        // Checks if string 'input' contains only numbers
        public static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }
    }
}