
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
        public RandomizeInt randomizer = new RandomizeInt();
        public ConditionChecker condition_checker = new ConditionChecker();
        
        public IDictionary<string, bool> namesInUse = new Dictionary<string, bool>();
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
        public string Title = Console.Title;
        public string Title_
        {
            get { return Title; }
            set { Title = value; Console.Title = value; }
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

            foreach (string envvar_name in environmentalVars.Keys)
            {
                namesInUse.Add(envvar_name, true); // All encompassing name reserve system
            }
            condition_checker.LoadOperations();
        }

        public void parse(string input)
        {
            try
            {
                bool valid_command = false;

                string[] split_input = input.Split(' ');

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
                        if (split_input[3] == "false" || split_input[3] == "0") { proper_value = true; }
                        if (split_input[3] == "true" || split_input[3] == "1") { proper_value = true; }


                        if (proper_value == true)
                        {
                            bool name_check = NameInUse(split_input[1]);
                            if (name_check == true) { Console.WriteLine($"{split_input[1]} name in use."); no_issues = false; }
                            if (no_issues == true)
                            {
                                // Syntax checks out, we proceed to declare the variable
                                BooleanVariable new_bool = new BooleanVariable();
                                new_bool.Name = split_input[1];
                                if (split_input[3] == "false" || split_input[3] == "0") { new_bool.bool_val = false; }
                                if (split_input[3] == "true" || split_input[3] == "1") { new_bool.bool_val = true; }
                                new_bool.Type = VariableType.Boolean;
                                local_variables.Add(new_bool);
                                namesInUse.Add(split_input[1], true);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Incorrect formatting to declare bool. Bool cannot take value: {split_input[3]}");
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

                        bool proper_value = IsNumeric(split_input[3]);
                        if (proper_value == true)
                        {
                            bool name_check = NameInUse(split_input[1]);
                            if (name_check == true) { Console.WriteLine($"{split_input[1]} name in use."); no_issues = false; }
                            if (no_issues == true)
                            {
                                // Syntax checks out, we proceed to declare the variable
                                IntegerVariable new_int = new IntegerVariable();
                                new_int.Name = split_input[1];
                                new_int.int_value = Int32.Parse(split_input[3]);
                                new_int.Type = VariableType.Int;
                                local_variables.Add(new_int);
                                namesInUse.Add(split_input[1], true);
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
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{split_input[1]} name in use.");
                            no_issues = false;
                        }
                    }
                }
                ///<summary>
                /// List items can hold multiple variable items. 
                /// 
                /// SYNTAX EXAMPLES:
                /// new_list (variabletype) (listname)             <- creates new list
                /// list_add (listname) (variablename [...])      <- can add more than 1 variable if separated by a space
                /// list_remove (listname) (variablename)        <- removes specified variablename from list
                /// list_setall (listname) (value)              <- every member of list receives new value
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
                                namesInUse.Add(listName, true);
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
                    if (split_input.Length > 3)
                    {
                        string listName = split_input[1];
                        string varName = split_input[2];
                        bool varExists = LocalVariableExists(varName);
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
                ///<summary>
                /// Tasks are a list of commands than can be executed as a background task (on a separate thread) or in-line with the main code.
                /// Tasks will run once in chronological order (unless a loop in the task keeps it alive)
                /// 
                /// SYNTAX EXAMPLES:
                /// new_task (taskname) ('inline'/'background') (*integer)  <- creates new task list, sets to inline/background, *integer is optional parameter to define the task's local script delay
                /// task_add (taskname) (command(s) [...])                 <- appends new line of commands to task list
                /// task_remove (taskname) (index)                        <- removes task line at specified index
                /// task_insert (taskname) (index) (command(s) [...])    <- interts new line of commands into index
                /// task_printall                                       <- prints list of all task items
                /// task_setdelay (name) (int:miliseconds)             <- sets the local script delay of task
                /// task_execute (taskname)                           <- executes specified task
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
                            command.Trim();
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
                                                        var.Value = "True";
                                                        break;
                                                    }
                                                    else if ((s == "0") || (s == "false"))
                                                    {
                                                        var.Value = "False";
                                                        break;
                                                    }
                                                }
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
                                        Console.Title = (split_input[3]);
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
                                                parse(command);
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
                                                parse(command);
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
                                                string[] commands_to_execute = proceeding_commands.Split('|');
                                                try
                                                {
                                                    foreach (string command in commands_to_execute)
                                                    {
                                                        command.Trim();
                                                        try
                                                        {
                                                            parse(command);
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
                                                                parse(command);
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


                //DO NOT INCLUDE, UNDER CONSTRUCTION: if (split_input[0].Equals("compile", StringComparison.OrdinalIgnoreCase)) { compiler.Compile(); }
                //DEBUGGING: if (split_input[0].Equals("NAMES"))
                //{
               //     foreach (string key in namesInUse.Keys)
              //      {
                //        Console.WriteLine(key);
               //     }
                //}
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
                    parse(command);
                    parse(pause_); // Script delay

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
                parse(command);
                parse(pause_);
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
                // Then check for list items
                if (capturedText.StartsWith("List:"))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    if (_placeholder.StartsWith("At:"))
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

                                int indexednumber = Int32.Parse(items_[1]);

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
                    } else
                    {
                        Console.WriteLine("Must point to list position with Item or At.");
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
                // Then check for a list reference
                if (capturedText.StartsWith("List:"))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    if (_placeholder.StartsWith("At:"))
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

                                int indexednumber = Int32.Parse(items_[1]);

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
                        else
                        {
                            Console.WriteLine("Expecting list name and item name separated by comma.");
                        }
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