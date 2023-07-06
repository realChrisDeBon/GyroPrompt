
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
using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Setup;
using GyroPrompt.Basic_Objects.GUIComponents;
using Terminal.Gui;
using GyroPrompt.Basic_Functions.Object_Modifiers;
using System.Diagnostics;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

public enum objectClass
{
    Variable,
    EnvironmentalVariable,
    List
}

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
        public List<LocalList> local_arrays = new List<LocalList>();
        public List<TaskList> tasklists_inuse = new List<TaskList>();

        public Calculate calculate = new Calculate();
        public TimeDateHandler timedate_handler = new TimeDateHandler();
        public RandomizeInt randomizer = new RandomizeInt();
        public ConditionChecker condition_checker = new ConditionChecker();
        public FilesystemInterface filesystem = new FilesystemInterface();
        public DataHasher datahasher = new DataHasher();
        public DataSerializer dataserializer = new DataSerializer();
        
        public IDictionary<string, objectClass> namesInUse = new Dictionary<string, objectClass>();
        public bool running_script = false; // Used for determining if a script is being ran
        public int current_line = 0; // Used for reading scripts
        ScriptCompiler compiler = new ScriptCompiler(); // UNDER CONSTRUCTION!

        /// <summary>
        /// These variables and methods are used for handling the GUI components. If the user enables the GUI layer, due to the nature of how the Terminal.GUI NuGet
        /// package works, it will operate in an instance of a new Window and will appear on top of the regular CLI interface (until the instance of the GUI Window is
        /// terminated). In order to manage the text output of the console, we have a top level bool GUIModeEnabled. If enabled, we direct the console output to a string
        /// variable which will (Eventually) become accessible to the user via an environmental variable. This will allow the console output to be accessed and ported
        /// to GUI components (text fields, labels, etc). When the GUIModeEnabled is set to false, the console output reverts to its original state and output directly to
        /// the console like normally (Eventually).
        /// </summary>

        static TaskScheduler uiTaskScheduler;
        public bool GUIModeEnabled = false;
        public string ConsoleOutCatcher = "";
        ConsoleOutputDirector consoleDirector = new ConsoleOutputDirector();
        public IDictionary<string, GUI_BaseItem> GUIObjectsInUse = new Dictionary<string, GUI_BaseItem>();
        
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
        public IDictionary<string, ConsoleColor> keyConsoleColor = new Dictionary<string, ConsoleColor>();
        public IDictionary<string, Color> terminalColor = new Dictionary<string, Color>();
        public void setConsoleStatus(ConsoleInfo _consoleinfo)
        {
            Console.ForegroundColor = _consoleinfo.status_forecolor;
            Console.BackgroundColor = _consoleinfo.status_backcolor;
        }

        IDictionary<string, object> environmentalVars = new Dictionary<string, object>();
        // The rest are just environmental variables
        public string Title = Console.Title;
        public string Title_
        {
            get { return Title; }
            set { Title = value; Console.Title = value; }
        }
        public int CursorX = Console.CursorLeft;
        public int CursorX_
        {
            get { CursorX = Console.CursorLeft; return Console.CursorLeft; }
            set { CursorX = value; Console.CursorLeft = value; }
        }
        public int CursorY = Console.CursorTop;
        public int CursorY_
        {
            get { CursorY = Console.CursorTop; return Console.CursorTop; }
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
            environmental_variables.Add(Title_);

            environmentalVars.Add("CursorX", CursorX_);
            environmentalVars.Add("CursorY", CursorY_);
            environmentalVars.Add("WindowWidth", WindowWidth_);
            environmentalVars.Add("WindowHeight", WindowHeight_);
            environmentalVars.Add("Forecolor", foreColor_);
            environmentalVars.Add("Backcolor", backColor_);
            environmentalVars.Add("ScriptDelay", ScriptDelay_);
            environmentalVars.Add("Title", Title_);

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

            terminalColor.Add("Black", Color.Black);
            terminalColor.Add("DarkBlue", Color.Blue);
            terminalColor.Add("DarkGreen", Color.Green);
            terminalColor.Add("DarkCyan", Color.Cyan);
            terminalColor.Add("DarkRed", Color.Red);
            terminalColor.Add("DarkMagenta", Color.Magenta);
            terminalColor.Add("Gray", Color.Gray);
            terminalColor.Add("DarkGray", Color.DarkGray);
            terminalColor.Add("Blue", Color.BrightBlue);
            terminalColor.Add("Green", Color.BrightGreen);
            terminalColor.Add("Cyan", Color.BrightCyan);
            terminalColor.Add("Red", Color.BrightRed);
            terminalColor.Add("Magenta", Color.BrightMagenta);
            terminalColor.Add("White", Color.White);

            foreach (string envvar_name in environmentalVars.Keys)
            {
                namesInUse.Add(envvar_name, objectClass.EnvironmentalVariable); // All encompassing name reserve system
            }
            condition_checker.LoadOperations(); // Load enum types for operators
        }
        

        /// <summary>
        /// Parser will handle input looped as opposed to the program entry point's Main()
        /// This will allow us to get a slightly higher degree of control in the future.
        /// </summary>
        public void beginInputLoop()
        {
            
            TextWriter originOut = Console.Out;
            while (true)
            {
                if (GUIModeEnabled == true) 
                {
                    using (var writer = new StringWriter())
                    {
                            Console.SetOut(writer);
                            //Console.Write("GyroPrompt > ");
                            string command = Console.ReadLine();
                            parse(command);
                            ConsoleOutCatcher = ConsoleOutCatcher + (writer.ToString());
                        writer.Flush();
                        if (GUIModeEnabled == false) { Console.SetOut(originOut); }
                    }
                } else if (GUIModeEnabled == false) 
                {
                    if (Console.Out != originOut)
                    {
                        Console.SetOut(originOut);
                    }
                    Console.Write("GyroPrompt > ");
                    string command = Console.ReadLine();
                    parse(command);
                }
            }
        }

        public void parse(string input)
        {
            try
            {
                bool valid_command = false;

                string[] split_input = input.Split(' ');

                // Detect comment declaration
                if ((split_input[0].Equals("#", StringComparison.OrdinalIgnoreCase)) || (split_input[0].StartsWith("#", StringComparison.OrdinalIgnoreCase)))
                {
                    // Hashtags are treated like comments
                }
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
                if (split_input[0].Equals("bool", StringComparison.OrdinalIgnoreCase))
                {
                    bool no_issues = true;
                    if (split_input.Length != 4)
                    {
                        Console.WriteLine("Incorrect formatting to declare bool.");
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
                                Console.WriteLine("Incorrect formatting to declare bool.");
                                no_issues = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Variable names may only contain letters and numbers.");
                            no_issues = false;
                        }
                        bool proper_value = false;
                        split_input[3].Split();
                        string aa = SetVariableValue(split_input[3]);
                        aa.ToLower();
                        if (aa.Equals("False",StringComparison.OrdinalIgnoreCase) || aa == "0") { proper_value = true; }
                        if (aa.Equals("True", StringComparison.OrdinalIgnoreCase) || aa == "1") { proper_value = true; }


                        if (proper_value == true)
                        {
                            bool name_check = NameInUse(split_input[1]);
                            if (name_check == true) { Console.WriteLine($"{split_input[1]} name in use."); no_issues = false; }
                            if (no_issues == true)
                            {
                                // Syntax checks out, we proceed to declare the variable
                                BooleanVariable new_bool = new BooleanVariable();
                                new_bool.Name = split_input[1];
                                if (aa.Equals("False", StringComparison.OrdinalIgnoreCase) || aa == "0") { new_bool.bool_val = false; }
                                if (aa.Equals("True", StringComparison.OrdinalIgnoreCase) || aa == "1") { new_bool.bool_val = true; }
                                new_bool.Type = VariableType.Boolean;
                                local_variables.Add(new_bool);
                                namesInUse.Add(split_input[1], objectClass.Variable);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Incorrect formatting to declare bool. Bool cannot take value: {aa}");
                        }
                    }
                }
                if (split_input[0].Equals("int", StringComparison.OrdinalIgnoreCase))
                {
                    bool no_issues = true;
                    if (split_input.Length != 4)
                    {
                        Console.WriteLine("Incorrect formatting to declare integer.");
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
                                Console.WriteLine("Incorrect formatting to declare integer.");
                                no_issues = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Variable names may only contain letters and numbers.");
                            no_issues = false;
                        }
                        string a_ = SetVariableValue(split_input[3]);
                        bool proper_value = IsNumeric(a_);
                        if (proper_value == true)
                        {
                            bool name_check = NameInUse(split_input[1]);
                            if (name_check == true) { Console.WriteLine($"{split_input[1]} name in use."); no_issues = false; }
                            if (no_issues == true)
                            {
                                // Syntax checks out, we proceed to declare the variable
                                IntegerVariable new_int = new IntegerVariable();
                                new_int.Name = split_input[1];
                                new_int.int_value = Int32.Parse(a_);
                                new_int.Type = VariableType.Int;
                                local_variables.Add(new_int);
                                namesInUse.Add(split_input[1], objectClass.Variable);
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

                        bool name_check = NameInUse(split_input[1]);
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
                                namesInUse.Add(new_string.Name, objectClass.Variable);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{split_input[1]} name in use.");
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

                        bool name_check = NameInUse(split_input[1]);
                        bool float_check = float.TryParse(split_input[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                        if (!float_check)
                        {
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
                                namesInUse.Add(new_float.Name, objectClass.Variable);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{split_input[1]} name in use.");
                            no_issues = false;
                        }
                    }
                }
                // Modify variable values
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

                                        foreach (string s in split_input.Skip(3))
                                        {
                                            a += s + " ";
                                        }
                                    }
                                    switch (var.Type)
                                    {
                                        case VariableType.String:
                                            var.Value = SetVariableValue(a);
                                            break;
                                        case VariableType.Int:
                                            string placeholder = SetVariableValue(a);
                                            string b = ConvertNumericalVariable(placeholder).Trim();
                                            bool isnumber = IsNumeric(b);
                                            if (isnumber == true)
                                            {
                                                var.Value = b;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Output is not valid integer: {b}");
                                            }
                                            break;
                                        case VariableType.Float:
                                            string placeholder2 = SetVariableValue(a);
                                            string b_ = ConvertNumericalVariable(placeholder2).Trim();
                                            bool isfloat = float.TryParse(b_, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                                            if (isfloat == true)
                                            {
                                                var.Value = b_;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Output is not valid float: {b_}");
                                            }
                                            break;
                                        case VariableType.Boolean:
                                            string[] acceptableValue = { "true", "false", "1", "0" };
                                            bool validInput = false;
                                            foreach (string s in acceptableValue)
                                            {
                                                if (split_input[3].Equals(s, StringComparison.OrdinalIgnoreCase) == true)
                                                {
                                                    if ((s == "true") || (s == "1"))
                                                    {
                                                        validInput = true;
                                                        var.Value = "True";
                                                        break;
                                                    }
                                                    else if ((s == "0") || (s == "false"))
                                                    {
                                                        validInput = true;
                                                        var.Value = "False";
                                                        break;
                                                    }
                                                }
                                            }
                                            if (validInput == false)
                                            {
                                                Console.WriteLine($"Output is not valid boolean: {split_input[3]}");
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
                if (split_input[0].Equals("toggle", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string var_name = split_input[1];
                        bool valid_var = LocalVariableExists(var_name);

                        if (valid_var == true)
                        {
                            foreach (LocalVariable local_var in local_variables)
                            {
                                if ((local_var.Name == var_name) && (local_var.Type == VariableType.Boolean))
                                {
                                    if (local_var.Value == "False") { local_var.Value = "True"; } else if (local_var.Value == "True") { local_var.Value = "False"; } // Switch the values
                                }
                                else if ((local_var.Name == var_name) && (local_var.Type != VariableType.Boolean))
                                {
                                    Console.WriteLine($"{local_var.Name} is not a boolean value.");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {var_name}.");
                        }
                    }
                }
                if (split_input[0].Equals("int+", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string var_name = split_input[1];
                        bool var_exists = LocalVariableExists(var_name);
                        if (var_exists == true)
                        {
                            foreach (LocalVariable localVar in local_variables)
                            {
                                if ((localVar.Name == var_name) && (localVar.Type == VariableType.Int))
                                {
                                    int a = Int32.Parse(localVar.Value);
                                    a++; // increment it by 1
                                    localVar.Value = a.ToString();
                                }
                                else if ((localVar.Name == var_name) && (localVar.Type != VariableType.Int))
                                {
                                    Console.WriteLine($"{localVar.Name} is not an integer value.");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {var_name}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("int+ can only take 1 value.");
                    }
                }
                if (split_input[0].Equals("int-", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string var_name = split_input[1];
                        bool var_exists = LocalVariableExists(var_name);
                        if (var_exists == true)
                        {
                            foreach (LocalVariable localVar in local_variables)
                            {
                                if ((localVar.Name == var_name) && (localVar.Type == VariableType.Int))
                                {
                                    int a = Int32.Parse(localVar.Value);
                                    a--; // decrease it by 1
                                    localVar.Value = a.ToString();
                                }
                                else if ((localVar.Name == var_name) && (localVar.Type != VariableType.Int))
                                {
                                    Console.WriteLine($"{localVar.Name} is not an integer value.");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {var_name}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("int- can only take 1 value.");
                    }
                }
                // Detect environmental variable modification
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
                                        string _num = SetVariableValue(split_input[3].Trim());
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
                                        string _num1 = SetVariableValue(split_input[3].Trim());
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
                                        string _num2 = SetVariableValue(split_input[3].Trim());
                                        bool _valid2 = IsNumeric(_num2);
                                        if (_valid2 == true)
                                        {
                                            try
                                            {
                                                CursorX_ = (Int32.Parse(_num2));
                                            }
                                            catch (ArgumentOutOfRangeException ex)
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
                                        string _num3 = SetVariableValue(split_input[3].Trim());
                                        bool _valid3 = IsNumeric(_num3);
                                        if (_valid3 == true)
                                        {
                                            try
                                            {
                                                CursorY_ = (Int32.Parse(_num3));
                                            }
                                            catch (ArgumentOutOfRangeException ex)
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
                                        }
                                        else
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
                                    case "title":
                                        string a_ = SetVariableValue(split_input[3]);
                                        Console.Title = (a_);
                                        break;
                                    default:
                                        Console.WriteLine($"{var_name} is invalid environmental variable.");
                                        break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid format.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to modify environment.");
                    }
                }
                if (split_input[0].Equals("pause", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string a_ = ConvertNumericalVariable(split_input[1]);
                        bool valid = IsNumeric(a_);
                        int b_ = Int32.Parse(split_input[1]);
                        if (valid)
                        {
                            Thread.Sleep(b_);

                        }
                        else
                        {
                            Console.WriteLine($"Invalid input: {a_}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to pause.");
                    }
                }
                ///<summary>
                /// Conditional statements and loops that will allow for the execution of code
                /// if a specific condition is true or false, or until a specific condition
                /// becomes true or false.
                /// 
                /// Example for If:
                /// If variable = [variable2] then println correct|set variable = 0 else println incorrect|set variable = [variable2]
                /// If variable's value = variable2's value, then the commands 'println correct' and 'set variable = 0' execute, otherwise 'println incorrect' and 'set variable = [variable2]' will execute
                /// Multiple commands can be ran when separated by a | pipe
                /// 
                /// Example for While:
                /// While variable1 < [variable2] do pause 500|set variable1 = [Calculate:{variable1}+1]|println Var1:[variable1] Var2:[variable2]
                /// Assuming both variable1 and variable2 are integers, var1 = 0 and var2 = 5, the above code would pause for 500 miliseconds, increment var1 by 1, then print
                /// both variables side-by-side 5 times until variable1 is no longer < to [variable2]
                /// 
                /// The 'do' statement is to 'while' what the 'then' statement is to 'if'
                /// </summary>
                if (split_input[0].Equals("if", StringComparison.OrdinalIgnoreCase))
                {
                    string first_value = split_input[1];
                    bool first_value_exists = LocalVariableExists(first_value);
                    if (first_value_exists == true)
                    {
                        OperatorTypes operator_ = new OperatorTypes();
                        string operator_type = split_input[2];
                        if (condition_checker.operationsDictionary.ContainsKey(operator_type))
                        {
                            operator_ = condition_checker.operationsDictionary[operator_type];
                            string condition_ = ""; // we're going to recompile each string until we hit a 'Then' statement
                            string proceeding_commands = "";
                            bool then_exists = false;
                            int x = 3; // This will mark when we switch from the condition to the proceeding commands
                            foreach (string b in split_input.Skip(3))
                            {
                                if (b.Equals("then", StringComparison.OrdinalIgnoreCase))
                                {
                                    x++;
                                    then_exists = true;
                                    break;
                                }
                                else
                                {
                                    condition_ += b + " ";
                                    x++;
                                }
                            }
                            bool else_statement_exists = false;
                            string else_statement_commands = "";
                            foreach (string c in split_input.Skip(x))
                            {
                                if (c.Equals("else", StringComparison.OrdinalIgnoreCase))
                                {
                                    else_statement_exists = true; // toggle existence of else statement
                                }
                                else
                                {
                                    if (else_statement_exists == false)
                                    {
                                        proceeding_commands += c + " "; // this will amend what executes under 'then'
                                    }
                                    else
                                    {
                                        else_statement_commands += c + " "; // this will amend what executes under 'else'
                                    }

                                }
                            }
                            //DEBUG: Console.WriteLine($"Debug: {proceeding_commands}");
                            string conditon_checkvariables = SetVariableValue(condition_).Trim();
                            //DEBUG: Console.WriteLine($"Debug: {conditon_checkvariables}");
                            string var_val = GrabVariableValue(first_value);
                            //DEBUG: Console.WriteLine($"Going to check if '{var_val}' is {operator_.ToString()} to '{conditon_checkvariables}'");
                            bool condition_is_met = false;
                            if (then_exists == true)
                            {
                                if ((operator_ == OperatorTypes.EqualTo) || (operator_ == OperatorTypes.NotEqualTo))
                                {
                                    // We check to see if value 'a' and value 'b' are either equal to or not equal to each other
                                    condition_is_met = condition_checker.ConditionChecked(operator_, var_val, conditon_checkvariables);
                                }
                                else
                                {
                                    // We must ensure var_cal and condition_checkvariables are numerical
                                    bool a_ = IsNumeric(var_val);
                                    bool b_ = IsNumeric(conditon_checkvariables);
                                    if ((a_ == true) && (b_ == true))
                                    {
                                        // Since both are numerical, we can use an operator that compares their value by greater/less than
                                        condition_is_met = condition_checker.ConditionChecked(operator_, var_val.Trim(), conditon_checkvariables.Trim());
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Can only use numerical values for operator: {operator_type}");
                                    }
                                }
                                if (condition_is_met == true)
                                {
                                    // Each command is seperated by the vertical pipe | allowing for multiple commands to execute
                                    string[] commands_to_execute = proceeding_commands.Split('|');
                                    try
                                    {
                                        foreach (string command in commands_to_execute)
                                        {
                                            try
                                            {
                                                parse(command.TrimEnd());
                                            }
                                            catch
                                            {
                                                //Error with specific command
                                                Console.WriteLine($"Error running command {command}.");
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // General error
                                        Console.WriteLine("There was an error processing list of commands.");
                                    }
                                }
                                else if ((condition_is_met == false) && (else_statement_exists == true))
                                {
                                    // Condition was false, so we execute the 'else' statement
                                    string[] else_commands_to_execute = else_statement_commands.Split('|');
                                    try
                                    {
                                        foreach (string command in else_commands_to_execute)
                                        {
                                            try
                                            {
                                                parse(command.TrimEnd());
                                            }
                                            catch
                                            {
                                                Console.WriteLine($"Error running command {command}.");
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("There was an error processing list of commands.");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"If statements must include 'then'.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{operator_type} is not a valid operator.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{first_value} does not exist.");
                    }
                }
                if (split_input[0].Equals("while", StringComparison.OrdinalIgnoreCase))
                {
                    string first_value = split_input[1];
                    bool first_value_exists = LocalVariableExists(first_value);
                    if (first_value_exists == true)
                    {
                        OperatorTypes operator_ = new OperatorTypes();
                        string operator_type = split_input[2];
                        if (condition_checker.operationsDictionary.ContainsKey(operator_type))
                        {
                            operator_ = condition_checker.operationsDictionary[operator_type];
                            string condition_ = ""; // we're going to recompile each string until we hit a 'Do' statement
                            string proceeding_commands = "";
                            bool do_exists = false;
                            int x = 3; // This will mark when we switch from the condition to the proceeding commands
                            foreach (string b in split_input.Skip(3))
                            {
                                if (b.Equals("do", StringComparison.OrdinalIgnoreCase))
                                {
                                    x++;
                                    do_exists = true;
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
                                proceeding_commands += c + " "; // this will amend what executes under 'Do'
                            }
                            //DEBUG: Console.WriteLine($"Debug: {proceeding_commands}");
                            string conditon_checkvariables = SetVariableValue(condition_).Trim();
                            //DEBUG: Console.WriteLine($"Debug: {conditon_checkvariables}");
                            string var_val = GrabVariableValue(first_value);
                            //DEBUG: Console.WriteLine($"Going to check if '{var_val}' is {operator_.ToString()} to '{conditon_checkvariables}'");
                            //DEBUG: Thread.Sleep(2000);
                            bool condition_is_met = false; // We assume true for the while statement
                            if (do_exists == true)
                            {
                                if ((operator_ == OperatorTypes.EqualTo) || (operator_ == OperatorTypes.NotEqualTo))
                                {
                                    // We check to see if value 'a' and value 'b' are either equal to or not equal to each other
                                    // Since both are numerical, we can use an operator that compares their value by greater/less than
                                    condition_is_met = condition_checker.ConditionChecked(operator_, var_val.Trim(), conditon_checkvariables.Trim());
                                    if (condition_is_met == true)
                                    {
                                        bool keep_running = true;
                                        while (keep_running)
                                        {
                                            string updated_var1 = GrabVariableValue(first_value); // We need to update the variable
                                            string updated_var2 = SetVariableValue(condition_).Trim(); // We need to update the variable
                                            bool condition_check = condition_checker.ConditionChecked(operator_, updated_var1, updated_var2);
                                            if (condition_check == false) { keep_running = false; break; } // Redundancy never hurt anyone
                                            else
                                            {
                                                string[] commands_to_execute = proceeding_commands.Split("|");
                                                try
                                                {
                                                    foreach (string command in commands_to_execute)
                                                    {
                                                        try
                                                        {
                                                            parse(command.TrimEnd());
                                                        }
                                                        catch
                                                        {
                                                            //Error with specific command, exiting 'while' loop
                                                            Console.WriteLine($"Error running command {command}.");
                                                            break;
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    // General error, exiting 'while' loop
                                                    Console.WriteLine("There was an error processing list of commands.");
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    // We must ensure var_cal and condition_checkvariables are numerical
                                    bool a_ = IsNumeric(var_val);
                                    bool b_ = IsNumeric(conditon_checkvariables);
                                    if ((a_ == true) && (b_ == true))
                                    {
                                        condition_is_met = condition_checker.ConditionChecked(operator_, var_val, conditon_checkvariables);
                                        if (condition_is_met == true)
                                        {
                                            bool keep_running = true;
                                            while (keep_running)
                                            {
                                                string updated_var1 = GrabVariableValue(first_value); // We need to update the variable
                                                string updated_var2 = SetVariableValue(condition_).Trim(); // We need to update the variable
                                                bool condition_check = condition_checker.ConditionChecked(operator_, updated_var1, updated_var2);
                                                if (condition_check == false) { keep_running = false; break; } // Redundancy is never a bad thing
                                                else
                                                {
                                                    string[] commands_to_execute = proceeding_commands.Split('|');
                                                    try
                                                    {
                                                        foreach (string command in commands_to_execute)
                                                        {
                                                            try
                                                            {
                                                                parse(command.TrimEnd());
                                                            }
                                                            catch
                                                            {
                                                                //Error with specific command, exiting 'while' loop
                                                                Console.WriteLine($"Error running command {command}.");
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        // General error, exiting 'while' loop
                                                        Console.WriteLine("There was an error processing list of commands.");
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                    }
                                    else
                                    {
                                        Console.WriteLine($"Can only use numerical values for operator: {operator_type}");
                                    }
                                }


                            }
                            else
                            {
                                Console.WriteLine($"While statements must include 'do'.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{operator_type} is not a valid operator.");
                        }
                    }
                    else
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
                    }
                    else
                    {
                        Console.WriteLine("Can only goto line number when running a script.");
                    }
                }
                // Grab user input
                if (split_input[0].Equals("readline", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string var_ = split_input[1];
                        string prompt_ = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                        bool validvar_ = LocalVariableExists(var_);
                        if (validvar_ == true)
                        {
                            foreach (LocalVariable var in local_variables)
                            {
                                if (var.Name == var_)
                                {
                                    if (var.Type == VariableType.String)
                                    {
                                        Console.Write(prompt_);
                                        string a_ = Console.ReadLine();
                                        var.Value = a_;

                                    } else
                                    {
                                        Console.WriteLine($"{var_} is not a string.");
                                        break;
                                    }

                                }
                            }
                        } else
                        {
                            Console.WriteLine($"Could not locate variable {var_}");
                        }
                    } else
                    {
                        Console.WriteLine("Invalid format for readline.");
                    }
                }
                if (split_input[0].Equals("readkey", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string var_ = split_input[1];
                        string prompt_ = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                        bool validvar_ = LocalVariableExists(var_);
                        if (validvar_ == true)
                        {
                            foreach (LocalVariable var in local_variables)
                            {
                                if (var.Name == var_)
                                {
                                    if (var.Type == VariableType.String)
                                    {
                                        Console.Write(prompt_);
                                        ConsoleKeyInfo ck = Console.ReadKey();
                                        string a_ = ck.KeyChar.ToString();
                                        if (ck.Key == ConsoleKey.DownArrow)
                                        {
                                            a_ = "DownArrow";
                                        }
                                        if (ck.Key == ConsoleKey.UpArrow)
                                        {
                                            a_ = "UpArrow";
                                        }
                                        if (ck.Key == ConsoleKey.LeftArrow)
                                        {
                                            a_ = "LeftArrow";
                                        }
                                        if (ck.Key == ConsoleKey.RightArrow)
                                        {
                                            a_ = "RightArrow";
                                        }
                                        var.Value = a_;

                                    }
                                    else
                                    {
                                        Console.WriteLine($"{var_} is not a string.");
                                        break;
                                    }

                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {var_}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for readkey.");
                    }
                }
                if (split_input[0].Equals("readint", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string var_ = split_input[1];

                        string[] prompt_ = new string[2];
                        int separatorIndex = Array.IndexOf(split_input, "|");

                        if (separatorIndex >= 0)
                        {
                            prompt_[0] = string.Join(" ", split_input.Skip(2).Take(separatorIndex - 2));
                            prompt_[1] = string.Join(" ", split_input.Skip(separatorIndex + 1));
                        }
                        else
                        {
                            prompt_[0] = string.Join(" ", split_input.Skip(2));
                        }


                        bool validvar_ = LocalVariableExists(var_);
                        if (validvar_ == true)
                        {
                            foreach (LocalVariable var in local_variables)
                            {
                                if (var.Name == var_)
                                {
                                    if ((var.Type == VariableType.Int) || (var.Type == VariableType.Float))
                                    {
                                        bool validUserInput = false;
                                        while (validUserInput == false)
                                        {
                                            Console.Write(prompt_[0]);
                                            string a = Console.ReadLine();
                                            bool ok = IsNumeric(a);
                                            if (ok == true)
                                            {
                                                validUserInput = true;
                                                var.Value = a;
                                                break;
                                            } else if (ok == false)
                                            {
                                                Console.WriteLine(prompt_[1]);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"{var_} is not a string.");
                                        break;
                                    }

                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {var_}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for readline.");
                    }
                }
                // Check for hash or serialize
                if (split_input[0].Equals("hash256", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string inputVar = split_input[1];
                        bool isValidVar = LocalVariableExists(inputVar);
                        if (isValidVar == true)
                        {
                            string prompt = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                            string hashedprompt = datahasher.CalculateHash(prompt);
                            foreach (LocalVariable variable in local_variables)
                            {
                                if (variable.Name == inputVar)
                                {
                                    if (variable.Type == VariableType.String)
                                    {
                                        variable.Value = hashedprompt;
                                    } else
                                    {
                                        Console.WriteLine("Variable must be string.");
                                    }
                                }
                            }
                        } else
                        {
                            Console.WriteLine($"Could not locate variable {inputVar}");
                        }

                    } else
                    {
                        Console.WriteLine("Invalid format to hash.");
                    }
                }
                if (split_input[0].Equals("json_serialize", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        string inputVar = split_input[1];
                        string objToSerialize = split_input[2];
                        bool isValidVar = LocalVariableExists(inputVar);
                        bool isValidObj = NameInUse(objToSerialize);

                        if (isValidVar == true)
                        {
                            if (isValidObj == true)
                            {
                                string serializedprompt = "";
                                switch (namesInUse[objToSerialize])
                                {
                                    case (objectClass.Variable):
                                        foreach (LocalVariable variable in local_variables)
                                        {
                                            if (variable.Name == objToSerialize)
                                            {
                                                serializedprompt += dataserializer.serializeInput(variable);
                                            }
                                        }
                                        break;
                                    case (objectClass.List):
                                        foreach (LocalList list in local_arrays)
                                        {
                                            if (list.Name == objToSerialize)
                                            {
                                                serializedprompt += dataserializer.serializeInput(list);
                                            }
                                        }
                                        break;
                                }

                                foreach (LocalVariable variable in local_variables)
                                {
                                    if (variable.Name == inputVar)
                                    {
                                        if (variable.Type == VariableType.String)
                                        {
                                            variable.Value = serializedprompt;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Variable must be string.");
                                        }
                                    }
                                }
                            } else
                            {
                                Console.WriteLine($"Could not locate object {objToSerialize}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {inputVar}");
                        }

                    }
                    else
                    {
                        Console.WriteLine("Invalid format to serialize.");
                    }
                }
                if (split_input[0].Equals("json_deserialize", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        string inputVar = split_input[1];
                        string objToSerialize = split_input[2];
                        bool isValidVar = LocalVariableExists(inputVar);
                        bool isValidObj = NameInUse(objToSerialize);

                        if (isValidVar == true)
                        {
                            if (isValidObj == true)
                            {
                                string serializedprompt = "";
                                switch (namesInUse[objToSerialize])
                                {
                                    case (objectClass.Variable):
                                        foreach (LocalVariable variable in local_variables)
                                        {
                                            if (variable.Name == objToSerialize)
                                            {
                                                serializedprompt += dataserializer.serializeInput(variable);
                                            }
                                        }
                                        break;
                                    case (objectClass.List):
                                        foreach (LocalList list in local_arrays)
                                        {
                                            if (list.Name == objToSerialize)
                                            {
                                                serializedprompt += dataserializer.serializeInput(list);
                                            }
                                        }
                                        break;
                                }

                                foreach (LocalVariable variable in local_variables)
                                {
                                    if (variable.Name == inputVar)
                                    {
                                        if (variable.Type == VariableType.String)
                                        {
                                            variable.Value = serializedprompt;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Variable must be string.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Could not locate object {objToSerialize}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {inputVar}");
                        }

                    }
                    else
                    {
                        Console.WriteLine("Invalid format to serialize.");
                    }
                }

                ///<summary>
                /// GUI items can transform the application into a more robust Terminal User Interface. GUI items will only
                /// display when top level bool GUIModeOn is set to true. 
                /// 
                /// Brief synopsis of syntax so far (may change)
                /// gui_mode on/gui_mode off/gui_mode reset                        <- toggle GUIModeEnabled bool
                /// new_gui_item button name tasklist x y width height            <- creates button named 'name' (name also becomes default text) which will execute tasklist when pressed, positioned at x, y with width, height
                /// new_gui_item textfield name x y width height bool text bool  <- creates textfield named 'name', positioned at x,y width and height, bool multiline, text, bool readonly
                /// new_gui_item menubar name list:menuitems list:tasklist       <- creates a menubar where each List becomes a menubar and each tasklist is matched to the menuitem where the text matches the tasklist's name
                /// gui_item_setwidth name fillvalue number                     <- sets width of object 'name'. FillValue: Percent (number becomes percent), Number (number becomes width value), Fill (number ignored, object will auto fill)
                /// gui_item_setheight name fillvalue number                   <- sets height of object 'name'. FillValue: Percent (number becomes percent), Number (number becomes height value), Fill (number ignored, object will auto fill)
                /// gui_item_gettext name variable                            <- sets value of 'variable' to object 'name' text
                /// gui_item_settext name value[...]                         <- sets text of object 'name' to value (reads like a string)
                /// </summary>
                if (split_input[0].Equals("gui_mode", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        consoleDirector.runningPermision = true;
                        GUIModeEnabled = true;
                        consoleDirector.InitializeGUIWindow();
                    }
                    else if (split_input[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                            TurnGUIModeOff();
                    } else if (split_input[1].Equals("reset", StringComparison.OrdinalIgnoreCase))
                    {
                        resetView();
                    }
                    void resetView()
                    {
                        while (GUIModeEnabled == true)
                        {
                            GUIModeEnabled = false;
                        }
                        consoleDirector.runningPermision = false;
                    }
                    void TurnGUIModeOff()
                    {
                        try
                        {
                            Terminal.Gui.Application.Shutdown();
                            Terminal.Gui.Application.RequestStop();
                        }
                        catch
                        {
                            // An exception is expected to be thrown but we're just going to ignore that little shitter for now

                        }
                    }
                }
                if (split_input[0].Equals("new_gui_item", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input[1].Equals("Button", StringComparison.OrdinalIgnoreCase))
                    {
                        // new_gui_item button Buttontext Tasklist x y width height
                        // This will create a new GUI button named 'Buttontext', when clicked will execute the tasklist 'Tasklist' by name
                        // The button's x y coordinates and width height are taken as the last 4 parameters (all integers)

                        if (split_input.Length >= 4)
                        {
                            //new_gui_item Button name Taslklist
                            bool nameinuse = GUIObjectsInUse.ContainsKey(split_input[2]);
                            string btnName = split_input[2];
                            string assignedTask = split_input[3];
                            bool validTask = false;
                            if (nameinuse == false)
                            {
                                foreach (TaskList tsklist in tasklists_inuse)
                                {
                                    if (tsklist.taskName == assignedTask)
                                    {
                                        int x = 0;
                                        int y = 0;
                                        int wid = 4;
                                        int hei = 2;
                                        string text = "Button";
                                        Color backgrn = Color.Black;
                                        Color foregrn = Color.White;
                                        validTask = true;
                                        bool extracting = false;
                                        foreach(string s in split_input.Skip(4))
                                        {
                                            if (extracting == true)
                                            {
                                                string q = SetVariableValue(s);
                                                foreach (char  c in q)
                                                {
                                                    if (c != '|')
                                                    {
                                                        text += c;
                                                    } else
                                                    {
                                                        extracting = false;
                                                    }
                                                }
                                                if(extracting == true)
                                                {
                                                    text += " ";
                                                }
                                            }
                                            else
                                            {
                                                if (s.StartsWith("XY:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string _placeholder = s.Remove(0, 3);
                                                    string a = ConvertNumericalVariable(_placeholder);
                                                    string[] b = a.Split(',');
                                                    if (b.Length == 2)
                                                    {
                                                        bool validx = IsNumeric(b[0]);
                                                        bool validy = IsNumeric(b[1]);
                                                        if (validx == true)
                                                        {
                                                            if (validy == true)
                                                            {
                                                                x = Int32.Parse(b[0]);
                                                                y = Int32.Parse(b[1]);
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Console.WriteLine($"Invalid value for X: {b[0]}");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Expecting X coordinate and Y coordinate separated by comma.");
                                                    }
                                                }
                                                if (s.StartsWith("HW:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string _placeholder = s.Remove(0, 3);
                                                    string a = ConvertNumericalVariable(_placeholder);
                                                    string[] b = a.Split(',');
                                                    if (b.Length == 2)
                                                    {
                                                        bool validh
                                                            = IsNumeric(b[0]);
                                                        bool validw = IsNumeric(b[1]);
                                                        if (validh == true)
                                                        {
                                                            if (validw == true)
                                                            {
                                                                hei = Int32.Parse(b[0]);
                                                                wid = Int32.Parse(b[1]);
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Console.WriteLine($"Invalid value for X: {b[0]}");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Expecting height and width separated by comma.");
                                                    }
                                                }
                                                if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string _placeholder = s.Remove(0, 5);
                                                    extracting = true;
                                                    text = _placeholder + " ";
                                                }
                                                if (s.StartsWith("Textcolor:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string _placeholder = s.Remove(0, 10);
                                                    string a = ConvertNumericalVariable(_placeholder);
                                                    if (terminalColor.ContainsKey(a))
                                                    {
                                                        foregrn = terminalColor[a];
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"{a} is not valid color.");
                                                    }
                                                }
                                                if (s.StartsWith("Backcolor:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string _placeholder = s.Remove(0, 10);
                                                    string a = ConvertNumericalVariable(_placeholder);
                                                    if (terminalColor.ContainsKey(a))
                                                    {
                                                        backgrn = terminalColor[a];
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"{a} is not valid color.");
                                                    }
                                                }
                                            }
                                        }
                                        GUI_Button newbutton = new GUI_Button(this, btnName, tsklist, text, x, y, wid, hei, foregrn, backgrn);
                                        consoleDirector.GUIButtonsToAdd.Add(newbutton);
                                        GUIObjectsInUse.Add(btnName, newbutton);
                                        break;
                                    }
                                }
                                if (validTask == false)
                                {
                                    Console.WriteLine($"Task named {assignedTask} not found");
                                }
                            } else
                            {
                                Console.WriteLine($"{btnName} name in use.");
                            }

                        } else
                        {
                            Console.WriteLine("Invalid format for GUI button.");
                        }

                    }
                    if (split_input[1].Equals("Textfield", StringComparison.OrdinalIgnoreCase))

                    {
                        if (split_input.Length >= 3)
                        {
                            bool validName = GUIObjectsInUse.ContainsKey(split_input[2]);
                            string txtFieldName = split_input[2];
                            if (validName == false)
                            {
                                int x = 0;
                                int y = 0;
                                int wid = 20;
                                int hei = 20;
                                string text = "Default text";
                                bool isMultiline = true;
                                bool isReadonly = false;
                                bool extracting = false;
                                Color backgrn = Color.Black;
                                Color foregrn = Color.White;

                                foreach (string s in split_input)
                                {
                                    if (extracting == true)
                                    {
                                        string q = SetVariableValue(s);
                                        foreach (char c in q)
                                        {
                                            if (c != '|')
                                            {
                                                text += c;
                                            }
                                            else
                                            {
                                                extracting = false;
                                            }
                                        }
                                        if (extracting == true)
                                        {
                                            text += " ";
                                        }
                                    }
                                    else
                                    {
                                        if (s.StartsWith("XY:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 3);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length == 2)
                                            {
                                                bool validx = IsNumeric(b[0]);
                                                bool validy = IsNumeric(b[1]);
                                                if (validx == true)
                                                {
                                                    if (validy == true)
                                                    {
                                                        x = Int32.Parse(b[0]);
                                                        y = Int32.Parse(b[1]);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Invalid value for X: {b[0]}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting X coordinate and Y coordinate separated by comma.");
                                            }
                                        }
                                        if (s.StartsWith("HW:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 3);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length == 2)
                                            {
                                                bool validh = IsNumeric(b[0]);
                                                bool validw = IsNumeric(b[1]);
                                                if (validh == true)
                                                {
                                                    if (validw == true)
                                                    {
                                                        hei = Int32.Parse(b[0]);
                                                        wid = Int32.Parse(b[1]);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Invalid value for X: {b[0]}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting height and width separated by comma.");
                                            }
                                        }
                                        if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 5);
                                            extracting = true;
                                            text = _placeholder + " ";
                                        }
                                        if (s.StartsWith("Multiline:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string q = SetVariableValue(_placeholder).TrimEnd();
                                            string[] validValues = { "False", "false", "0", "True", "true", "1" };
                                            int val = 0;
                                            bool correctValue = false;
                                            for(val = 0; val < validValues.Length; val++)
                                            {
                                                if (q == validValues[val])
                                                {
                                                    if (val <= 2)
                                                    {
                                                        isMultiline = false;
                                                    } else if (val >= 3)
                                                    {
                                                        isMultiline = true;
                                                    }
                                                    correctValue = true;
                                                    break;
                                                }
                                            }
                                            if (correctValue == true)
                                            {

                                            } else
                                            {
                                                Console.WriteLine($"Invalid input: {q}. Expecting bool.");
                                            }
                                        }
                                        if (s.StartsWith("Readonly:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 9);
                                            string q = SetVariableValue(_placeholder).TrimEnd();
                                            string[] validValues = { "False", "false", "0", "True", "true", "1" };
                                            int val = 0;
                                            bool correctValue = false;
                                            for (val = 0; val < validValues.Length; val++)
                                            {
                                                if (q == validValues[val])
                                                {
                                                    if (val <= 2)
                                                    {
                                                        isReadonly = false;
                                                    }
                                                    else if (val >= 3)
                                                    {
                                                        isReadonly = true;
                                                    }
                                                    correctValue = true;
                                                    break;
                                                }
                                            }
                                            if (correctValue == true)
                                            {

                                            }
                                            else
                                            {
                                                Console.WriteLine($"Invalid input: {q}. Expecting bool.");
                                            }
                                        }
                                        if (s.StartsWith("Textcolor:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            if (terminalColor.ContainsKey(a))
                                            {
                                                foregrn = terminalColor[a];
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{a} is not valid color.");
                                            }
                                        }
                                        if (s.StartsWith("Backcolor:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            if (terminalColor.ContainsKey(a))
                                            {
                                                backgrn = terminalColor[a];
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{a} is not valid color.");
                                            }
                                        }
                                    }
                                }

                                GUI_textfield newtextfield = new GUI_textfield(txtFieldName, x, y, wid, hei, isMultiline, text, isReadonly, foregrn, backgrn);
                                consoleDirector.GUITextFieldsToAdd.Add(newtextfield);
                                GUIObjectsInUse.Add(newtextfield.GUIObjName, newtextfield);

                            } else
                            {
                                Console.WriteLine($"{txtFieldName} name in use.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid format for GUI text field.");
                        }
                    }
                    if (split_input[1].Equals("Menubar", StringComparison.OrdinalIgnoreCase))
                    {
                        if (split_input.Length >= 3)
                        {
                            bool minimumList = false;
                            string menuBarName = split_input[2];
                            List<LocalList> menuItemsToPass = new List<LocalList>();
                            List<TaskList> taskListToPass = new List<TaskList>();
                            bool validName = GUIObjectsInUse.ContainsKey(menuBarName);
                            if (validName == false)
                            {
                                foreach (string s in split_input.Skip(3))
                                {
                                    if (s.StartsWith("Menuitems:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 10);
                                        string[] _listname = _placeholder.Split(',');
                                        foreach(string r in _listname)
                                        {
                                            LocalList newmenu = local_arrays.Find(j => j.Name == r.TrimEnd());
                                            if (newmenu != null)
                                            {
                                                if (newmenu.arrayType == ArrayType.String)
                                                {
                                                    menuItemsToPass.Add(newmenu);
                                                    minimumList = true;
                                                } else
                                                {
                                                    Console.WriteLine($"{newmenu.Name} is not string list.");
                                                }
                                            }
                                        }
                                    }
                                    if (s.StartsWith("Menutasks:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 10);
                                        string[] _listname = _placeholder.Split(',');
                                        foreach (string r in _listname)
                                        {
                                            TaskList newtasklist = tasklists_inuse.Find(j => j.taskName == r.TrimEnd());
                                            if (newtasklist != null)
                                            {
                                                taskListToPass.Add(newtasklist);
                                            }
                                        }
                                    }

                                }
                                if (minimumList == true)
                                {
                                    GUI_Menubar newmenubar = new GUI_Menubar(this, menuBarName, menuItemsToPass, taskListToPass);
                                    consoleDirector.GUIMenuBarsToAdd.Add(newmenubar);
                                    GUIObjectsInUse.Add(newmenubar.GUIObjName, newmenubar);
                                } else
                                {
                                    Console.WriteLine("Menubar requires minimum 1 list with 1 item.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"{menuBarName} name in use.");
                            }

                        } else
                        {
                            Console.WriteLine("Invalid format for GUI menubar.");
                        }
                    }
                    if (split_input[1].Equals("Label", StringComparison.OrdinalIgnoreCase))
                    {
                        if (split_input.Length >= 3)
                        {
                            bool nameinuse = GUIObjectsInUse.ContainsKey(split_input[2]);
                            string labelName = split_input[2];
                            if (nameinuse == false)
                            {
                                int x = 0;
                                int y = 0;
                                int wid = 4;
                                int hei = 1;
                                string text = "Label";
                                Color backgrn = Color.Black;
                                Color foregrn = Color.White;
                                bool extracting = false;
                                foreach (string s in split_input.Skip(3))
                                {
                                    if (extracting == true)
                                    {
                                        string q = SetVariableValue(s);
                                        foreach (char c in q)
                                        {
                                            if (c != '|')
                                            {
                                                text += c;
                                            }
                                            else
                                            {
                                                extracting = false;
                                            }
                                        }
                                        if (extracting == true)
                                        {
                                            text += " ";
                                        }
                                    }
                                    else
                                    {
                                        if (s.StartsWith("XY:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 3);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length == 2)
                                            {
                                                bool validx = IsNumeric(b[0]);
                                                bool validy = IsNumeric(b[1]);
                                                if (validx == true)
                                                {
                                                    if (validy == true)
                                                    {
                                                        x = Int32.Parse(b[0]);
                                                        y = Int32.Parse(b[1]);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Invalid value for X: {b[0]}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting X coordinate and Y coordinate separated by comma.");
                                            }
                                        }
                                        if (s.StartsWith("HW:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 3);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length == 2)
                                            {
                                                bool validh
                                                    = IsNumeric(b[0]);
                                                bool validw = IsNumeric(b[1]);
                                                if (validh == true)
                                                {
                                                    if (validw == true)
                                                    {
                                                        hei = Int32.Parse(b[0]);
                                                        wid = Int32.Parse(b[1]);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Invalid value for X: {b[0]}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting height and width separated by comma.");
                                            }
                                        }
                                        if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 5);
                                            extracting = true;
                                            text = _placeholder + " ";
                                        }
                                        if (s.StartsWith("Textcolor:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            if (terminalColor.ContainsKey(a.TrimEnd()))
                                            {
                                                foregrn = terminalColor[a.TrimEnd()];
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{a} is not valid color.");
                                            }
                                        }
                                        if (s.StartsWith("Backcolor:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            if (terminalColor.ContainsKey(a.TrimEnd()))
                                            {
                                                backgrn = terminalColor[a.TrimEnd()];
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{a} is not valid color.");
                                            }
                                        }
                                    }
                                }
                                GUI_Label newlabel = new GUI_Label(labelName, text, x, y, wid, hei, foregrn, backgrn);
                                consoleDirector.GUILabelsToAdd.Add(newlabel);
                                GUIObjectsInUse.Add(newlabel.GUIObjName, newlabel);

                            }
                            else
                            {
                                Console.WriteLine($"{labelName} name in use.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid format for GUI label.");
                        }
                    }
                    if (split_input[1].Equals("Checkbox", StringComparison.OrdinalIgnoreCase))
                    {
                        if (split_input.Length >= 3)
                        {
                            string expectedName = split_input[2];
                            bool nameInUse = GUIObjectsInUse.ContainsKey(expectedName);
                            if (nameInUse == false)
                            {
                                int x = 0;
                                int y = 0;
                                int wid = 4;
                                int hei = 1;
                                string text = "Checkbox";
                                Color backgrn = Color.Black;
                                Color foregrn = Color.White;
                                bool isChecked = false;
                                bool hasLinkedBools = false;
                                List<string> listOfBools_ = new List<string>();
                                bool extracting = false;

                                foreach (string s in split_input)
                                {
                                    if (extracting == true)
                                    {
                                        string q = SetVariableValue(s);
                                        foreach (char c in q)
                                        {
                                            if (c != '|')
                                            {
                                                text += c;
                                            }
                                            else
                                            {
                                                extracting = false;
                                            }
                                        }
                                        if (extracting == true)
                                        {
                                            text += " ";
                                        }
                                    }
                                    else
                                    {
                                        if (s.StartsWith("LinkBool:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 9);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length >= 1)
                                            {
                                                foreach (string expectedBoolVar in b)
                                                {
                                                    expectedBoolVar.TrimEnd(); // Redundancy
                                                    expectedBoolVar.TrimStart(); // Redundancy
                                                    LocalVariable temp = local_variables.Find(z => z.Name == expectedBoolVar.TrimEnd());
                                                    if (temp != null)
                                                    {
                                                        if (temp.Type == VariableType.Boolean)
                                                        {
                                                            listOfBools_.Add(temp.Name);
                                                            hasLinkedBools = true;
                                                        } else
                                                        {
                                                            Console.WriteLine($"{expectedBoolVar} is not a bool.");
                                                        }
                                                    } else
                                                    {
                                                        Console.WriteLine($"Could not locate variable {expectedBoolVar}");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting atleast 1 bool value.");
                                            }
                                        }
                                        if (s.StartsWith("XY:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 3);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length == 2)
                                            {
                                                bool validx = IsNumeric(b[0]);
                                                bool validy = IsNumeric(b[1]);
                                                if (validx == true)
                                                {
                                                    if (validy == true)
                                                    {
                                                        x = Int32.Parse(b[0]);
                                                        y = Int32.Parse(b[1]);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Invalid value for X: {b[0]}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting X coordinate and Y coordinate separated by comma.");
                                            }
                                        }
                                        if (s.StartsWith("HW:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 3);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            string[] b = a.Split(',');
                                            if (b.Length == 2)
                                            {
                                                bool validh = IsNumeric(b[0]);
                                                bool validw = IsNumeric(b[1]);
                                                if (validh == true)
                                                {
                                                    if (validw == true)
                                                    {
                                                        hei = Int32.Parse(b[0]);
                                                        wid = Int32.Parse(b[1]);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Invalid value for Y: {b[1]}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Invalid value for X: {b[0]}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Expecting height and width separated by comma.");
                                            }
                                        }
                                        if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 5);
                                            extracting = true;
                                            text = _placeholder + " ";
                                        }
                                        if (s.StartsWith("Checked:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 9);
                                            string q = SetVariableValue(_placeholder).TrimEnd();
                                            string[] validValues = { "False", "false","No", "no", "0", "True", "true","Yes","yes", "1" };
                                            int val = 0;
                                            bool correctValue = false;
                                            for (val = 0; val < validValues.Length; val++)
                                            {
                                                if (q.Equals(validValues[val], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (val <= 4)
                                                    {
                                                        isChecked = false;
                                                    }
                                                    else if (val >= 5)
                                                    {
                                                        isChecked = true;
                                                    }
                                                    correctValue = true;
                                                    break;
                                                }
                                            }
                                            if (correctValue == true)
                                            {

                                            }
                                            else
                                            {
                                                Console.WriteLine($"Invalid input: {q}. Expecting bool.");
                                            }
                                        }
                                        if (s.StartsWith("Textcolor:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            if (terminalColor.ContainsKey(a))
                                            {
                                                foregrn = terminalColor[a];
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{a} is not valid color.");
                                            }
                                        }
                                        if (s.StartsWith("Backcolor:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
                                            string a = ConvertNumericalVariable(_placeholder);
                                            if (terminalColor.ContainsKey(a))
                                            {
                                                backgrn = terminalColor[a];
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{a} is not valid color.");
                                            }
                                        }
                                    }
                                }
                                GUI_Checkbox newcheckbox = new GUI_Checkbox(this, expectedName, text, x, y, wid, hei, isChecked, hasLinkedBools, listOfBools_, foregrn, backgrn);
                                consoleDirector.GUICheckboxToAdd.Add(newcheckbox);
                                GUIObjectsInUse.Add(expectedName, newcheckbox);

                            } else
                            {
                                Console.WriteLine($"{expectedName} name in use.");
                            }
                        } else
                        {
                            Console.WriteLine("Invalid format for GUI checkbox.");
                        }
                    }
                }
                if (split_input[0].Equals("gui_item_setwidth"))
                {
                    if (split_input.Length == 4)
                    {
                        string guiObjectName = split_input[1];
                        if (GUIObjectsInUse.ContainsKey(guiObjectName))
                        {
                            GUIObjectType guiobjecttype = GUIObjectsInUse[guiObjectName].GUIObjectType;

                            string fv = split_input[2].ToLower();
                            bool validFill = false;
                            coordVal filval = coordVal.Number;
                            switch (fv)
                            {
                                case "number":
                                    validFill = true;
                                    filval = coordVal.Number;
                                    break;
                                case "percent":
                                    validFill = true;
                                    filval = coordVal.Percentage;
                                    break;
                                case "fill":
                                    validFill = true;
                                    filval = coordVal.Fill;
                                    break;
                                default:
                                    validFill = false;
                                    break;
                            }
                            if (validFill == true)
                            {
                                bool validNumber = IsNumeric(split_input[3]);
                                int xx = Int32.Parse(split_input[3]);
                                if (validNumber == true)
                                {
                                    bool foundAndChangedWidth = false;
                                    switch (guiobjecttype)
                                    {
                                        case GUIObjectType.Button:
                                            foreach (GUI_Button guibtn in consoleDirector.GUIButtonsToAdd)
                                            {
                                                if (guibtn.GUIObjName == guiObjectName)
                                                {
                                                    guibtn.SetWidth(xx, filval);
                                                    foundAndChangedWidth = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Textfield:
                                            foreach (GUI_textfield guitxt in consoleDirector.GUITextFieldsToAdd)
                                            {
                                                if (guitxt.GUIObjName == guiObjectName)
                                                {
                                                    guitxt.SetWidth(xx, filval);
                                                    foundAndChangedWidth = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Label:
                                            foreach (GUI_Label guilbl in consoleDirector.GUILabelsToAdd)
                                            {
                                                if (guilbl.GUIObjName == guiObjectName)
                                                {
                                                    guilbl.SetWidth(xx, filval);
                                                    foundAndChangedWidth = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Checkbox:
                                            foreach (GUI_Checkbox guichbx in consoleDirector.GUICheckboxToAdd)
                                            {
                                                if (guichbx.GUIObjName == guiObjectName)
                                                {
                                                    guichbx.SetWidth(xx, filval);
                                                    foundAndChangedWidth = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        default:
                                            // Object not found but somehow a quantum misfire of code happened and we ended up here
                                            foundAndChangedWidth = false;
                                            break;
                                    }

                                    if (foundAndChangedWidth == false)
                                    {
                                        Console.WriteLine("Object type cannot accept argument for setwidth.");
                                    }

                                }
                                else
                                {
                                    Console.WriteLine($"Invalid input: {split_input[3]}. Expected integer.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Invalid format: {fv}. Expected: Percent, Fill, Number");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate GUI object {guiObjectName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to set width.");
                    }
                }
                if (split_input[0].Equals("gui_item_setheight"))
                {
                    if (split_input.Length == 4)
                    {
                        string guiObjectName = split_input[1];
                        if (GUIObjectsInUse.ContainsKey(guiObjectName))
                        {
                            GUIObjectType guiobjecttype = GUIObjectsInUse[guiObjectName].GUIObjectType;
                            string fv = split_input[2].ToLower();
                            bool validFill = false;
                            coordVal filval = coordVal.Number;
                            switch (fv)
                            {
                                case "number":
                                    validFill = true;
                                    filval = coordVal.Number;
                                    break;
                                case "percent":
                                    validFill = true;
                                    filval = coordVal.Percentage;
                                    break;
                                case "fill":
                                    validFill = true;
                                    filval = coordVal.Fill;
                                    break;
                                default:
                                    validFill = false;
                                    break;
                            }
                            if (validFill == true)
                            {
                                bool validNumber = IsNumeric(split_input[3]);
                                int xx = Int32.Parse(split_input[3]);
                                if (validNumber == true)
                                {
                                    bool foundAndChangedHeight = false;
                                    switch (guiobjecttype)
                                    {
                                        case GUIObjectType.Button:
                                            foreach (GUI_Button guibtn in consoleDirector.GUIButtonsToAdd)
                                            {
                                                if (guibtn.GUIObjName == guiObjectName)
                                                {
                                                    guibtn.SetHeight(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Textfield:
                                            foreach (GUI_textfield guitxt in consoleDirector.GUITextFieldsToAdd)
                                            {
                                                if (guitxt.GUIObjName == guiObjectName)
                                                {
                                                    guitxt.SetHeight(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Label:
                                            foreach (GUI_Label guilbl in consoleDirector.GUILabelsToAdd)
                                            {
                                                if (guilbl.GUIObjName == guiObjectName)
                                                {
                                                    guilbl.SetHeight(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Checkbox:
                                            foreach (GUI_Checkbox guichbx in consoleDirector.GUICheckboxToAdd)
                                            {
                                                if (guichbx.GUIObjName == guiObjectName)
                                                {
                                                    guichbx.SetHeight(xx, filval);
                                                    foundAndChangedHeight = true;
                                                }
                                            }
                                            break;
                                        default:
                                            // Object not found but somehow a quantum misfire of code happened and we ended up here
                                            foundAndChangedHeight = false;
                                            break;
                                    }
                                    if (foundAndChangedHeight == false)
                                    {
                                        Console.WriteLine("Object type cannot accept argument for setheight.");
                                    }

                                }
                                else
                                {
                                    Console.WriteLine($"Invalid input: {split_input[3]}. Expected integer.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Invalid format: {fv}. Expected: Percent, Fill, Number");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate GUI object {guiObjectName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to set height.");
                    }
                }
                if (split_input[0].Equals("gui_item_setx", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 4)
                    {
                        string guiObjectName = split_input[1];
                        if (GUIObjectsInUse.ContainsKey(guiObjectName))
                        {
                            GUIObjectType guiobjecttype = GUIObjectsInUse[guiObjectName].GUIObjectType;
                            string fv = split_input[2].ToLower();
                            bool validFill = false;
                            coordValue filval = coordValue.Number;
                            switch (fv)
                            {
                                case "number":
                                    validFill = true;
                                    filval = coordValue.Number;
                                    break;
                                case "percent":
                                    validFill = true;
                                    filval = coordValue.Percent;
                                    break;
                                case "center":
                                    validFill = true;
                                    filval = coordValue.Center;
                                    break;
                                case "leftof":
                                    validFill = true;
                                    filval = coordValue.LeftOf;
                                    break;
                                case "rightof":
                                    validFill = true;
                                    filval = coordValue.RightOf;
                                    break;
                                default:
                                    validFill = false;
                                    break;
                            }
                            if (validFill == true)
                            {
                                if ((filval != coordValue.RightOf) && (filval != coordValue.LeftOf))
                                {
                                    bool validNumber = IsNumeric(split_input[3]);
                                    int xx = Int32.Parse(split_input[3]);
                                    if (validNumber == true)
                                    {
                                        bool foundAndChangedHeight = false;
                                        switch (guiobjecttype)
                                        {
                                            case GUIObjectType.Button:
                                                foreach (GUI_Button guibtn in consoleDirector.GUIButtonsToAdd)
                                                {
                                                    if (guibtn.GUIObjName == guiObjectName)
                                                    {
                                                        guibtn.SetXCoord(xx, filval);
                                                        foundAndChangedHeight = true;
                                                        break;
                                                    }
                                                }
                                                break;
                                            case GUIObjectType.Textfield:
                                                foreach (GUI_textfield guitxt in consoleDirector.GUITextFieldsToAdd)
                                                {
                                                    if (guitxt.GUIObjName == guiObjectName)
                                                    {
                                                        guitxt.SetXCoord(xx, filval);
                                                        foundAndChangedHeight = true;
                                                        break;
                                                    }
                                                }
                                                break;
                                            case GUIObjectType.Label:
                                                foreach (GUI_Label guilbl in consoleDirector.GUILabelsToAdd)
                                                {
                                                    if (guilbl.GUIObjName == guiObjectName)
                                                    {
                                                        guilbl.SetXCoord(xx, filval);
                                                        foundAndChangedHeight = true;
                                                        break;
                                                    }
                                                }
                                                break;
                                            case GUIObjectType.Checkbox:
                                                foreach (GUI_Checkbox guichbx in consoleDirector.GUICheckboxToAdd)
                                                {
                                                    if (guichbx.GUIObjName == guiObjectName)
                                                    {
                                                        guichbx.SetXCoord(xx, filval);
                                                        foundAndChangedHeight = true;
                                                        break;
                                                    }
                                                }
                                                break;
                                            default:
                                                // Object not found but somehow a quantum misfire of code happened and we ended up here
                                                foundAndChangedHeight = false;
                                                break;
                                        }
                                        if (foundAndChangedHeight == false)
                                        {
                                            Console.WriteLine("Object type cannot accept argument for setx.");
                                        }

                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid input: {split_input[3]}. Expected integer.");
                                    }
                                } else
                                {
                                    string guidingObject = split_input[3];
                                    bool guidingObjExists = GUIObjectsInUse.ContainsKey(guidingObject);
                                    // Holy shit I could have just used GUIObjectType guiobj = GUIObjectsInUse[guidingObject].GUIObjectType; and skipped this retarded ass nesting I did wtf I need to fix this later
                                    if (guidingObjExists == true)
                                    {
                                        GUI_textfield textitem_ = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName ==  guidingObject);
                                        GUI_Button buttonitem_ = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == guidingObject);
                                        GUI_Label labelitem_ = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == guidingObject);
                                        GUI_Checkbox checkboxitem = consoleDirector.GUICheckboxToAdd.Find(z => z.GUIObjName == guidingObject);
                                        if (textitem_ != null)
                                        {
                                            switch (guiobjecttype)
                                            {
                                                case GUIObjectType.Button:
                                                    GUI_Button positioningButton = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningButton != null)
                                                    {
                                                        positioningButton.SetToLeftOrRight(textitem_.textView, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Textfield:
                                                    GUI_textfield positioningTextfield = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningTextfield != null)
                                                    {
                                                        positioningTextfield.SetToLeftOrRight(textitem_.textView, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Label:
                                                    GUI_Label positioningLabel = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningLabel != null)
                                                    {
                                                        positioningLabel.SetToLeftOrRight(textitem_.textView, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Checkbox:
                                                    GUI_Checkbox positioningCheckbox = consoleDirector.GUICheckboxToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningCheckbox != null)
                                                    {
                                                        positioningCheckbox.SetToLeftOrRight(textitem_.textView, filval);
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        } else if (buttonitem_!= null)
                                        {
                                            switch (guiobjecttype)
                                            {
                                                case GUIObjectType.Button:
                                                    GUI_Button positioningButton = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningButton != null)
                                                    {
                                                        positioningButton.SetToLeftOrRight(buttonitem_.newButton, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Textfield:
                                                    GUI_textfield positioningTextfield = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningTextfield != null)
                                                    {
                                                        positioningTextfield.SetToLeftOrRight(buttonitem_.newButton, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Label:
                                                    GUI_Label positioningLabel = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningLabel != null)
                                                    {
                                                        positioningLabel.SetToLeftOrRight(buttonitem_.newButton, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Checkbox:
                                                    GUI_Checkbox positioningCheckbox = consoleDirector.GUICheckboxToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningCheckbox != null)
                                                    {
                                                        positioningCheckbox.SetToLeftOrRight(buttonitem_.newButton, filval);
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        } else if (labelitem_ != null)
                                        {
                                            switch (guiobjecttype)
                                            {
                                                case GUIObjectType.Button:
                                                    GUI_Button positioningButton = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningButton != null)
                                                    {
                                                        positioningButton.SetToLeftOrRight(labelitem_.newlabel, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Textfield:
                                                    GUI_textfield positioningTextfield = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningTextfield != null)
                                                    {
                                                        positioningTextfield.SetToLeftOrRight(labelitem_.newlabel, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Label:
                                                    GUI_Label positioningLabel = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningLabel != null)
                                                    {
                                                        positioningLabel.SetToLeftOrRight(labelitem_.newlabel, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Checkbox:
                                                    GUI_Checkbox positioningCheckbox = consoleDirector.GUICheckboxToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningCheckbox != null)
                                                    {
                                                        positioningCheckbox.SetToLeftOrRight(labelitem_.newlabel, filval);
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        } else if (checkboxitem != null)
                                        {
                                            switch(guiobjecttype)
                                            {
                                                case GUIObjectType.Button:
                                                    GUI_Button positioningButton = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningButton != null)
                                                    {
                                                        positioningButton.SetToLeftOrRight(checkboxitem.newCheckbox, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Textfield:
                                                    GUI_textfield positioningTextfield = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningTextfield != null)
                                                    {
                                                        positioningTextfield.SetToLeftOrRight(checkboxitem.newCheckbox, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Label:
                                                    GUI_Label positioningLabel = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningLabel != null)
                                                    {
                                                        positioningLabel.SetToLeftOrRight(checkboxitem.newCheckbox, filval);
                                                    }
                                                    break;
                                                case GUIObjectType.Checkbox:
                                                    GUI_Checkbox positioningCheckbox = consoleDirector.GUICheckboxToAdd.Find(z => z.GUIObjName == guiObjectName);
                                                    if (positioningCheckbox != null)
                                                    {
                                                        positioningCheckbox.SetToLeftOrRight(checkboxitem.newCheckbox, filval);
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        } else
                                        {
                                            Console.WriteLine($"{guidingObject} is not a valid reference point item.");
                                        }


                                    } else
                                    {
                                        Console.WriteLine($"{guidingObject} name not in use.");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Invalid format: {fv}. Expected: Percent, Number, Center");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate GUI object {guiObjectName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to set X.");
                    }
                }
                if (split_input[0].Equals("gui_item_sety", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 4)
                    {
                        string guiObjectName = split_input[1];
                        if (GUIObjectsInUse.ContainsKey(guiObjectName))
                        {
                            GUIObjectType guiobjecttype = GUIObjectsInUse[guiObjectName].GUIObjectType;
                            string fv = split_input[2].ToLower();
                            bool validFill = false;
                            coordValue filval = coordValue.Number;
                            switch (fv)
                            {
                                case "number":
                                    validFill = true;
                                    filval = coordValue.Number;
                                    break;
                                case "percent":
                                    validFill = true;
                                    filval = coordValue.Percent;
                                    break;
                                case "center":
                                    validFill = true;
                                    filval = coordValue.Center;
                                    break;
                                default:
                                    validFill = false;
                                    break;
                            }
                            if (validFill == true)
                            {
                                bool validNumber = IsNumeric(split_input[3]);
                                int xx = Int32.Parse(split_input[3]);
                                if (validNumber == true)
                                {
                                    bool foundAndChangedHeight = false;
                                    switch (guiobjecttype)
                                    {
                                        case GUIObjectType.Button:
                                            foreach (GUI_Button guibtn in consoleDirector.GUIButtonsToAdd)
                                            {
                                                if (guibtn.GUIObjName == guiObjectName)
                                                {
                                                    guibtn.SetYCoord(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Textfield:
                                            foreach (GUI_textfield guitxt in consoleDirector.GUITextFieldsToAdd)
                                            {
                                                if (guitxt.GUIObjName == guiObjectName)
                                                {
                                                    guitxt.SetYCoord(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Label:
                                            foreach (GUI_Label guilbl in consoleDirector.GUILabelsToAdd)
                                            {
                                                if (guilbl.GUIObjName == guiObjectName)
                                                {
                                                    guilbl.SetYCoord(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        case GUIObjectType.Checkbox:
                                            foreach (GUI_Checkbox guichbx in consoleDirector.GUICheckboxToAdd)
                                            {
                                                if (guichbx.GUIObjName == guiObjectName)
                                                {
                                                    guichbx.SetYCoord(xx, filval);
                                                    foundAndChangedHeight = true;
                                                    break;
                                                }
                                            }
                                            break;
                                        default:
                                            // Object not found but somehow a quantum misfire of code happened and we ended up here
                                            foundAndChangedHeight = false;
                                            break;
                                    }
                                    if (foundAndChangedHeight == false)
                                    {
                                        Console.WriteLine("Object type cannot accept argument for sety.");
                                    }

                                }
                                else
                                {
                                    Console.WriteLine($"Invalid input: {split_input[3]}. Expected integer.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Invalid format: {fv}. Expected: Percent, Fill, Center");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate GUI object {guiObjectName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to set Y.");
                    }
                }
                if (split_input[0].Equals("gui_item_gettext", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        bool validVriable = false;
                        bool guiObjectExists = false;
                        string guiObjectName = split_input[1].TrimEnd();
                        string variableName = split_input[2].TrimEnd();
                        validVriable = LocalVariableExists(variableName);
                        guiObjectExists = GUIObjectsInUse.ContainsKey(guiObjectName);

                        if ((validVriable == true) && ( guiObjectExists == true))
                        {
                           
                            GUIObjectType objtype = GUIObjectsInUse[guiObjectName].GUIObjectType;
                            switch (objtype)
                            {
                                case GUIObjectType.Textfield:
                                    foreach (GUI_textfield txtfield in consoleDirector.GUITextFieldsToAdd)
                                    {
                                        if (txtfield.GUIObjName == guiObjectName)
                                        {
                                            foreach (LocalVariable var in local_variables)
                                            {
                                                if (var.Name == variableName)
                                                {
                                                    if (var.Type == VariableType.String)
                                                    {
                                                        var.Value = txtfield.textfieldtext;
                                                        break;
                                                    } else
                                                    {
                                                        Console.WriteLine($"{var.Name} is not a string.");
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case GUIObjectType.Button:
                                    foreach (GUI_Button buttonobj in consoleDirector.GUIButtonsToAdd)
                                    {
                                        if (buttonobj.GUIObjName == guiObjectName)
                                        {
                                            foreach (LocalVariable var in local_variables)
                                            {
                                                if (var.Name == variableName)
                                                {
                                                    if (var.Type == VariableType.String)
                                                    {
                                                        var.Value = buttonobj.newButton.Text.ToString();
                                                        break;
                                                    } else
                                                    {
                                                        Console.WriteLine($"{var.Name} is not a string.");
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case GUIObjectType.Label:
                                    foreach (GUI_Label lbltxt in consoleDirector.GUILabelsToAdd)
                                    {
                                        if (lbltxt.GUIObjName == guiObjectName)
                                        {
                                            foreach (LocalVariable var in local_variables)
                                            {
                                                if (var.Name == variableName)
                                                {
                                                    if (var.Type == VariableType.String)
                                                    {
                                                        var.Value = lbltxt.newlabel.Text.ToString();
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"{var.Name} is not a string.");
                                                        break;
                                                    }
                                                }
                                            }
                                            
                                        }
                                    }
                                    break;
                                case GUIObjectType.Checkbox:
                                    foreach (GUI_Checkbox chkbox in consoleDirector.GUICheckboxToAdd)
                                    {
                                        if (chkbox.GUIObjName == guiObjectName)
                                        {
                                            foreach (LocalVariable var in local_variables)
                                            {
                                                if (var.Name == variableName)
                                                {
                                                    if (var.Type == VariableType.String)
                                                    {
                                                        var.Value = chkbox.newCheckbox.Text.ToString();
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"{var.Name} is not a string.");
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        } else if ((validVriable == true) && (guiObjectExists == false)){
                            Console.WriteLine($"Could not locate GUI object {guiObjectName}.");
                        } else if ((validVriable == false) && (guiObjectExists == true)) {
                            Console.WriteLine($"Could not locate variable {variableName}.");
                        } else
                        {
                            Console.WriteLine($"Invalid GUI object: {guiObjectName} Invalid variable: {variableName}");
                        }
                    } else
                    {
                        Console.WriteLine("Invalid format to gettext.");
                    }
                }
                if (split_input[0].Equals("gui_item_settext", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string guiObjectName = split_input[1];
                        string textToSetTo = "";
                        StringBuilder newstring = new StringBuilder();
                        if (split_input.Length > 3)
                        {
                            foreach(string s  in split_input.Skip(2))
                            {
                                newstring.Append(s);
                            }
                            textToSetTo = SetVariableValue(newstring.ToString());
                        } else if (split_input.Length == 3) { 
                            textToSetTo = SetVariableValue(split_input[2]);
                        }
                        bool guiObjectExists = (GUIObjectsInUse.ContainsKey(guiObjectName));
                        if (guiObjectExists == true)
                        {
                            GUIObjectType objtype = GUIObjectsInUse[guiObjectName].GUIObjectType;
                            switch (objtype)
                            { 
                                case GUIObjectType.Textfield:
                                    foreach (GUI_textfield txtfield in consoleDirector.GUITextFieldsToAdd)
                                    {
                                        if (txtfield.GUIObjName == guiObjectName)
                                        {
                                            txtfield.SetText(textToSetTo);
                                        }
                                    }
                                    break;
                                case GUIObjectType.Button:
                                    foreach (GUI_Button buttonobj in consoleDirector.GUIButtonsToAdd)
                                    {
                                        if (buttonobj.GUIObjName == guiObjectName)
                                        {
                                            buttonobj.SetText(textToSetTo);
                                        }
                                    }
                                    break;
                                case GUIObjectType.Label:
                                    foreach (GUI_Label labelobj in consoleDirector.GUILabelsToAdd)
                                    {
                                        if (labelobj.GUIObjName == guiObjectName)
                                        {
                                            labelobj.SetText(textToSetTo);
                                        }
                                    }
                                    break;
                                case GUIObjectType.Checkbox:
                                    foreach (GUI_Checkbox chkbox in consoleDirector.GUICheckboxToAdd)
                                    {
                                        if (chkbox.GUIObjName == guiObjectName)
                                        {
                                            chkbox.SetText(textToSetTo);
                                        }
                                    }
                                    break;
                                default:
                                    Console.WriteLine($"Cannot set text to {guiObjectName}");
                                    break;
                            }
                        }
                        else if (guiObjectExists == false)
                        {
                            Console.WriteLine($"Could not locate GUI object {guiObjectName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to settext.");
                    }
                }
                if (split_input[0].Equals("msgbox", StringComparison.OrdinalIgnoreCase))
                {
                    if (GUIModeEnabled == true)
                    {
              
                            bool extracting = false;
                            bool extractingTitle = false;
                            bool hasText = false;
                            bool hasTitle = false;
                            int[] hasButtons = { 0, 1, 2 }; // 0 is no, 1 is button "OK", 2 is button "YES,NO" which returns a value
                            int selectedButton = 0;

                            string expected_variable = "";
                            LocalVariable bool_forYesNo = null;

                            string text = "";
                            string title = "";
                            foreach (string s in split_input)
                            {
                                if (extracting == true)
                                {
                                    string q = SetVariableValue(s);
                                    foreach (char c in q)
                                    {
                                        if (c != '|')
                                        {
                                            if (extractingTitle == true)
                                            {
                                                title += c;
                                            } else if (extractingTitle == false)
                                            {
                                                text += c;
                                            }
                                        }
                                        else
                                        {
                                            extracting = false;
                                            extractingTitle = false;
                                        }
                                    }
                                    if (extracting == true)
                                    {
                                        if (extractingTitle == true)
                                        {
                                            title += " ";
                                        }
                                        else if (extractingTitle == false)
                                        {
                                            text += " ";
                                        }
                                    }
                                }
                                else
                                {
                                    if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 5);
                                        extracting = true;
                                        extractingTitle = false;
                                        text = _placeholder + " ";
                                        hasText = true;
                                    }
                                    if (s.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 6);
                                        extractingTitle = true;
                                        extracting = true;
                                        title = _placeholder + " ";
                                        hasTitle = true;
                                    }
                                    if (s.StartsWith("Buttons:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 8);
                                        string a = ConvertNumericalVariable(_placeholder);
                                        if (a.StartsWith("YESNO,", StringComparison.OrdinalIgnoreCase))
                                        {
                                            selectedButton = 2;
                                            expected_variable = s.Remove(0, 6).TrimEnd();
                                            bool validValue = LocalVariableExists(expected_variable);
                                            if (validValue == true)
                                            {
                                                LocalVariable var_totakevalue = local_variables.Find(locvar => locvar.Name == expected_variable);
                                                if (var_totakevalue != null)
                                                {
                                                    if (var_totakevalue.Type != VariableType.Boolean)
                                                    {
                                                        Console.WriteLine($"{expected_variable} variable not a bool.");
                                                        break;
                                                    } else
                                                    {
                                                        bool_forYesNo = var_totakevalue;
                                                    }
                                                }
                                            } else
                                            {
                                                Console.WriteLine($"{expected_variable} variable not found.");
                                                break;
                                            }
                                        }
                                        if (a.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                                        {
                                            selectedButton = 1;
                                        }
                                    }
                                }
                            }

                            if (hasText == true)
                            {
                                if (hasTitle == true)
                                {
                                    if (selectedButton != 0)
                                    {
                                        switch (selectedButton)
                                        {
                                            case 1:
                                            Terminal.Gui.Application.MainLoop.Invoke(() =>
                                            {
                                                consoleDirector.ok_msgbox(title, text);
                                            });
                                                break;
                                            case 2:
                                            int result = -1;
                                                Terminal.Gui.Application.MainLoop.Invoke(() =>
                                                {
                                                    result = consoleDirector.yesno_msgbox(title, text);
                                                });
                                                if (result == 0)
                                                {
                                                    bool_forYesNo.Value = "True";
                                                }
                                                else if (result == 1)
                                                {
                                                    bool_forYesNo.Value = "False";
                                                }
                                                break;
                                        }
                                        
                                    } else
                                    {
                                        Console.WriteLine("Message box requires buttons to be defined: OK, YESNO");
                                    }
                                } else
                                {
                                    Console.WriteLine("Message box requires title");
                                }
                            } else
                            {
                                Console.WriteLine("Message box requires text.");
                            }

                    } else
                    {
                            Console.WriteLine("GUI mode must be on.");
                    }
                    
                    
                }
                if (split_input[0].Equals("savedialog", StringComparison.OrdinalIgnoreCase))
                {
                    Terminal.Gui.Application.MainLoop.Invoke(() =>
                    {
                        string newtitle = consoleDirector.showsaveDialog();
                        Console.Title = newtitle;
                    });
                }

                /// <summary>
                /// List items can hold multiple variable items. 
                /// 
                /// SYNTAX EXAMPLES:
                /// new_list variabletype listname             <- creates new list
                /// list_add listname variablename [...]      <- can add more than 1 variable if separated by a space
                /// list_remove listname variablename        <- removes specified variablename from list
                /// list_setall listname value              <- every member of list receives new value
                /// </summary>
                if (split_input[0].Equals("new_list", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        string arrayType = split_input[1].ToLower();
                        ArrayType new_arrayType = new ArrayType();
                        bool valid_arrayType = true;
                        // Ensure a valid input was provided
                        switch (arrayType)
                        {
                            case "string":
                                new_arrayType = ArrayType.String;
                                break;
                            case "int":
                                new_arrayType = ArrayType.Int;
                                break;
                            case "integer":
                                new_arrayType = ArrayType.Int;
                                break;
                            case "float":
                                new_arrayType = ArrayType.Float;
                                break;
                            case "bool":
                                new_arrayType = ArrayType.Boolean;
                                break;
                            case "boolean":
                                new_arrayType = ArrayType.Boolean;
                                break;
                            default:
                                valid_arrayType = false;
                                break;
                        }
                        if (valid_arrayType == true)
                        {
                            string listName = split_input[2];
                            bool properName = ContainsOnlyLettersAndNumbers(listName);
                            bool alreadyExists = NameInUse(listName);
                            if ((properName == true) && (alreadyExists == false))
                            {
                                LocalList newArray = new LocalList();
                                newArray.Name = listName;
                                newArray.arrayType = new_arrayType;
                                local_arrays.Add(newArray);
                                namesInUse.Add(listName, objectClass.List);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Improper type: {arrayType}.");
                        }

                    }
                    else
                    {
                        Console.WriteLine("Invalid format to create new list.");
                    }
                }
                if (split_input[0].Equals("list_add", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string listName = split_input[1];
                        string varName = split_input[2];
                        bool varExists = LocalVariableExists(varName.TrimEnd());
                        bool arrayExists = false;
                        if (split_input.Length > 3)
                        {
                            bool foundList = false;
                            string badVariable = "";
                            foreach (LocalList list in local_arrays)
                            {
                                if (list.Name == listName)
                                {
                                    foundList = true;
                                    foreach (string str in split_input.Skip(2))
                                    {
                                        bool validVar = LocalVariableExists(str);
                                        string currentVar = str;
                                        if (validVar)
                                        {
                                            foreach (LocalVariable locvar in local_variables)
                                            {
                                                if (locvar.Name == currentVar)
                                                {
                                                    list.itemAdd(locvar);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            badVariable += str + " "; // add every non-added value to list
                                        }
                                    }
                                    if (badVariable != "")
                                    {
                                        Console.WriteLine($"Item(s) not found: {badVariable}");
                                    }
                                    break;
                                }
                            }
                            if (foundList == false) { Console.WriteLine($"Could not locate list {listName}."); }
                        }
                        else if (split_input.Length == 3)
                        {
                            if (varExists == true)
                            {
                                foreach (LocalList array in local_arrays)
                                {
                                    if (array.Name == listName)
                                    {
                                        foreach (LocalVariable localVar in local_variables)
                                        {
                                            if (localVar.Name == varName)
                                            {
                                                array.itemAdd(localVar);
                                                break;
                                            }
                                        }
                                        arrayExists = true;
                                    }
                                }
                                if (arrayExists == false)
                                {
                                    Console.WriteLine($"Could not locate list {listName}.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Could not locate variable {varName}.");
                            }
                        }
                        else { Console.WriteLine("Invald format to add items to list."); }
                    }
                }
                if (split_input[0].Equals("list_remove", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string listName = split_input[1];
                        string varName = split_input[2];
                        bool varExists = LocalVariableExists(varName);
                        bool arrayExists = false;
                        if (varExists == true)
                        {
                            foreach (LocalList array in local_arrays)
                            {
                                if (array.Name == listName)
                                {
                                    array.itemRemove(varName);
                                    break;
                                }
                            }
                            if (arrayExists == false)
                            {
                                Console.WriteLine($"Could not locate list {listName}.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate variable {varName}.");
                        }
                    }
                }
                if (split_input[0].Equals("list_setall", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 2)
                    {
                        bool arrayExist = false;
                        string arrayName = split_input[1];

                        foreach (LocalList localLists in local_arrays)
                        {
                            if (localLists.Name == arrayName)
                            {
                                switch (localLists.arrayType)
                                {
                                    // The array type will determine how we handle the input
                                    case ArrayType.Float:
                                        if (split_input.Length > 3)
                                        {
                                            Console.WriteLine("Invalid format to pass float.");
                                        }
                                        else
                                        {
                                            string a_ = SetVariableValue(split_input[2]);
                                            string b_ = ConvertNumericalVariable(a_);
                                            bool isfloat = float.TryParse(b_, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                                            if (isfloat == true)
                                            {
                                                localLists.SetAllWithValue(b_);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Output is not a valid float: {b_}");
                                            }
                                        }
                                        break;
                                    case ArrayType.Int:
                                        if (split_input.Length > 3)
                                        {
                                            Console.WriteLine("Invalid format to pass integer.");
                                        }
                                        else
                                        {
                                            string a_ = SetVariableValue(split_input[2]);
                                            string b_ = ConvertNumericalVariable(a_);
                                            bool isInt = IsNumeric(b_);
                                            if (isInt == true)
                                            {
                                                localLists.SetAllWithValue(b_);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Output is not a valid integer: {b_}");
                                            }
                                        }
                                        break;
                                    case ArrayType.String:
                                        // We'll recompile string and pass it
                                        string newValue = "";
                                        if (split_input.Length > 3)
                                        {
                                            foreach (string s in split_input.Skip(2))
                                            {
                                                newValue += s;
                                            }
                                        }
                                        string aa = SetVariableValue(newValue);
                                        string bb = ConvertNumericalVariable(aa);
                                        localLists.SetAllWithValue(bb.Trim());
                                        break;
                                    case ArrayType.Boolean:
                                        if (split_input.Length > 3)
                                        {
                                            Console.WriteLine("Invalid format to pass boolean.");
                                        }
                                        else
                                        {
                                            bool isBool = false;
                                            string[] acceptableValue = { "true", "false", "1", "0" };
                                            string operation = "";
                                            foreach (string s in acceptableValue)
                                            {
                                                if (split_input[2].Equals(s, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if ((s == "true") || (s == "1")) { operation = "True"; }
                                                    if ((s == "false") || s == "0") { operation = "False"; }
                                                    isBool = true;
                                                    break;
                                                }
                                            }
                                            if (isBool == true)
                                            {
                                                localLists.SetAllWithValue(operation);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Output is not a valid boolean: {split_input[2]}");
                                            }
                                        }
                                        break;
                                }
                                arrayExist = true;
                                break;
                            }
                        }
                        if (arrayExist == false)
                        {
                            Console.WriteLine($"Could not locate list {arrayName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to set all in list.");
                    }
                }
                if (split_input[0].Equals("list_printall", StringComparison.OrdinalIgnoreCase))
                {
                    string listName = split_input[1];
                    if (split_input.Length == 2)
                    {
                        bool listExists = false;
                        foreach (LocalList list_ in local_arrays)
                        {
                            if (list_.Name == listName)
                            {
                                listExists = true;
                                list_.PrintAll();
                                break;
                            }
                        }
                        if (listExists == false)
                        {
                            Console.WriteLine($"Could not locate list {listName}");
                        }
                    } else { Console.WriteLine("Invalid format for list_printall"); 
                    }
                }
                
                /// <summary>
                /// The filesystem interface allows the user to read, write, move, copy, and edit attributes of
                /// files and directories.
                ///
                /// SYNTAX EXAMPLES:
                /// filesystem_write path contents[...]                 <- writes contents to path, will overwrite original contents
                /// filesystem_append path contents[...]               <- appens contents to path, will not overwrit
                /// filesystem_readall path variable                  <- sets variable value to contents of file
                /// filesystem_readtolist path list                  <- assigns each line of file to a string variable to list [list must be either A) an empty string list, or B) not exist at all]
                /// filesystem_delete path                          <- deletes file at path
                /// filesystem_copy currentpath targetpath         <- copys file in currentpath to targetpath
                /// filesystem_move currentpath targetpath        <- moves file from currentpath to targetpath
                /// filesystem_sethidden path                                  <- sets file at path to hidden
                /// filesystem_setvisible path                                <- sets file at path to not hidden
                /// filesystem_mkdir path                                                   <- creates directory at path
                /// filesystem_rmdir path                                                  <- removes directory at path
                /// filesystem_copydir currentpath targetpath                             <- copy directory in current path to targetpath
                /// filesystem_movedir current path targetpath                           <- move directory from current path to targetpath
                /// <summary>
                if (split_input[0].Equals("filesystem_write", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string path_ = SetVariableValue(split_input[1]);
                        string content_ = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                        filesystem.WriteOverFile(path_, content_);
                    } else
                    {
                        Console.WriteLine("Invalid format for filesystem_write");
                    }
                }
                if (split_input[0].Equals("filesystem_append", StringComparison.OrdinalIgnoreCase))
                {
                    ///<summary>
                    /// This script does not work and is giving me a really weird fucking problem
                    /// [txtfield] is not properly converting to its value and is outputting nothing into the file
                    /// 
                    /// bool isreadonly = false
                    /// string btntext = Save
                    /// string txtfield = nol
                    /// int varzero = 0
                    /// new_task btnclick background 0
                    /// task_add btnclick gui_item_gettext maintext txtfield
                    /// task_add btnclick environment set title[txtfield]
                    /// task_add btnclick pause 1000
                    /// task_add btnclick filesystem_append C:\Users\chris\OneDrive\Desktop\demonstration.txt[txtfield]
                    /// new_gui_item textfield maintext 5 5[isreadonly]
                    /// new_gui_item button mainButton btnclick 10 15 5 3
                    /// gui_item_sety mainButton percent 90
                    /// gui_item_settext mainButton[btntext]
                    /// gui_item_setwidth maintext percent 100
                    /// gui_item_setheight maintext percent 50
                    /// gui_item_setx maintext number 0
                    /// gui_mode on
                    /// 
                    /// 
                    /// </summary>
                    if (split_input.Length >= 3)
                    {
                        string path_ = SetVariableValue(split_input[1]);
                        string content_ = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                        filesystem.AppendToFile(path_, content_);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_append");
                    }
                }
                if (split_input[0].Equals("filesystem_readall", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        bool varexists = LocalVariableExists(split_input[2]);
                        if (varexists == true)
                        {
                            try
                            {
                                string a_ = filesystem.ReadEntireFile(split_input[1]);
                                if (a_ == null)
                                {
                                    foreach (LocalVariable var in local_variables)
                                    {
                                        if (var.Name == split_input[2])
                                        {
                                            if (var.Type == VariableType.String)
                                            {
                                                var.Value = a_;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{split_input[2]} not a valid string variable.");
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"Error occurred when reading from file {split_input[1]}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{split_input[2]} not a valid string variable.");
                        }
                    } else
                    {
                        Console.WriteLine("Invalid format for filesystem_readall");
                    }
                }
                if (split_input[0].Equals("filesystem_readtolist", StringComparison.OrdinalIgnoreCase))

                {
                    if (split_input.Length == 3)
                    {
                        string path_ = SetVariableValue(split_input[1]);
                        string locallist_ = SetVariableValue(split_input[2]);
                        bool listExists = false;
                        bool listEmpty = false;
                        bool listIsString = false;
                        // Make sure if there is a list with this name, it is empty
                        foreach(LocalList list_ in local_arrays)
                        {
                            if (list_.Name == locallist_)
                            {
                                listExists = true;
                                if (list_.numberOfElements == 0)
                                {
                                    listEmpty = true; // It exists but it is empty (usable)
                                    if (list_.arrayType == ArrayType.String)
                                    {
                                        listIsString = true;
                                    }
                                }
                                break;
                            }
                        }
                        
                        if (listExists == false)
                        {
                            LocalList locallist = new LocalList();
                            bool issuccess = true;
                            try
                            {
                                locallist = filesystem.ReadFileToList(path_, locallist_);
                            } catch
                            {
                                issuccess = false;
                            }
                            if (issuccess == true)
                            {
                                local_arrays.Add(locallist);
                            }
                        } else if ((listExists == true) && (listEmpty == true))
                        {
                            if (listIsString == true)
                            {
                                LocalList locallist = new LocalList();
                                bool issuccess = true;
                                try
                                {
                                    locallist = filesystem.ReadFileToList(path_, locallist_);
                                }
                                catch
                                {
                                    issuccess = false;
                                }
                                if (issuccess == true)
                                {
                                    foreach(LocalList lists_ in local_arrays)
                                    {
                                        if (lists_.Name == locallist_)
                                        {

                                            foreach(LocalVariable var in locallist.items)
                                            {
                                                lists_.items.Add(var);
                                                lists_.numberOfElements++;
                                            }
                                            break;
                                        }
                                    }
                                }


                            } else
                            {
                                Console.WriteLine($"If providing name of list that already exists, it must be an empty string list.");
                            }
                        } else
                        {
                            Console.WriteLine($"If providing name of list that already exists, it must be an empty string list.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_append");
                    }
                }
                if (split_input[0].Equals("filesystem_delete", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        filesystem.DeleteFile(split_input[1]);
                    } else
                    {
                        Console.WriteLine("Invalid format for filesystem_delete");
                    }
                }
                if (split_input[0].Equals("filesystem_copy", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        filesystem.CopyFileToLocation(split_input[1], split_input[2]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_copy");
                    }
                }
                if (split_input[0].Equals("filesystem_move", StringComparison.OrdinalIgnoreCase))
                {
                        if (split_input.Length == 3)
                        {
                            filesystem.MoveFileToLocation(split_input[1], split_input[2]);
                        }
                        else
                        {
                            Console.WriteLine("Invalid format for filesystem_move");
                        }
                }
                if (split_input[0].Equals("filesystem_sethidden", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        filesystem.SetFileToHidden(split_input[1]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_sethidden");
                    }
                }
                if (split_input[0].Equals("filesystem_setvisible", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        filesystem.SetHiddenFileToVisible(split_input[1]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_setvisible");
                    }
                }
                if (split_input[0].Equals("filesystem_mkdir", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        filesystem.CreateDirectory(split_input[1]);
                    } else
                    {
                        Console.WriteLine("Invalid format for filesystem_mkdir");
                    }
                }
                if (split_input[0].Equals("filesystem_rmdir", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        filesystem.RemoveDirectory(split_input[1]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_rmdir");
                    }
                }
                if (split_input[0].Equals("filesystem_copydir", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        filesystem.CopyDirectoryToLocation(split_input[1], split_input[2]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_copydir");
                    }
                }
                if (split_input[0].Equals("filesystem_movedir", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        filesystem.MoveDirectoryToLocation(split_input[1], split_input[2]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid format for filesystem_movedir");
                    }
                }

                /// Tasks are a list of commands than can be executed as a background task (on a separate thread) or in-line with the main code.
                /// Tasks will run once in chronological order (unless a loop in the task keeps it alive)
                /// 
                /// SYNTAX EXAMPLES:
                /// new_task taskname 'inline'/'background' integer       <- creates new task list, sets to inline/background, *integer is optional parameter to define the task's local script delay
                /// task_add taskname command(s) [...]                   <- appends new line of commands to task list
                /// task_remove taskname index                          <- removes task line at specified index
                /// task_insert taskname index command(s)[...]         <- interts new line of commands into index
                /// task_printall                                     <- prints list of all task items
                /// task_setdelay name int:miliseconds               <- sets the local script delay of task
                /// task_execute taskname                           <- executes specified task
                /// </summary>
                if (split_input[0].Equals("new_task", StringComparison.OrdinalIgnoreCase))
                {
                    string taskName_;
                    TaskType taskType_;
                    int scriptDelay_;
                    bool error_raised = false;
                    if (split_input.Length == 4)
                    {
                        if (namesInUse.ContainsKey(split_input[1]))
                        {
                            error_raised = true;
                            Console.WriteLine($"{split_input[1]} name in use.");
                        }
                        else
                        {
                            bool nameCheck = ContainsOnlyLettersAndNumbers(split_input[1]);
                            if (nameCheck == true)
                            {
                                // We have a valid name, now we proceed
                                taskName_ = split_input[1];

                                string a_ = split_input[2].Trim().ToLower(); // should be either 'inline' or 'background'
                                if ((a_ == "inline") || (a_ == "background"))
                                {
                                    if (a_ == "inline")
                                    {
                                        taskType_ = TaskType.InlineTask;
                                        bool validInteger = IsNumeric(split_input[3].Trim());
                                        if (validInteger == true)
                                        {
                                            scriptDelay_ = Int32.Parse(split_input[3]);
                                            TaskList newTask = new TaskList(taskName_, taskType_, scriptDelay_);
                                            tasklists_inuse.Add(newTask);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{split_input[3]} is not a valid integer for task's local script delay.");
                                        }
                                    }
                                    else if (a_ == "background")
                                    {
                                        taskType_ = TaskType.BackgroundTask;
                                        bool validInteger = IsNumeric(split_input[3].Trim());
                                        if (validInteger == true)
                                        {
                                            scriptDelay_ = Int32.Parse(split_input[3]);
                                            TaskList newTask = new TaskList(taskName_, taskType_, scriptDelay_);
                                            tasklists_inuse.Add(newTask);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{split_input[3]} is not a valid integer for task's local script delay.");
                                        }
                                    }

                                }
                                else
                                {
                                    error_raised = true;
                                    Console.WriteLine("Task type must be 'inline' or 'background'.");
                                }
                            }
                            else
                            {
                                error_raised = true;
                                Console.WriteLine("Task names may only contain letters and numbers.");
                            }
                        }
                    }
                    else if (split_input.Length == 3) // We are taking an optional final parameter (integer) for task's script delay
                    {
                        if (namesInUse.ContainsKey(split_input[1]))
                        {
                            error_raised = true;
                            Console.WriteLine($"{split_input[1]} name in use.");
                        }
                        else
                        {
                            bool nameCheck = ContainsOnlyLettersAndNumbers(split_input[1]);
                            if (nameCheck == true)
                            {
                                // We have a valid name, now we proceed
                                taskName_ = split_input[1];
                                string a_ = split_input[2].Trim().ToLower(); // should be either 'inline' or 'background'
                                if ((a_ == "inline") || (a_ == "background"))
                                {
                                    if (a_ == "inline")
                                    {
                                        taskType_ = TaskType.InlineTask;
                                        TaskList newTask = new TaskList(taskName_, taskType_);
                                        tasklists_inuse.Add(newTask);
                                    }
                                    else if (a_ == "background")
                                    {
                                        taskType_ = TaskType.BackgroundTask;
                                        TaskList newTask = new TaskList(taskName_, taskType_);
                                        tasklists_inuse.Add(newTask);
                                    }
                                }
                                else
                                {
                                    error_raised = true;
                                    Console.WriteLine("Task type must be 'inline' or 'background'.");
                                }
                            }
                            else
                            {
                                error_raised = true;
                                Console.WriteLine("Task names may only contain letters and numbers.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to create new task.");
                    }
                }
                if (split_input[0].Equals("task_add", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length >= 3)
                    {
                        string taskname = split_input[1];
                        bool foundtasklist = false;
                        string command = "";
                        if (split_input.Length > 3)
                        {
                            foreach (string s in split_input.Skip(2))
                            {
                                command += s + " ";
                            }
                        }
                        else if (split_input.Length == 3)
                        {
                            command = split_input[2];
                        }
                        foreach (TaskList ts in tasklists_inuse)
                        {
                            if (ts.taskName == taskname)
                            {
                                ts.AppendCommand(command);
                                foundtasklist = true;
                                break;
                            }
                        }
                        if (foundtasklist == false)
                        {
                            Console.WriteLine($"Could not locate task list {taskname}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to add task to task list.");
                    }
                }
                if (split_input[0].Equals("task_remove", StringComparison.OrdinalIgnoreCase))
                {

                }
                if (split_input[0].Equals("task_insert", StringComparison.OrdinalIgnoreCase))
                {
                    string taskname = split_input[1];
                    bool foundtasklist = false;
                    string command = "";
                    if (split_input.Length > 3)
                    {
                        foreach (string s in split_input.Skip(2))
                        {
                            command += s + " ";
                        }
                        command.Trim();
                    }
                    else if (split_input.Length == 3)
                    {
                        command = split_input[2];
                    }
                    bool validInteger = IsNumeric(split_input[2]); // must be valid number
                    if (validInteger == true)
                    {

                        foreach (TaskList ts in tasklists_inuse)
                        {
                            if (ts.taskName == taskname)
                            {
                                ts.AppendCommand(command);
                                foundtasklist = true;
                                break;
                            }
                        }
                        if (foundtasklist == false)
                        {
                            Console.WriteLine($"Could not locate task list {taskname}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{split_input[2]} is not a valid integer.");
                    }
                }
                if (split_input[0].Equals("task_clearall", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string taskname = split_input[1];
                        bool foundtasklist = false;
                        foreach (TaskList ts in tasklists_inuse)
                        {
                            if (ts.taskName == taskname)
                            {
                                ts.Clear();
                                foundtasklist = true;
                                break;
                            }
                        }
                        if (foundtasklist == false)
                        {
                            Console.WriteLine($"Could not locate task list {taskname}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to clear task list.");
                    }
                }
                if (split_input[0].Equals("task_printall", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string taskname = split_input[1];
                        bool foundtasklist = false;
                        foreach (TaskList ts in tasklists_inuse)
                        {
                            if (ts.taskName == taskname)
                            {
                                ts.PrintContents();
                                foundtasklist = true;
                                break;
                            }
                        }
                        if (foundtasklist == false)
                        {
                            Console.WriteLine($"Could not locate task list {taskname}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to print task list.");
                    }
                }
                if (split_input[0].Equals("task_setdelay", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 3)
                    {
                        string taskname = split_input[1];
                        bool foundtasklist = false;
                        bool validInteger = IsNumeric(split_input[2]);
                        if (validInteger == true)
                        {
                            int a_ = Int32.Parse(split_input[2]);
                            foreach (TaskList ts in tasklists_inuse)
                            {
                                if (ts.taskName == taskname)
                                {
                                    ts.scriptDelay = a_;
                                    foundtasklist = true;
                                    break;
                                }
                            }
                            if (foundtasklist == false)
                            {
                                Console.WriteLine($"Could not locate task list {taskname}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{split_input[2]} is not a valid integer for task's local script delay.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to set task list delay.");
                    }
                }
                if (split_input[0].Equals("task_execute", StringComparison.OrdinalIgnoreCase))
                {
                    if (split_input.Length == 2)
                    {
                        string taskname = split_input[1];
                        bool foundtasklist = false;
                        List<string> commandsToPass = new List<string>();
                        int scriptDelay_ = 0;
                        TaskType taskType_ = TaskType.InlineTask; // We just assume inline unless otherwise specified
                        foreach (TaskList ts in tasklists_inuse)
                        {
                            if (ts.taskName == taskname)
                            {
                                commandsToPass = ts.taskList;
                                scriptDelay_ = ts.scriptDelay;
                                taskType_ = ts.taskType;
                                foundtasklist = true;
                                break;
                            }
                        }
                        if (foundtasklist == false)
                        {
                            Console.WriteLine($"Could not locate task list {taskname}");
                        }
                        else
                        {
                            // We proceed to execute the task
                            executeTask(commandsToPass, taskType_, scriptDelay_);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format to execute task list.");
                    }
                }

                //if (split_input[0].Equals("compile", StringComparison.OrdinalIgnoreCase)) { compiler.Compile(split_input[1]); }

                if (split_input[0].Equals("SETUP"))
                {
                    SetupFiletype setup = new SetupFiletype();
                    bool isadmin = setup.IsAdministrator();
                    if (isadmin == false)
                    {
                        Console.WriteLine("You will need to restart GyroPrompt as an administrator in order to setup .gs script files.");
                    } else
                    {
                        bool runSetup = setup.SystemsCheck();
                        if (runSetup == false)
                        {
                            Console.WriteLine("Could not properly setup .gs script files.");
                        } else
                        {
                            Console.WriteLine("Setup successful!");
                        }
                    }
                }

                if (split_input[0].Equals("BEEP", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Beep(1000, 1000);
                }

            } catch (Exception error){ Console.WriteLine($"Error with input - {error}"); }
        }

        // Executes a script file line-by-line
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

        /// <summary>
        /// taskInfo is a struct used to pass List<string> and integer scriptdelay into new thread as object (if task is background and not in-line)
        /// If TaskType is inline, then we'll just process it in the executeTask method
        /// </summary>
        public struct taskInfo
        {
            public List<string> commands { get; set; }
            public int scriptdelay { get; set; }
        }
        public void executeTask(List<string> commands, TaskType type, int scriptDelay)
        {
            taskInfo taskInfo = new taskInfo();
            taskInfo.commands = commands;
            taskInfo.scriptdelay = scriptDelay;
            string pause_ = ($"pause {taskInfo.scriptdelay.ToString()}");
            if (type == TaskType.InlineTask)
            {
                // Executes the commands inline
                foreach (string command in commands)
                {
                    parse(command.TrimEnd());
                    parse(pause_.TrimEnd()); // Script delay

                }
            } else if (type == TaskType.BackgroundTask)
            {
                // Executes the commands in background
                Thread thread = new Thread(new ParameterizedThreadStart(newthread));
                thread.Start(taskInfo);
            }
        }
        public void newthread(object commandList)
        {
            taskInfo currentTask = (taskInfo)commandList;
            string pause_ = ($"pause {currentTask.scriptdelay.ToString()}");
            foreach(string command in currentTask.commands)
            {
                parse(command.TrimEnd());
                parse(pause_.TrimEnd());
            }
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
                if (environmentalVars.ContainsKey(capturedText))
                {
                    a = a + environmentalVars[capturedText].ToString();
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
                // Then check for date and time
                if (capturedText.StartsWith("Date:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    string processedTimeDate = timedate_handler.returnDateTime(_placeholder);
                    a += processedTimeDate;
                }
                // Then check for list items
                if (capturedText.StartsWith("List:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    if (_placeholder.StartsWith("At:", StringComparison.OrdinalIgnoreCase))
                    {
                        // Referencing an index position within the list
                        string place_ = _placeholder.Remove(0, 3);
                        string[] items_ = place_.Split(',');
                        if (items_.Length == 2)
                        {
                            bool validName = false;
                            bool isNumber = IsNumeric(items_[1].Trim());
                            
                            if (isNumber == true)
                            {
                                string a_ = ConvertNumericalVariable(items_[1]);
                                int indexednumber = Int32.Parse(a_);

                                foreach (LocalList list in local_arrays)
                                {
                                    if (list.Name == items_[0])
                                    {
                                        string b = list.GetValueAtIndex(indexednumber);
                                        a += b;
                                        validName = true;
                                        break;
                                    }
                                }
                                if (validName == false)
                                {
                                    Console.WriteLine($"Could not locate list: {items_[0]}.");
                                }
                            }

                        } else
                        {
                            Console.WriteLine("Expecting list name and index position separated by comma.");
                        }
                        
                    } else if (_placeholder.StartsWith("Item:"))
                    {
                        // Referencing a list item by name
                        string place_ = _placeholder.Remove(0, 5);
                        string[] items_ = place_.Split(",");
                        if ( items_.Length == 2)
                        {
                            bool varExist = LocalVariableExists(items_[1].Trim());
                            if (varExist == true)
                            {
                                bool validList = false;
                                foreach (LocalList list in local_arrays)
                                {
                                    if (list.Name == items_[0])
                                    {
                                        foreach (LocalVariable locvar in list.items)
                                        {
                                            if (locvar.Name == items_[1].Trim())
                                            {
                                                a += locvar.Value;
                                            }
                                        }
                                        validList = true;
                                        break;
                                    }
                                }
                                if (validList == false)
                                {
                                    Console.WriteLine($"Could not locate list: {items_[0]}");
                                }
                            } else { Console.WriteLine($"Could not locate variable {items_[1]}."); }
                        } else
                        {
                            Console.WriteLine("Expecting list name and item name separated by comma.");
                        }
                    } else if (_placeholder.StartsWith("Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        string place_ = _placeholder.Remove(0, 7);
                        bool validlist = false;
                        foreach (LocalList list in local_arrays)
                        {
                            if (list.Name == place_)
                            {
                                a += list.numberOfElements.ToString();
                                validlist = true;
                                break;
                            }
                            if (validlist == false)
                            {
                                Console.WriteLine($"Could not locate list: {place_}");
                            }
                        }
                    } else
                    {
                        Console.WriteLine("Must point to list position with Item or At, or number of items with Len.");
                    }
                }
                // Then check for filesystem conditions
                if (capturedText.StartsWith("Filesystem:", StringComparison.OrdinalIgnoreCase))
                {
                    string placeholder_ = capturedText.Remove(0, 11);
                    if (placeholder_.StartsWith("FileExists:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 11);
                        bool exists = filesystem.DoesFileExist(_placeholder);
                        if (exists == true)
                        {
                            a += "True";
                        } else if (exists == false)
                        {
                            a += "False";
                        }
                    }
                    if (placeholder_.StartsWith("DirExists:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 10);
                        bool exists = filesystem.DoesDirectoryExist(_placeholder);
                        if (exists == true)
                        {
                            a += "True";
                        }
                        else if (exists == false)
                        {
                            a += "False";
                        }
                    }
                }
                // Then check for references to GUI items
                if (capturedText.StartsWith("GUIItem:", StringComparison.OrdinalIgnoreCase))
                {
                    // text, x, y, width, height
                    string placeholder_ = capturedText.Remove(0, 8);
                    bool validProperty = false;
                    int returnProperty = 0;
                    if (placeholder_.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 5).TrimEnd();
                        validProperty = true;
                        returnProperty = 0;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("X:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 2).TrimEnd();
                        validProperty = true;
                        returnProperty = 1;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("Y:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 2).TrimEnd();
                        validProperty = true;
                        returnProperty = 2;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("Height:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 7).TrimEnd();
                        validProperty = true;
                        returnProperty = 3;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("Width:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 6).TrimEnd();
                        validProperty = true;
                        returnProperty = 4;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("IsChecked:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 10).TrimEnd();
                        validProperty = true;
                        returnProperty = 5;
                        gotoGUIObject(_placeholder, returnProperty);
                    }

                    void gotoGUIObject(string expectedGUIObjName, int expectedReturnProperty)
                    {
                        if (GUIObjectsInUse.ContainsKey(expectedGUIObjName))
                        {
                            GUIObjectType guiObjType = GUIObjectsInUse[expectedGUIObjName].GUIObjectType;
                            switch(guiObjType)
                            {
                                case GUIObjectType.Button:
                                    GUI_Button buttonitem_ = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (buttonitem_ != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                a += buttonitem_.newButton.Text.ToString();
                                                break;
                                            case 1:
                                                string x_ = buttonitem_.newButton.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                a += filteredString;
                                                break;
                                            case 2:
                                                string y_ = buttonitem_.newButton.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                a += filteredString1;
                                                break;
                                            case 3:
                                                string hei_ = buttonitem_.newButton.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                a += filteredString2;
                                                break;
                                            case 4:
                                                string wid_ = buttonitem_.newButton.Width.ToString();
                                                string filteredString3 = new string(wid_.Where(char.IsDigit).ToArray());
                                                a += filteredString3;
                                                break;
                                            case 5:
                                                Console.WriteLine("IsChecked only applies to toggle items: checkbox and radiobutton");
                                                break;
                                        }
                                    }
                                    break;
                                case GUIObjectType.Textfield:
                                    GUI_textfield textitem_ = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (textitem_ != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                a += textitem_.textView.Text.ToString();
                                                break;
                                            case 1:
                                                string x_ = textitem_.textView.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                a += filteredString;
                                                break;
                                            case 2:
                                                string y_ = textitem_.textView.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                a += filteredString1;
                                                break;
                                            case 3:
                                                string hei_ = textitem_.textView.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                a += filteredString2;
                                                break;
                                            case 4:
                                                string wid = textitem_.textView.Width.ToString();
                                                string filteredString3 = new string(wid.Where(char.IsDigit).ToArray());
                                                a += filteredString3;
                                                break;
                                            case 5:
                                                Console.WriteLine("IsChecked only applies to toggle items: checkbox and radiobutton");
                                                break;
                                        }
                                    }
                                    break;
                                case GUIObjectType.Label:
                                    GUI_Label labelitem_ = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (labelitem_ != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                a += labelitem_.newlabel.Text.ToString();
                                                break;
                                            case 1:
                                                string x_ = labelitem_.newlabel.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                a += filteredString;
                                                break;
                                            case 2:
                                                string y_ = labelitem_.newlabel.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                a += filteredString1;
                                                break;
                                            case 3:
                                                string hei_ = labelitem_.newlabel.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                a += filteredString2;
                                                break;
                                            case 4:
                                                string wid_ = labelitem_.newlabel.Width.ToString();
                                                string filteredString3 = new string(wid_.Where(char.IsDigit).ToArray());
                                                a += filteredString3;
                                                break;
                                            case 5:
                                                Console.WriteLine("IsChecked only applies to toggle items: checkbox and radiobutton");
                                                break;
                                        }
                                    }
                                    break;
                                case GUIObjectType.Checkbox:
                                    // To be populated
                                    break;
                                case GUIObjectType.Radiobutton:
                                    // To be populated
                                    break;
                            }

                        } else
                        {
                            Console.WriteLine($"Could not locate GUI object {expectedGUIObjName}.");
                        }
                    }
                    if (validProperty == false)
                    {
                        Console.WriteLine("Invalid property type. Expected Text, X, Y, Height, or Width");
                    }
                }
                // Finally, check for newline
                if (capturedText.Equals("nl", StringComparison.OrdinalIgnoreCase)) { a = a + "\n"; }
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
                if (environmentalVars.ContainsKey(capturedText))
                {
                    Console.Write(environmentalVars[capturedText].ToString());
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
                // Then check for date and time
                if (capturedText.StartsWith("Date:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    string processedTimeDate = timedate_handler.returnDateTime(_placeholder);
                    Console.Write(processedTimeDate);
                }
                // Then check for a list reference
                if (capturedText.StartsWith("List:"))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    if (_placeholder.StartsWith("At:"))
                    {
                        // Referencing an index position within the list
                        string place_ = _placeholder.Remove(0, 3);
                        string converints = ConvertNumericalVariable(place_);
                        string[] items_ = converints.Split(',');
                        if (items_.Length == 2)
                        {
                            bool validName = false;
                            bool isNumber = IsNumeric(items_[1].TrimEnd());
                            string a_ = ConvertNumericalVariable(items_[1].TrimEnd());
                            if (isNumber == true)
                            {

                                int indexednumber = Int32.Parse(a_);

                                foreach (LocalList list in local_arrays)
                                {
                                    if (list.Name == items_[0])
                                    {
                                        string b = list.GetValueAtIndex(indexednumber);
                                        Console.Write(b);
                                        validName = true;
                                        break;
                                    }
                                }
                                if (validName == false)
                                {
                                    Console.WriteLine($"Could not locate list: {items_[0]}.");
                                }
                            }

                        }
                        else
                        {
                            Console.WriteLine("Expecting list name and index position separated by comma.");
                        }

                    }
                    else if (_placeholder.StartsWith("Item:"))
                    {
                        // Referencing a list item by name
                        string place_ = _placeholder.Remove(0, 5);
                        string[] items_ = place_.Split(",");
                        if (items_.Length == 2)
                        {
                            bool varExist = LocalVariableExists(items_[1].Trim());
                            if (varExist == true)
                            {
                                bool validList = false;
                                foreach (LocalList list in local_arrays)
                                {
                                    if (list.Name == items_[0])
                                    {
                                        foreach (LocalVariable locvar in list.items)
                                        {
                                            if (locvar.Name == items_[1].Trim())
                                            {
                                                Console.Write($"{locvar.Value}");
                                            }
                                        }
                                        validList = true;
                                        break;
                                    }
                                }
                                if (validList == false)
                                {
                                    Console.WriteLine($"Could not locate list: {items_[0]}");
                                }
                            }
                            else { Console.WriteLine($"Could not locate variable {items_[1]}."); }
                        }
                        else if (_placeholder.StartsWith("Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            string cplace_ = _placeholder.Remove(0, 7);
                            bool validlist = false;
                            foreach (LocalList list in local_arrays)
                            {
                                if (list.Name == cplace_)
                                {
                                    Console.Write(list.numberOfElements.ToString());
                                    validlist = true;
                                    break;
                                }
                                if (validlist == false)
                                {
                                    Console.WriteLine($"Could not locate list: {cplace_}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Expecting list name and item name separated by comma.");
                        }
                    }
                    else if (_placeholder.StartsWith("Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        string place_ = _placeholder.Remove(0, 7);
                        bool validlist = false;
                        foreach (LocalList list in local_arrays)
                        {
                            if (list.Name == place_)
                            {
                                Console.Write(list.numberOfElements.ToString());
                                validlist = true;
                                break;
                            }
                            if (validlist == false)
                            {
                                Console.WriteLine($"Could not locate list: {place_}");
                            }
                        }
                    }
                }
                // Then check for filesystem
                if (capturedText.StartsWith("Filesystem:"))
                {
                    string placeholder_ = capturedText.Remove(0, 11);
                    if (placeholder_.StartsWith("FileExists:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 11);
                        bool exists = filesystem.DoesFileExist(_placeholder);
                        if (exists == true)
                        {
                            Console.Write("True");
                        }
                        else if (exists == false)
                        {
                            Console.Write("False");
                        }
                    }
                    if (placeholder_.StartsWith("DirExists:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 10);
                        bool exists = filesystem.DoesDirectoryExist(_placeholder);
                        if (exists == true)
                        {
                            Console.Write("True");
                        }
                        else if (exists == false)
                        {
                            Console.Write("False");
                        }
                    }
                }
                // Check for GUI object property
                // Then check for references to GUI items
                if (capturedText.StartsWith("GUIItem:", StringComparison.OrdinalIgnoreCase))
                {
                    // text, x, y, width, height
                    string placeholder_ = capturedText.Remove(0, 8).TrimEnd();
                    bool validProperty = false;
                    int returnProperty = 0;
                    if (placeholder_.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 5).TrimEnd();
                        validProperty = true;
                        returnProperty = 0;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("X:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 2).TrimEnd();
                        validProperty = true;
                        returnProperty = 1;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("Y:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 2).TrimEnd();
                        validProperty = true;
                        returnProperty = 2;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("Height:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 7).TrimEnd();
                        validProperty = true;
                        returnProperty = 3;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("Width:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 6).TrimEnd();
                        validProperty = true;
                        returnProperty = 4;
                        gotoGUIObject(_placeholder, returnProperty);
                    }
                    if (placeholder_.StartsWith("IsChecked:", StringComparison.OrdinalIgnoreCase))
                    {
                        string _placeholder = placeholder_.Remove(0, 10).TrimEnd();
                        validProperty = true;
                        returnProperty = 5;
                        gotoGUIObject(_placeholder, returnProperty);
                    }

                    void gotoGUIObject(string expectedGUIObjName, int expectedReturnProperty)
                    {
                        if (GUIObjectsInUse.ContainsKey(expectedGUIObjName))
                        {
                            GUIObjectType guiObjType = GUIObjectsInUse[expectedGUIObjName].GUIObjectType;
                            switch (guiObjType)
                            {
                                case GUIObjectType.Button:
                                    GUI_Button buttonitem_ = consoleDirector.GUIButtonsToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (buttonitem_ != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                Console.Write(buttonitem_.newButton.Text.ToString());
                                                break;
                                            case 1:
                                                string x_ = buttonitem_.newButton.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString);
                                                break;
                                            case 2:
                                                string y_ = buttonitem_.newButton.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString1);
                                                break;
                                            case 3:
                                                string hei_ = buttonitem_.newButton.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString2);
                                                break;
                                            case 4:
                                                string wid_ = buttonitem_.newButton.Width.ToString();
                                                string filteredString3 = new string(wid_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString3);
                                                break;
                                            case 5:
                                                Console.WriteLine("IsChecked only applies to toggle items: checkbox and radiobutton");
                                                break;
                                        }
                                    }
                                    break;
                                case GUIObjectType.Textfield:
                                    GUI_textfield textitem_ = consoleDirector.GUITextFieldsToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (textitem_ != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                Console.Write(textitem_.textView.Text.ToString());
                                                break;
                                            case 1:
                                                string x_ = textitem_.textView.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString);
                                                break;
                                            case 2:
                                                string y_ = textitem_.textView.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString1);
                                                break;
                                            case 3:
                                                string hei_ = textitem_.textView.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString2);
                                                break;
                                            case 4:
                                                string wid = textitem_.textView.Width.ToString();
                                                string filteredString3 = new string(wid.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString3);
                                                break;
                                            case 5:
                                                Console.WriteLine("IsChecked only applies to toggle items: checkbox and radiobutton");
                                                break;
                                        }
                                    }
                                    break;
                                case GUIObjectType.Label:
                                    GUI_Label labelitem_ = consoleDirector.GUILabelsToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (labelitem_ != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                Console.Write(labelitem_.newlabel.Text.ToString());
                                                break;
                                            case 1:
                                                string x_ = labelitem_.newlabel.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString);
                                                break;
                                            case 2:
                                                string y_ = labelitem_.newlabel.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString1);
                                                break;
                                            case 3:
                                                string hei_ = labelitem_.newlabel.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString2);
                                                break;
                                            case 4:
                                                string wid_ = labelitem_.newlabel.Width.ToString();
                                                string filteredString3 = new string(wid_.Where(char.IsDigit).ToArray());
                                                Console.Write(filteredString3);
                                                break;
                                            case 5:
                                                Console.WriteLine("IsChecked only applies to toggle items: checkbox and radiobutton");
                                                break;
                                        }
                                    }
                                    break;
                                case GUIObjectType.Checkbox:
                                    // To be populated
                                    break;
                                case GUIObjectType.Radiobutton:
                                    // To be populated
                                    break;
                            }

                        }
                        else
                        {
                            Console.WriteLine($"Could not locate GUI object {expectedGUIObjName}.");
                        }
                    }
                    if (validProperty == false)
                    {
                        Console.WriteLine("Invalid property type. Expected Text, X, Y, Height, or Width");
                    }
                }
                // Check for newline
                if (capturedText.Equals("nl", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(); }
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
                if (capturedText.StartsWith("Backcolor:", StringComparison.OrdinalIgnoreCase))
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
        // Check if name is in use anywhere
        public bool NameInUse(string name)
        {
            if (namesInUse.ContainsKey(name))
            {
                return true;
            }
            else { return false; }
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
            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}