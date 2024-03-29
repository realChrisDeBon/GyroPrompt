﻿#define is_PARSER
#define HASNETCODE
#define HASGUICODE
#define HASJSON
#define HASHASH
#define HASIFILESYSTEM
#define HASTASK
#define HASFUNCTION
#define HASLIST
#define HASARRAY

#define valueModifier_calculate

global using GlobalSuppressions;
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.ComponentModel;


using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using GyroPrompt.Basic_Functions;
#if HASHASH || HASJSON
using GyroPrompt.Basic_Functions.Object_Modifiers;
#endif
#if HASTASK || HASLIST
using GyroPrompt.Basic_Objects.Collections;
#endif
#if HASARRAY
using GyroPrompt.Basic_Objects.Collections.Arrays;
#endif

using GyroPrompt.Basic_Objects.Component;
using GyroPrompt.Basic_Objects.Variables;

#if HASNETCODE
using GyroPrompt.Network_Objects;
using GyroPrompt.Network_Objects.TCPSocket;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
#endif

#if is_PARSER
using GyroPrompt.Setup;
#endif

#if HASGUICODE
using GyroPrompt.Basic_Objects.GUIComponents;
using Terminal.Gui;
using Color = Terminal.Gui.Color;
#endif

public enum objectClass
{
    [Description("variable")]
    Variable,
    [Description("environmental variable")]
    EnvironmentalVariable,
    [Description("array")]
    VarArray,
    [Description("list")]
    List,
    [Description("task list")]
    TaskList,
    [Description("TCP object")]
    TCPNetObj,
    [Description("data packet")]
    DataPacket,
    [Description("GUI object")]
    GUIObj,
    [Description("function")]
    Function
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
#if HASLIST
        public List<LocalList> local_lists = new List<LocalList>();
#endif
#if HASARRAY
        public ArrayList local_arrays = new ArrayList();
#endif
#if HASTASK
        public List<TaskList> tasklists_inuse = new List<TaskList>();
#endif
#if HASFUNCTION
        public Dictionary<string, string[]> local_function = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
#endif

#if HASNETCODE
        // Mostly network related lists and objects
        public ArrayList activeTCPObjects = new ArrayList();
        public List<ClientSide> activeClients = new List<ClientSide>();
        public List<ServerSide> activeServers = new List<ServerSide>();
        public List<dataPacket> datapacketStack = new List<dataPacket>();
        public object dpStackLock = new object();

        public object eventMessageLock = new object();
        public const string eventmsg_str = "event_message";
        public const string datapacketid_str = "datapacket_ID";
        public const string datapacketsndr_str = "datapacket_sender";
        public const string datapacketval_str = "datapacket_value";
        public string eventMessage = "";
        public string eventMessage_
        {
            get { return eventMessage; }
            set {
                eventMessage = value;
                LocalVariable em = local_variables.Find(e => e.Name == eventmsg_str);
                em.Value = value;
            }
        }
        public void addPacketToStack(dataPacket dpToStack)
        {
            datapacketStack.Add(dpToStack);
            parseDP(dpToStack);
        }
        public void parseDP(dataPacket dpIn)
        {
            LocalVariable tempID_ = local_variables.Find(y => y.Name == datapacketid_str);
            tempID_.Value = dpIn.ID;
            LocalVariable tempSender_ = local_variables.Find(z => z.Name == datapacketsndr_str);
            tempSender_.Value = dpIn.senderAddress;
            LocalList temp = new LocalList();
            LocalVariable temp_ = local_variables.Find(x => x.Name == datapacketval_str);
            temp_.Value = dpIn.objData;

            if (temp_ == null)
            {
                return;
            }
            switch (dpIn.objType)
            {
                case NetObjType.ObjString:
                    temp_.Type = VariableType.String;
                    break;
                case NetObjType.ObjInt:
                    temp_.Type = VariableType.Int;
                    break;
                case NetObjType.ObjFloat:
                    temp_.Type = VariableType.Float;
                    break;
                case NetObjType.ObjBool:
                    temp_.Type = VariableType.Boolean;
                    break;
                case NetObjType.ObjList:
                    // we'll fill this in later
                    break;
                case NetObjType.ByteArray:
                    // um, yeah, later
                    break;
            }
            datapacketStack.Remove(dpIn);
        }
        // -------------------------------------------
#endif
        public SysErrorParser errorHandler = new SysErrorParser();
        public Calculate calculate = new Calculate();
        public TimeDateHandler timedate_handler = new TimeDateHandler();
        public RandomizeInt randomizer = new RandomizeInt();
        public ConditionChecker condition_checker = new ConditionChecker();
#if HASIFILESYSTEM
        public FilesystemInterface filesystem = new FilesystemInterface();
#endif
#if HASHASH
        public DataHasher datahasher = new DataHasher();
#endif
#if HASJSON
        public DataSerializer dataserializer = new DataSerializer();
#endif

        public Dictionary<string, objectClass> namesInUse = new Dictionary<string, objectClass>();
        public bool running_script = false; // Used for determining if a script is being ran
        public int current_line = 0; // Used for reading scripts
        public int max_lines = 0;

        /// <summary>
        /// These variables and methods are used for handling the GUI components. If the user enables the GUI layer, due to the nature of how the Terminal.GUI NuGet
        /// package works, it will operate in an instance of a new Window and will appear on top of the regular CLI interface (until the instance of the GUI Window is
        /// terminated). In order to manage the text output of the console, we have a top level bool GUIModeEnabled. If enabled, we direct the console output to a string
        /// variable which will (Eventually) become accessible to the user via an environmental variable. This will allow the console output to be accessed and ported
        /// to GUI components (text fields, labels, etc). When the GUIModeEnabled is set to false, the console output reverts to its original state and output directly to
        /// the console like normally (Eventually).
        /// </summary>
#if HASGUICODE
        public bool GUIModeEnabled = false;
        public string ConsoleOutCatcher = "";
        public ConsoleOutputDirector consoleDirector = new ConsoleOutputDirector();
        public Dictionary<string, GUI_BaseItem> GUIObjectsInUse = new Dictionary<string, GUI_BaseItem>(StringComparer.OrdinalIgnoreCase);
#endif
        /// <summary>
        /// Below are environmental variables. These are meant for the users to be able to interact with the console settings and modify the environment.
        /// The ConsoleInfo struct/method and keyConsoleKey Dictionary enable easier manipulation of console colors and to save current settings to be recalled.
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
        public Dictionary<string, ConsoleColor> keyConsoleColor = new Dictionary<string, ConsoleColor>(StringComparer.OrdinalIgnoreCase);
#if HASGUICODE
        public Dictionary<string, Terminal.Gui.Color> terminalColor = new Dictionary<string, Terminal.Gui.Color>(StringComparer.OrdinalIgnoreCase);
#endif
        public void setConsoleStatus(ConsoleInfo _consoleinfo)
        {
            Console.ForegroundColor = _consoleinfo.status_forecolor;
            Console.BackgroundColor = _consoleinfo.status_backcolor;
        }
        Dictionary<string, object> environmentalVars = new Dictionary<string, object>();

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
        public bool PromptOn = true;
        public bool PromptOn_
        {
            get { return PromptOn; }
            set { PromptOn = value; }
        }
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

        public Dictionary<string, bool> booldict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "True", true},
            { "False", false},
            { "1", true},
            { "0", false},
        };

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
            environmental_variables.Add(PromptOn_);

            environmentalVars.Add("CursorX", CursorX_);
            environmentalVars.Add("CursorY", CursorY_);
            environmentalVars.Add("WindowWidth", WindowWidth_);
            environmentalVars.Add("WindowHeight", WindowHeight_);
            environmentalVars.Add("Forecolor", foreColor_);
            environmentalVars.Add("Backcolor", backColor_);
            environmentalVars.Add("ScriptDelay", ScriptDelay_);
            environmentalVars.Add("Title", Title_);
            environmentalVars.Add("PromptOn", PromptOn_);

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

#if HASGUICODE
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
#endif

#if HASNETCODE
            StringVariable datapacketvalue = new()
            {
                Name = datapacketval_str,
                Type = VariableType.String,
                Value = "null"
            };
            StringVariable datapacketsender = new()
            {
                Name = datapacketsndr_str,
                Type = VariableType.String,
                Value = "null"
            };
            StringVariable datapacketID = new()
            {
                Name = datapacketid_str,
                Type = VariableType.String,
                Value = "null"
            };
            StringVariable eventmessage = new()
            {
                Name = eventmsg_str,
                Type = VariableType.String,
                Value = eventMessage_
            };
            local_variables.Add(datapacketvalue);
            local_variables.Add(datapacketsender);
            local_variables.Add(datapacketID);
            local_variables.Add(eventmessage);
            namesInUse.Add(datapacketval_str, objectClass.Variable);
            namesInUse.Add(datapacketsndr_str, objectClass.Variable);
            namesInUse.Add(datapacketid_str, objectClass.Variable);
            namesInUse.Add(eventmsg_str, objectClass.Variable);
#endif
            foreach (string envvar_name in environmentalVars.Keys)
            {
                namesInUse.Add(envvar_name, objectClass.EnvironmentalVariable); // All encompassing name reserve system
            }
            condition_checker.LoadOperations(); // Load enum types for operators
            errorHandler.topLevelParser = this; // Hand error handler a reference to instance of this parser
#if HASGUICODE
            errorHandler.GUIConsole = consoleDirector; // Hand error handler a reference to instance of console director
#endif
#if HASIFILESYSTEM
            filesystem.topparse = this;
            filesystem.LoadComDict();
#endif
        }

#if is_PARSER
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
                    if (PromptOn == true) { Console.Write("GyroPrompt > "); }
                    string command = Console.ReadLine();
                    parse(command);
                }
            }
        }
#endif
        public void parse(string input)
        {
            try
            {
                bool valid_command = false;
                bool entry_made = false;
                string[] split_input = input.Split(' ');

                // Detect comment declaration
                if ((split_input[0].Equals("#", StringComparison.OrdinalIgnoreCase)) || (split_input[0].StartsWith("#", StringComparison.OrdinalIgnoreCase)))
                {
                    // Hashtags are treated like comments
                    valid_command = true;
                    entry_made = true;
                }
                // Detect a print statement
                if (split_input[0].Equals("print", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    // create a new string combining all elements in split array, except for the first, then pass it to print command
                    string input_to_print = "";
                    int len = split_input.Length;
                    int pos = 1;
                    foreach (string s in split_input.Skip(1))
                    {
                        input_to_print = input_to_print + s;
                        pos++;
                        if (pos != len)
                        {
                            input_to_print += " ";
                        }
                    }
                    print(input_to_print);
                    valid_command = true;
                }
                if (split_input[0].Equals("println", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    // create a new string combining all elements in split array, except for the first, then pass it to print command
                    string input_to_print = "";
                    int len = split_input.Length;
                    int pos = 1;
                    foreach (string s in split_input.Skip(1))
                    {
                        input_to_print = input_to_print + s;
                        pos++;
                        if (pos != len)
                        {
                            input_to_print += " ";
                        }
                    }
                    print(input_to_print);
                    Console.WriteLine(); // end with new line
                    valid_command = true;
                }
                if (split_input[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    Console.Clear();
                    valid_command = true;
                }
                // Detect a new variable declaration
                if (split_input[0].Equals("bool", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "bool newname = True/False";
                    bool no_issues = true;
                    if (split_input.Length != 4)
                    {
                        errorHandler.ThrowError(1100, "bool variable", null, null, null, expectedFormat);
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
                                errorHandler.ThrowError(1100, "bool variable", null, null, null, expectedFormat);
                                no_issues = false;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1600, split_input[1]);
                            no_issues = false;
                        }
                        bool proper_value = false;
                        split_input[3].Split();
                        string aa = SetVariableValue(split_input[3]);
                        aa.ToLower();
                        if (aa.Equals("False", StringComparison.OrdinalIgnoreCase) || aa == "0") { proper_value = true; }
                        if (aa.Equals("True", StringComparison.OrdinalIgnoreCase) || aa == "1") { proper_value = true; }


                        if (proper_value == true)
                        {
                            bool name_check = NameInUse(split_input[1]);
                            if (name_check == true) {
                                no_issues = false;
                                errorHandler.ThrowError(1300, null, null, split_input[1], null, expectedFormat);
                            }
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
                                valid_command = true;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1400, "bool variable", null, aa, "True/False", expectedFormat);
                        }
                    }
                }
                if (split_input[0].Equals("int", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "int newname = 1";
                    bool no_issues = true;
                    if (split_input.Length != 4)
                    {
                        errorHandler.ThrowError(1100, "int variable", null, null, null, expectedFormat);
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
                                errorHandler.ThrowError(1100, "int variable", null, null, null, expectedFormat);
                                no_issues = false;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1600, split_input[1]);
                            no_issues = false;
                        }
                        string a_ = SetVariableValue(split_input[3]);
                        bool proper_value = IsNumeric(a_);
                        if (proper_value == true)
                        {
                            bool name_check = NameInUse(split_input[1]);
                            if (name_check == true) { errorHandler.ThrowError(1300, null, null, split_input[1], null, expectedFormat); no_issues = false; }
                            if (no_issues == true)
                            {
                                // Syntax checks out, we proceed to declare the variable
                                IntegerVariable new_int = new IntegerVariable();
                                new_int.Name = split_input[1];
                                new_int.int_value = Int32.Parse(a_);
                                new_int.Type = VariableType.Int;
                                local_variables.Add(new_int);
                                namesInUse.Add(split_input[1], objectClass.Variable);
                                valid_command = true;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1400, "int variable", null, split_input[3], "a valid integer", expectedFormat);
                        }
                    }
                }
                if (split_input[0].Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "string newname = A sentence.";
                    bool no_issues = true;
                    if (split_input.Length <= 3)
                    {
                        errorHandler.ThrowError(1100, "string variable", null, null, null, expectedFormat);
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
                                errorHandler.ThrowError(1100, "string variable", null, null, null, expectedFormat);
                                no_issues = false;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1600, split_input[1]);
                            no_issues = false;
                        }

                        bool name_check = NameInUse(split_input[1]);
                        if (name_check == false)
                        {
                            // Recombine string
                            string a = "";
                            int pos = 4;
                            int len = split_input.Length;
                            foreach (string s in split_input.Skip(3))
                            {
                                a += SetVariableValue(s);
                                if (pos != len)
                                {
                                    a += " ";
                                }
                                pos++;
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
                                valid_command = true;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1300, null, null, split_input[1], null, expectedFormat);
                            no_issues = false;
                        }
                    }
                }
                if (split_input[0].Equals("float", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "float newname = 10.0";
                    bool no_issues = true;
                    if (split_input.Length != 4)
                    {
                        errorHandler.ThrowError(1100, "float variable", null, null, null, expectedFormat);
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
                                errorHandler.ThrowError(1100, "float variable", null, null, null, expectedFormat);
                                no_issues = false;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1600, split_input[1]);
                            no_issues = false;
                        }

                        bool name_check = NameInUse(split_input[1]);
                        bool float_check = float.TryParse(split_input[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                        if (!float_check)
                        {
                            errorHandler.ThrowError(1400, "float variable", null, split_input[3], "a valid float", expectedFormat);
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
                                valid_command = true;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1300, null, null, split_input[1], null, expectedFormat);
                            no_issues = false;
                        }
                    }
                }
                // Detect a new variable array declatation
#if HASARRAY

                if (split_input[0].StartsWith("new_array_", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "new_array_type newname value(s)" + Environment.NewLine + "type is replaced with bool, int, float or string and all values are separated with vertical pipe";

                    if (split_input.Length < 3)
                    {
                        errorHandler.ThrowError(1100, "new_array_type", null, null, null, expectedFormat);

                        return;
                    }
                    string expectedVariableType = split_input[0].Remove(0, 10), expectedArrayName = SetVariableValue(split_input[1]), initialValues = "", badValsFounds = "";
                    bool validVarType = false, validName = ContainsOnlyLettersAndNumbers(expectedArrayName), nameAlreadyUsed = NameInUse(expectedArrayName), clearToProceed = true, someBadVals = false;
                    Array_Type arrayType = Array_Type.None;

                    switch (true)
                    {
                        case bool b when expectedVariableType.Equals("bool", StringComparison.OrdinalIgnoreCase):
                            arrayType = Array_Type.Boolean;
                            validVarType = true;
                            break;
                        case bool b when expectedVariableType.Equals("int", StringComparison.OrdinalIgnoreCase):
                            arrayType = Array_Type.Int;
                            validVarType = true;
                            break;
                        case bool b when expectedVariableType.Equals("string", StringComparison.OrdinalIgnoreCase):
                            arrayType = Array_Type.String;
                            validVarType = true;
                            break;
                        case bool b when expectedVariableType.Equals("float", StringComparison.OrdinalIgnoreCase):
                            arrayType = Array_Type.Float;
                            validVarType = true;
                            break;
                        default:
                            // Throw error & return
                            errorHandler.ThrowError(1400, "array type", null, expectedVariableType, "a bool, int, float or string", expectedFormat);
                            return;
                            break;
                    }

                    if (validVarType == false)
                    {
                        // Throw error & return
                        // Redundant but still
                        clearToProceed = false;
                        errorHandler.ThrowError(1400, "array type", null, expectedVariableType, "a bool, int, float or string", expectedFormat);
                        return;
                    }
                    if (validName == false)
                    {
                        // Throw error & return
                        clearToProceed = false;
                        errorHandler.ThrowError(1600, expectedArrayName, null, null, null, expectedFormat);
                        return;
                    }
                    if (nameAlreadyUsed == true)
                    {
                        // Throw error & return
                        clearToProceed = false;
                        errorHandler.ThrowError(1300, null, null, expectedArrayName, null, expectedFormat);
                        return;
                    }
                    if (clearToProceed == true)
                    {
                        int pos = 3;
                        int len = split_input.Length;
                        foreach (string s in split_input.Skip(2))
                        {
                            initialValues += SetVariableValue(s);
                            if (pos != len)
                            {
                                initialValues += " ";
                            }
                            pos++;
                        }

                        string[] initialValuesSplit = initialValues.Split('|');

                        switch (arrayType)
                        {
                            case Array_Type.None:

                                break;
                            case Array_Type.Boolean:
                                List<bool> valsToPassb = new List<bool>();
                                foreach (string q in initialValuesSplit)
                                {
                                    if (booldict.ContainsKey(q))
                                    {
                                        valsToPassb.Add(booldict[q]);
                                    } else
                                    {
                                        badValsFounds += q + " ";
                                        someBadVals = true;
                                    }
                                }

                                bool_array newboolarray = new bool_array(expectedArrayName, valsToPassb);
                                local_arrays.Add(newboolarray);
                                namesInUse.Add(expectedArrayName, objectClass.VarArray);
                                if (someBadVals == true)
                                {
                                    // Throw error
                                    errorHandler.ThrowError(1400, "bool array", null, badValsFounds, "True/False", expectedFormat);
                                }
                                break;
                            case Array_Type.Int:
                                List<int> valsToPassi = new List<int>();
                                foreach (string e in initialValuesSplit)
                                {
                                    if (IsNumeric(e.Trim()) == true)
                                    {
                                        valsToPassi.Add(Int32.Parse(e.Trim()));
                                    }
                                    else
                                    {
                                        badValsFounds += e + " ";
                                        someBadVals = true;
                                    }
                                }

                                int_array newintarray = new int_array(expectedArrayName, valsToPassi);
                                local_arrays.Add(newintarray);
                                namesInUse.Add(expectedArrayName, objectClass.VarArray);
                                if (someBadVals == true)
                                {
                                    // Throw error
                                    errorHandler.ThrowError(1400, "integer array", null, badValsFounds, "a valid integer", expectedFormat);
                                }
                                break;
                            case Array_Type.String:
                                List<string> valsToPasss = new List<string>();
                                foreach (string m in initialValuesSplit)
                                {
                                    valsToPasss.Add(m);
                                }

                                str_array newstrarray = new str_array(expectedArrayName, valsToPasss);
                                local_arrays.Add(newstrarray);
                                namesInUse.Add(expectedArrayName, objectClass.VarArray);
                                if (someBadVals == true)
                                {
                                    // Throw error
                                    errorHandler.ThrowError(1400, "string array", null, badValsFounds, "a valid string", expectedFormat);
                                }
                                break;
                            case Array_Type.Float:
                                List<float> valsToPassf = new List<float>();
                                foreach (string m in initialValuesSplit)
                                {
                                    bool float_check = float.TryParse(m, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                                    if (float_check == true)
                                    {
                                        valsToPassf.Add(float.Parse(m, CultureInfo.InvariantCulture.NumberFormat));
                                    }
                                    else
                                    {
                                        badValsFounds += m + " ";
                                        someBadVals = true;
                                    }
                                }

                                float_array newfloatarray = new float_array(expectedArrayName, valsToPassf);
                                local_arrays.Add(newfloatarray);
                                namesInUse.Add(expectedArrayName, objectClass.VarArray);
                                if (someBadVals == true)
                                {
                                    // Throw error
                                    errorHandler.ThrowError(1400, "float array", null, badValsFounds, "a valid float", expectedFormat);
                                }
                                break;
                        }

                    }
                }
#endif
                // Modify variable values
                if (split_input[0].Equals("set", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "set existingvariable = value";
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
                                    string a__ = $"set {var_name} = ";
                                    string a_c = SetVariableValue(input.Remove(0, a__.Length));

                                    switch (var.Type)
                                    {
                                        case VariableType.String:
                                            var.Value = a_c;
                                            valid_command = true;
                                            break;
                                        case VariableType.Int:
                                            string placeholder = SetVariableValue(a).Trim();
                                            string b = ConvertNumericalVariable(placeholder).Trim();
                                            bool isnumber = IsNumeric(a_c);
                                            if (isnumber == true)
                                            {
                                                var.Value = a_c;
                                                valid_command = true;
                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1400, $"integer", null, b, "integer value", expectedFormat);
                                            }
                                            break;
                                        case VariableType.Float:
                                            string placeholder2 = SetVariableValue(a);
                                            string b_ = ConvertNumericalVariable(placeholder2).Trim();
                                            bool isfloat = float.TryParse(a_c, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                                            if (isfloat == true)
                                            {
                                                var.Value = a_c;
                                                valid_command = true;
                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1400, $"float", null, b_, "float value", expectedFormat);
                                            }
                                            break;
                                        case VariableType.Boolean:
                                            string val = SetVariableValue(split_input[3]);
                                            bool validInput = false;
                                            if (booldict.ContainsKey(a_c))
                                            {
                                                bool temp__ = booldict[val];
                                                validInput = true;
                                                var.Value = temp__.ToString();
                                                valid_command = true;
                                            }

                                            if (validInput == false)
                                            {
                                                errorHandler.ThrowError(1400, $"bool", null, split_input[3], "True/False", expectedFormat);
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    errorHandler.ThrowError(1100, "setting variable value", null, null, null, expectedFormat);
                                }
                            }
                        }
                        if (name_found == false)
                        {
                            errorHandler.ThrowError(1200, null, var_name, null, null, expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "setting variable value", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("toggle", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "toggle boolvariable";
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
                                    valid_command = true;
                                }
                                else if ((local_var.Name == var_name) && (local_var.Type != VariableType.Boolean))
                                {
                                    errorHandler.ThrowError(2100, null, null, local_var.Name, "bool variable", expectedFormat);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, var_name, null, null, expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "toggle", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("int+", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "int+ integername";
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
                                    valid_command = true;
                                }
                                else if ((localVar.Name == var_name) && (localVar.Type != VariableType.Int))
                                {
                                    errorHandler.ThrowError(2100, null, null, localVar.Name, "integer variable", expectedFormat);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, var_name, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "int+", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("int-", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "int- integername";
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
                                    valid_command = true;
                                }
                                else if ((localVar.Name == var_name) && (localVar.Type != VariableType.Int))
                                {
                                    errorHandler.ThrowError(2100, null, null, localVar.Name, "integer variable", expectedFormat);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, var_name, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "int-", null, null, null, expectedFormat);
                    }
                }
                // Detect environmental variable modification
                if (split_input[0].Equals("environment", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "environment set environmentalvariable value";
                    if (split_input.Length > 3)
                    {
                        if (split_input[1].Equals("set", StringComparison.OrdinalIgnoreCase))
                        {
                            if (split_input.Length >= 4)
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
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, _num, "integer value", expectedFormat);
                                        }
                                        break;
                                    case "windowwidth":
                                        string _num1 = SetVariableValue(split_input[3].Trim());
                                        bool _valid1 = IsNumeric(_num1);
                                        if (_valid1 == true)
                                        {
                                            WindowWidth_ = (Int32.Parse(_num1));
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, _num1, "integer value", expectedFormat);
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
                                                valid_command = true;
                                            }
                                            catch (ArgumentOutOfRangeException ex)
                                            {
                                                Console.WriteLine("Cursor out of bounds.");
                                            }
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, _num2, "integer value", expectedFormat);
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
                                                valid_command = true;
                                            }
                                            catch (ArgumentOutOfRangeException ex)
                                            {
                                                Console.WriteLine("Cursor out of bounds.");
                                            }
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, _num3, "integer value", expectedFormat);
                                        }
                                        break;
                                    case "backcolor":
                                        if (keyConsoleColor.ContainsKey(split_input[3]))
                                        {
                                            ConsoleColor color = keyConsoleColor[split_input[3]];
                                            backColor_ = color;
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, split_input[3], "color value", expectedFormat);
                                        }
                                        break;
                                    case "forecolor":
                                        if (keyConsoleColor.ContainsKey(split_input[3]))
                                        {
                                            ConsoleColor color = keyConsoleColor[split_input[3]];
                                            foreColor_ = color;
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, split_input[3], "color value", expectedFormat);
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
                                                valid_command = true;
                                            }
                                            catch (ArgumentOutOfRangeException ex)
                                            {
                                                Console.WriteLine("Error passing value.");
                                            }
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, _num4, "integer value", expectedFormat);
                                        }
                                        break;
                                    case "title":
                                        string newTitle = "";
                                        for (int xx = 3; xx < split_input.Length; xx++)
                                        {
                                            newTitle += SetVariableValue(split_input[xx]);
                                            if (xx != split_input.Length - 1)
                                            {
                                                newTitle += " ";
                                            }
                                        }
                                        Console.Title = (newTitle);
                                        valid_command = true;
                                        break;
                                    case "prompt":
                                        string a__ = SetVariableValue(split_input[3]);
                                        string[] validValues = { "True", "On", "False", "Off" };
                                        bool validinput = false;
                                        int x = 0;
                                        foreach (string ss in validValues)
                                        {
                                            if (a__.Equals(ss, StringComparison.OrdinalIgnoreCase))
                                            {
                                                validinput = true;
                                                if (x <= 1)
                                                {
                                                    PromptOn_ = true;
                                                } else if (x >= 2)
                                                {
                                                    PromptOn_ = false;
                                                }
                                                valid_command = true;
                                            }
                                            x++;
                                        }
                                        if (validinput == false)
                                        {
                                            errorHandler.ThrowError(1400, var_name, null, a__, "True/False", expectedFormat);
                                        }
                                        break;
                                    default:
                                        errorHandler.ThrowError(1200, null, var_name, null, null, expectedFormat);
                                        break;
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1100, "environment set", null, null, null, expectedFormat);
                            }
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "environment", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("pause", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "pause 1000";
                    if (split_input.Length == 2)
                    {
                        string a_ = ConvertNumericalVariable(split_input[1]);
                        bool valid = IsNumeric(a_);
                        int b_ = Int32.Parse(split_input[1]);
                        if (valid)
                        {
                            valid_command = true;
                            Thread.Sleep(b_);
                        }
                        else
                        {
                            errorHandler.ThrowError(1400, "pause", null, a_, "integer value", expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "pause", null, null, null, expectedFormat);
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
                    entry_made = true;
                    string expectedFormat = "if variable = value then command(s) OR if variable = value then command(s) else command(s)" + Environment.NewLine + "If value is reference to variable, variable name should be bracketed [ ]";
                    string first_value = SetVariableValue(split_input[1]).TrimEnd();
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
                                    condition_ += b;
                                    if (!split_input[x + 1].Equals("then", StringComparison.OrdinalIgnoreCase))
                                    {
                                        condition_ += " ";
                                    }
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
                                        errorHandler.ThrowError(1400, $"operator {operator_type}", null, $"{var_val} and/or {conditon_checkvariables}", "numbers only", expectedFormat);
                                    }
                                }
                                if (condition_is_met == true)
                                {
                                    // Each command is seperated by the vertical pipe | allowing for multiple commands to execute
                                    string[] commands_to_execute = proceeding_commands.Split('|');
                                    try
                                    {
                                        valid_command = true;
                                        foreach (string command in commands_to_execute)
                                        {
                                            try
                                            {
                                                parse(command.TrimEnd());
                                            }
                                            catch
                                            {
                                                //Error with specific command

                                            }

                                        }
                                    }
                                    catch
                                    {
                                        // General error

                                    }
                                }
                                else if ((condition_is_met == false) && (else_statement_exists == true))
                                {
                                    // Condition was false, so we execute the 'else' statement
                                    string[] else_commands_to_execute = else_statement_commands.Split('|');
                                    try
                                    {
                                        valid_command = true;
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

                                    }
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(2000, "if", null, null, "then", expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1400, $"if", null, operator_type, "= != > >= < <=", expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1200, null, first_value, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("while", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "while variable = value do command(s)" + Environment.NewLine + "If value is reference to variable, variable name should be bracketed [ ]";
                    string first_value = SetVariableValue(split_input[1]).TrimEnd();
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
                                    condition_ += b;
                                    if (!split_input[x + 1].Equals("do", StringComparison.OrdinalIgnoreCase))
                                    {
                                        condition_ += " ";
                                    }
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
                                                    valid_command = true;
                                                    foreach (string command in commands_to_execute)
                                                    {
                                                        try
                                                        {
                                                            parse(command.TrimEnd());
                                                        }
                                                        catch
                                                        {
                                                            //Error with specific command, exiting 'while' loop

                                                            break;
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    // General error, exiting 'while' loop

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

                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(1400, $"operator {operator_type}", null, $"{var_val} and/or {conditon_checkvariables}", "numbers only", expectedFormat);

                                    }
                                }


                            }
                            else
                            {
                                errorHandler.ThrowError(2000, "while", null, null, "do", expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1400, $"while", null, operator_type, "= != > >= < <=", expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1200, null, first_value, null, null, expectedFormat);
                    }
                }
                ///<summary>
                /// Script specific commands that will only execute if running_script = true
                /// These commands only have an impact on the flow of a script file and not on
                /// code that is executed from the prompt manually.
                /// </summary>
                if (split_input[0].Equals("goto", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "goto 10";
                    if (running_script == true)
                    {
                        if (split_input.Length == 2)
                        {
                            bool valid = IsNumeric(split_input[1]);
                            if (valid == true)
                            {
                                int a = Int32.Parse(split_input[1]);
                                if (a < max_lines)
                                {
                                    current_line = a;
                                    valid_command = true;
                                } else
                                {
                                    errorHandler.ThrowError(1200, null, $"line {a}", null, null, expectedFormat);
                                }

                            } else
                            {
                                errorHandler.ThrowError(1400, $"goto", null, split_input[1], "integer value", expectedFormat);
                            }
                        }
                        errorHandler.ThrowError(1100, "goto", null, null, null, expectedFormat);
                    }
                    else
                    {
                        errorHandler.ThrowError(2200, null, null, null, null, expectedFormat);
                    }
                }
                // Grab user input
                if (split_input[0].Equals("readline", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "readline strvariable prompt\nFinal parameter prompt is optional.";
                    if (split_input.Length >= 2)
                    {
                        string var_ = SetVariableValue(split_input[1]);
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
                                        valid_command = true;
                                        Console.Write(prompt_);
                                        string a_ = Console.ReadLine();
                                        var.Value = a_;

                                    } else
                                    {
                                        errorHandler.ThrowError(2100, null, null, var_, "string variable", expectedFormat);
                                        break;
                                    }

                                }
                            }
                        } else
                        {
                            errorHandler.ThrowError(1200, null, var_, null, null, expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "readline", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("readkey", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "readkey strvariable prompt" + Environment.NewLine + "Final parameter prompt is optional.";
                    if (split_input.Length >= 2)
                    {
                        string var_ = SetVariableValue(split_input[1]);
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
                                        if (split_input.Length > 2) { Console.Write(prompt_); }
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
                                        Console.WriteLine();
                                        valid_command = true;
                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(2100, null, null, var_, "string variable", expectedFormat);
                                        break;
                                    }

                                }
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, var_, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "readkey", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("readint", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "readint intvariable prompt|errormessage" + Environment.NewLine + "Final parameter prompt and errormessage should be separated by vertical pipe | and are optional.";
                    if (split_input.Length >= 2)
                    {
                        string var_ = SetVariableValue(split_input[1]);

                        string[] prompt_ = new string[2];
                        bool delimiterDetecter = false;

                        string placeholder = string.Join(" ", split_input.Skip(2));
                        foreach (char c in placeholder)
                        {
                            if (c != '|')
                            {
                                if (delimiterDetecter == false)
                                {
                                    prompt_[0] += c;
                                } else if (delimiterDetecter == true)
                                {
                                    prompt_[1] += c;
                                }
                            } else if (c == '|')
                            {
                                delimiterDetecter = true;
                            }
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
                                        valid_command = true;
                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(2100, null, null, var_, "integer variable", expectedFormat);
                                        break;
                                    }

                                }
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, var_, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "readint", null, null, null, expectedFormat);
                    }
                }
                // String manipulation
                if (split_input[0].Equals("string_replace", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "string_replace strvariable targetvalue|newvalue";
                    if (split_input.Length > 2)
                    {
                        string expectedVariableName = split_input[1];
                        string valuesPresplit = "";
                        bool variableExists = false;
                        bool variableIsString = false;
                        bool verticalPipeExists = false;

                        LocalVariable soughtString = local_variables.Find(q => q.Name == expectedVariableName);
                        if (soughtString != null)
                        {
                            variableExists = true;
                            if (soughtString.Type == VariableType.String)
                            {
                                variableIsString = true;

                                // Recombine all strings
                                int pos = 3;
                                int len = split_input.Length;
                                foreach (string s in split_input.Skip(2))
                                {
                                    valuesPresplit += SetVariableValue(s);
                                    if (pos != len)
                                    {
                                        valuesPresplit += " ";
                                    }
                                    pos++;
                                }

                                string[] valuesSplit = valuesPresplit.Split('|');
                                if (valuesSplit.Length == 1)
                                {
                                    errorHandler.ThrowError(1700, null, "|", null, null, expectedFormat);
                                    return;
                                } else if (valuesSplit.Length > 2)
                                {
                                    errorHandler.ThrowError(1400, "targetvalue|newvalue", null, "more than one vertical pipe", "targetvalue|newvalue", expectedFormat);
                                    return;
                                } else if (valuesSplit.Length == 2)
                                {
                                    // Clear to proceed
                                    string temp_ = soughtString.Value.Replace(valuesSplit[0], valuesSplit[1]);
                                    soughtString.Value = temp_;
                                }



                            } else
                            {
                                errorHandler.ThrowError(2100, null, null, expectedVariableName, "string variable", expectedFormat);
                                return;
                            }
                        } else
                        {
                            errorHandler.ThrowError(1200, null, expectedVariableName, null, null, expectedFormat);
                            return;
                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "string_replace", null, null, null, expectedFormat);
                        return;
                    }

                }
                if (split_input[0].Equals("string_remove", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "string_remove strvariable targetvalue";
                    if (split_input.Length > 2)
                    {
                        string expectedVariableName = split_input[1];
                        string targetValue = "";
                        bool variableExists = false;
                        bool variableIsString = false;
                        bool verticalPipeExists = false;

                        LocalVariable soughtString = local_variables.Find(q => q.Name == expectedVariableName);
                        if (soughtString != null)
                        {
                            variableExists = true;
                            if (soughtString.Type == VariableType.String)
                            {
                                variableIsString = true;

                                // Recombine all strings
                                int pos = 3;
                                int len = split_input.Length;
                                foreach (string s in split_input.Skip(2))
                                {
                                    targetValue += SetVariableValue(s);
                                    if (pos != len)
                                    {
                                        targetValue += " ";
                                    }
                                    pos++;
                                }

                                // Clear to proceed
                                int tvlen = targetValue.Length;
                                string temp_ = soughtString.Value;
                                while (temp_.Contains(targetValue) == true)
                                {
                                    int indxof = temp_.IndexOf(targetValue);
                                    string a = temp_.Remove(indxof, tvlen);
                                    temp_ = a;
                                }
                                soughtString.Value = temp_;

                            }
                            else
                            {
                                errorHandler.ThrowError(2100, null, null, expectedVariableName, "string variable", expectedFormat);
                                return;
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, expectedVariableName, null, null, expectedFormat);
                            return;
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "string_remove", null, null, null, expectedFormat);
                        return;
                    }

                }
                // Create function
#if HASFUNCTION
                if ((split_input[0].Equals("new_function", StringComparison.OrdinalIgnoreCase)) || (split_input[0].Equals("new_function*", StringComparison.OrdinalIgnoreCase)))
                {
                    entry_made = true;
                    string expectedFormat = "new_function newname command(s)";
                    string expectedFunctionName = SetVariableValue(split_input[1]);
                    bool okname = ContainsOnlyLettersAndNumbers(expectedFunctionName);
                    bool nameUsed = NameInUse(expectedFunctionName);
                    bool verticalPipeSplit = true;
                    if (split_input.Length < 3)
                    {
                        errorHandler.ThrowError(1100, "new_function", null, null, null, expectedFormat);
                        return;
                    }
                    if (okname == false)
                    {
                        errorHandler.ThrowError(1600, expectedFunctionName, null, null, null, expectedFormat);
                        return;
                    }
                    if (nameUsed == true)
                    {
                        errorHandler.ThrowError(1300, null, null, expectedFunctionName, null, expectedFormat);
                        return;
                    }
                    if ((okname == true) && (nameUsed == false))
                    {
                        if (split_input[0].Equals("new_function", StringComparison.OrdinalIgnoreCase))
                        {
                            verticalPipeSplit = true;
                        } else if (split_input[0].Equals("new_function*", StringComparison.OrdinalIgnoreCase))
                        {
                            verticalPipeSplit = false;
                        }



                        // Recombine the string
                        string commandListUnsplit = "";
                        int pos = 2;
                        int len = split_input.Length;
                        foreach (string s in split_input.Skip(2))
                        {
                            commandListUnsplit += s;
                            pos++;
                            if (pos != len)
                            {
                                commandListUnsplit += " ";
                            }
                        }
                        // Then split it by vertical pipe or put single line
                        string[] commandListSplit = commandListUnsplit.Split('|');
                        string[] commandsNotSplit = { commandListUnsplit };
                        if (verticalPipeSplit == true)
                        {
                            local_function.Add(expectedFunctionName, commandListSplit);
                        } else if (verticalPipeSplit == false)
                        {
                            local_function.Add(expectedFunctionName, commandsNotSplit);
                        }
                        namesInUse.Add(expectedFunctionName, objectClass.Function);
                        valid_command = true;
                    }
                }
                if (split_input[0].Equals("function", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "function functionname";
                    if (split_input.Length == 2)
                    {
                        string functionToExecute = split_input[1];
                        if (local_function.ContainsKey(functionToExecute))
                        {
                            foreach (string command_ in local_function[functionToExecute])
                            {
                                parse(command_);
                            }
                            valid_command = true;
                        } else
                        {
                            errorHandler.ThrowError(1200, null, functionToExecute, null, null, expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "function", null, null, null, expectedFormat);
                    }
                }
#endif
                // Check for hash or serialize
#if HASHASH
                if (split_input[0].Equals("hash256", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "hash256 strvariable valuetohash";
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
                                        valid_command = true;
                                    } else
                                    {
                                        Console.WriteLine("Variable must be string.");
                                    }
                                }
                            }
                        } else
                        {
                            errorHandler.ThrowError(1200, null, inputVar, null, null, expectedFormat);
                        }

                    } else
                    {
                        errorHandler.ThrowError(1100, "hash256", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("hash512", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "hash512 strvariable valuetohash";
                    if (split_input.Length >= 3)
                    {
                        string inputVar = split_input[1];
                        bool isValidVar = LocalVariableExists(inputVar);
                        if (isValidVar == true)
                        {
                            string prompt = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                            string hashedprompt = datahasher.CalculateHash512(prompt);
                            foreach (LocalVariable variable in local_variables)
                            {
                                if (variable.Name == inputVar)
                                {
                                    if (variable.Type == VariableType.String)
                                    {
                                        variable.Value = hashedprompt;
                                        valid_command = true;
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
                            errorHandler.ThrowError(1200, null, inputVar, null, null, expectedFormat);
                        }

                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "hash512", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("hash384", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "hash384 strvariable valuetohash";
                    if (split_input.Length >= 3)
                    {
                        string inputVar = split_input[1];
                        bool isValidVar = LocalVariableExists(inputVar);
                        if (isValidVar == true)
                        {
                            string prompt = SetVariableValue(string.Join(" ", split_input.Skip(2)));
                            string hashedprompt = datahasher.CalculateHash384(prompt);
                            foreach (LocalVariable variable in local_variables)
                            {
                                if (variable.Name == inputVar)
                                {
                                    if (variable.Type == VariableType.String)
                                    {
                                        variable.Value = hashedprompt;
                                        valid_command = true;
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
                            errorHandler.ThrowError(1200, null, inputVar, null, null, expectedFormat);
                        }

                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "hash384", null, null, null, expectedFormat);
                    }
                }
#endif
#if HASJSON
                if (split_input[0].Equals("json_serialize", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "json_serialize strvariable objecttoserialize";
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
                                                valid_command = true;
                                            }
                                        }
                                        break;
                                    case (objectClass.List):
                                        foreach (LocalList list in local_lists)
                                        {
                                            if (list.Name == objToSerialize)
                                            {
                                                serializedprompt += dataserializer.serializeInput(list);
                                                valid_command = true;
                                            }
                                        }
                                        break;
                                    case (objectClass.DataPacket):
                                        foreach (dataPacket datapckt in datapacketStack)
                                        {
                                            if (datapckt.ID == objToSerialize)
                                            {
                                                serializedprompt += dataserializer.serializeInput(datapckt);
                                                valid_command = true;
                                            }
                                        }
                                        break;
                                    default:
                                        string type_ = GetDescription(namesInUse[objToSerialize]);
                                        errorHandler.ThrowError(2100, null, null, type_, "variable, list, or datapacket", expectedFormat);
                                        break;
                                }

                                foreach (LocalVariable variable in local_variables)
                                {
                                    if (variable.Name == inputVar)
                                    {
                                        if (variable.Type == VariableType.String)
                                        {
                                            variable.Value = serializedprompt;
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, $"json_serialize", null, $"{variable.Name}", "string variable", expectedFormat);
                                        }
                                    }
                                }
                            } else
                            {
                                errorHandler.ThrowError(1200, null, objToSerialize, null, null, expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, inputVar, null, null, expectedFormat);
                        }

                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "json_serialize", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("json_deserialize", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "json_deserialize object strvariable";
                    if (split_input.Length == 3)
                    {
                        string receivingObject = split_input[1];
                        string stringSerialized = split_input[2];
                        bool isValidReceivingObj = NameInUse(receivingObject);
                        bool stringExists = LocalVariableExists(stringSerialized);

                        objectClass expectedClass = default;
                        string newname = "";
                        string newvalue = "";
                        string informationBetweenBrackets = "";
                        int valtype = -1;
                        bool foundListElements = false;
                        Dictionary<string, string> foundElement = new Dictionary<string, string>(); // Name, Value
                        // We will parse the string, assuming it contains serialize object
                        if (stringExists == true)
                        {
                            LocalVariable expectedStringVar = local_variables.Find(o => o.Name == stringSerialized);
                            if (expectedStringVar == null)
                            {
                                // Throw error of wrong object type
                                errorHandler.ThrowError(2100, null, null, stringSerialized, "string variable holding serialized data", expectedFormat);
                                return;
                            } else
                            {
                                if (expectedStringVar.Type != VariableType.String)
                                {
                                    // Throw error of wrong variable type, only a string could hold serialized data
                                    errorHandler.ThrowError(2100, null, null, expectedStringVar.Name, "string variable holding serialized data", expectedFormat);
                                    return;
                                } else
                                {

                                    string notsplitData = expectedStringVar.Value;

                                    // Trim the beginning and ending curly brackets

                                    string gg = notsplitData.Replace("{", "");
                                    string hh = gg.Replace("}", "");
                                    notsplitData = hh.TrimEnd();


                                    // Grab the items within the list if they exist (they'll be within square brackets)
                                    int startIndex = notsplitData.IndexOf('[');
                                    int endIndex = notsplitData.IndexOf(']');
                                    // Extract the information between the brackets
                                    if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                                    {
                                        foundListElements = true;
                                        informationBetweenBrackets = notsplitData.Substring(startIndex + 1, endIndex - startIndex - 1);
                                        // Add each item to the foundElement dictionary
                                        string[] newvars = informationBetweenBrackets.Split(',');
                                        for (int i = 0; i < newvars.Length; i++)
                                        {
                                            if (newvars[i].Contains("\"Name\":"))
                                            {
                                                string name_ = newvars[i].Remove(0, 8);
                                                string nameout_ = name_.Replace("\"", "");
                                                string value_ = newvars[i + 1].Remove(0, 9);
                                                string valueout_ = value_.Replace("\"", "");
                                                if (!foundElement.ContainsKey(nameout_))
                                                {
                                                    foundElement.Add(nameout_, valueout_);
                                                }
                                            }
                                        }
                                    }
                                    if (foundListElements == false)
                                    {
                                        informationBetweenBrackets = "`````"; // temp fix for zero length string exception thrown
                                    }
                                    string beforeSplit = notsplitData.Replace(informationBetweenBrackets, "");
                                    string[] splitData = beforeSplit.Split(',');

                                    if (splitData.Length >= 3)
                                    {
                                        string[] possibleTypes = { "\"Typrintpe\":", "\"arrayType\":" };
                                        int typeLengthToRemove = -1;
                                        bool foundName = false;
                                        int nameIndx = Array.FindIndex(splitData, element => element.Contains("\"Name\":"));
                                        bool foundType = false;
                                        int typeIndx = -1;
                                        bool foundValue = false;
                                        int valueIndx = Array.FindIndex(splitData, element => element.Contains("\"Value\":"));
                                        if (valueIndx >= 0)
                                        {
                                            foundValue = true;
                                            string stepone = splitData[valueIndx].Remove(0, 9);
                                            newvalue = stepone.Remove(stepone.Length - 1, 1);
                                        }
                                        if (nameIndx >= 0)
                                        {
                                            foundName = true;
                                            newname = splitData[nameIndx].Remove(0, 8).Replace("\"", "");
                                        }
                                        for (int x = 0; x < possibleTypes.Length; x++)
                                        {
                                            typeIndx = Array.FindIndex(splitData, element => element.Contains(possibleTypes[x]));
                                            if (typeIndx >= 0)
                                            {
                                                switch (x)
                                                {
                                                    case 0:
                                                        expectedClass = objectClass.Variable;
                                                        break;
                                                    case 1:
                                                        expectedClass = objectClass.List;
                                                        break;
                                                    default:
                                                        // Throw error, invalid type

                                                        return;
                                                        break;
                                                }
                                                typeLengthToRemove = possibleTypes[x].Length;
                                                foundType = true;
                                                break;
                                            }
                                        }

                                        if (foundName == true)
                                        {
                                            //////////////////////////////
                                        } else
                                        {
                                            // Throw error, name not found
                                            errorHandler.ThrowError(1200, null, newname, null, null, expectedFormat);
                                        }

                                        if (foundType == true)
                                        {
                                            string stepone = splitData[typeIndx].Remove(0, typeLengthToRemove);
                                            switch (stepone)
                                            {
                                                case "0":
                                                    valtype = 0;
                                                    break;
                                                case "1":
                                                    valtype = 1;
                                                    break;
                                                case "2":
                                                    valtype = 2;
                                                    break;
                                                case "3":
                                                    valtype = 3;
                                                    break;
                                                default:
                                                    // Throw error, invalid type

                                                    return;
                                                    break;
                                            }
                                        } else
                                        {
                                            // Throw error, type not found
                                            errorHandler.ThrowError(1200, null, "valid type", null, null, expectedFormat);

                                        }
                                        if ((foundName == true) && (foundType == true))
                                        {

                                            if (isValidReceivingObj == true)
                                            {
                                                if (namesInUse[receivingObject] == expectedClass)
                                                {
                                                    bool found = false;
                                                    switch (namesInUse[receivingObject])
                                                    {
                                                        case (objectClass.Variable):
                                                            if (foundValue == false)
                                                            {
                                                                errorHandler.ThrowError(2100, null, null, "variable has no value", "variable value", expectedFormat);
                                                                return;
                                                            }
                                                            foreach (LocalVariable variable in local_variables)
                                                            {
                                                                if (variable.Name == receivingObject)
                                                                {
                                                                    //variable.Name = newname;
                                                                    variable.Value = newvalue;
                                                                    switch (valtype)
                                                                    {
                                                                        case 0:
                                                                            variable.Type = VariableType.String;
                                                                            break;
                                                                        case 1:
                                                                            variable.Type = VariableType.Int;
                                                                            break;
                                                                        case 2:
                                                                            variable.Type = VariableType.Float;
                                                                            break;
                                                                        case 3:
                                                                            variable.Type = VariableType.Boolean;
                                                                            break;
                                                                    }
                                                                    found = true;
                                                                    valid_command = true;
                                                                    break;
                                                                }
                                                            }
                                                            break;
                                                        case (objectClass.List):
                                                            foreach (LocalList list in local_lists)
                                                            {
                                                                if (list.Name == receivingObject)
                                                                {
                                                                    list.items.Clear();
                                                                    //list.Name = newname;
                                                                    switch (valtype)
                                                                    {
                                                                        case 0:
                                                                            list.arrayType = ArrayType.String;
                                                                            break;
                                                                        case 1:
                                                                            list.arrayType = ArrayType.Int;
                                                                            break;
                                                                        case 2:
                                                                            list.arrayType = ArrayType.Float;
                                                                            break;
                                                                        case 3:
                                                                            list.arrayType = ArrayType.Boolean;
                                                                            break;
                                                                    }
                                                                    foreach (var newvar in foundElement)
                                                                    {
                                                                        LocalVariable newvariable = new LocalVariable();
                                                                        newvariable.Name = (newvar.Key);
                                                                        newvariable.Value = (newvar.Value);
                                                                        switch (valtype)
                                                                        {
                                                                            case 0:
                                                                                newvariable.Type = VariableType.String;
                                                                                break;
                                                                            case 1:
                                                                                newvariable.Type = VariableType.Int;
                                                                                break;
                                                                            case 2:
                                                                                newvariable.Type = VariableType.Float;
                                                                                break;
                                                                            case 3:
                                                                                newvariable.Type = VariableType.Boolean;
                                                                                break;
                                                                        }
                                                                        list.items.Add(newvariable);
                                                                    }
                                                                    valid_command = true;
                                                                    found = true;
                                                                    break;
                                                                }
                                                            }
                                                            break;
                                                        case (objectClass.DataPacket):
                                                            foreach (dataPacket datapckt in datapacketStack)
                                                            {
                                                                if (datapckt.ID == stringSerialized)
                                                                {

                                                                    valid_command = true;
                                                                }
                                                            }
                                                            break;
                                                        default:
                                                            string type_ = GetDescription(namesInUse[receivingObject]);
                                                            errorHandler.ThrowError(2100, null, null, type_, "variable, list, or datapacket", expectedFormat);
                                                            break;
                                                    }

                                                } else
                                                {
                                                    errorHandler.ThrowError(2100, null, null, $"{receivingObject} is a {GetDescription(namesInUse[receivingObject])}", $"{GetDescription(expectedClass)}", expectedFormat);
                                                }
                                            }
                                            else
                                            {
                                                // receiving object does not exist, we will create new object rather than find existing
                                                switch (expectedClass)
                                                {
                                                    case (objectClass.Variable):
                                                        if (foundValue == false)
                                                        {
                                                            errorHandler.ThrowError(2100, null, null, "variable has no value", "variable value", expectedFormat);
                                                            return;
                                                        }
                                                        LocalVariable newvar_ = new LocalVariable();
                                                        newvar_.Name = receivingObject;
                                                        newvar_.Value = newvalue;

                                                        switch (valtype)
                                                        {
                                                            case 0:
                                                                newvar_.Type = VariableType.String;
                                                                break;
                                                            case 1:
                                                                newvar_.Type = VariableType.Int;
                                                                break;
                                                            case 2:
                                                                newvar_.Type = VariableType.Float;
                                                                break;
                                                            case 3:
                                                                newvar_.Type = VariableType.Boolean;
                                                                break;
                                                        }
                                                        local_variables.Add(newvar_);
                                                        namesInUse.Add(receivingObject, objectClass.Variable);
                                                        valid_command = true;
                                                        break;
                                                    case (objectClass.List):
                                                        LocalList newlist_ = new LocalList();
                                                        newlist_.Name = receivingObject;
                                                        switch (valtype)
                                                        {
                                                            case 0:
                                                                newlist_.arrayType = ArrayType.String;
                                                                break;
                                                            case 1:
                                                                newlist_.arrayType = ArrayType.Int;
                                                                break;
                                                            case 2:
                                                                newlist_.arrayType = ArrayType.Float;
                                                                break;
                                                            case 3:
                                                                newlist_.arrayType = ArrayType.Boolean;
                                                                break;
                                                        }
                                                        foreach (var newvar in foundElement)
                                                        {
                                                            LocalVariable newvariable = new LocalVariable();
                                                            newvariable.Name = (newvar.Key);
                                                            newvariable.Value = (newvar.Value);
                                                            switch (valtype)
                                                            {
                                                                case 0:
                                                                    newvariable.Type = VariableType.String;
                                                                    break;
                                                                case 1:
                                                                    newvariable.Type = VariableType.Int;
                                                                    break;
                                                                case 2:
                                                                    newvariable.Type = VariableType.Float;
                                                                    break;
                                                                case 3:
                                                                    newvariable.Type = VariableType.Boolean;
                                                                    break;
                                                            }
                                                            newlist_.items.Add(newvariable);
                                                        }
                                                        local_lists.Add(newlist_);
                                                        namesInUse.Add(receivingObject, objectClass.List);
                                                        valid_command = true;
                                                        break;
                                                    case (objectClass.DataPacket):
                                                        foreach (dataPacket datapckt in datapacketStack)
                                                        {
                                                            if (datapckt.ID == stringSerialized)
                                                            {

                                                                valid_command = true;
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        string type_ = GetDescription(namesInUse[receivingObject]);
                                                        errorHandler.ThrowError(2100, null, null, type_, "variable, list, or datapacket", expectedFormat);
                                                        break;
                                                }
                                            }

                                        }

                                    } else
                                    {
                                        // Throw error as string likely does not contain serialized data
                                        errorHandler.ThrowError(2100, null, null, stringSerialized, "string variable holding serialize data", expectedFormat);

                                    }
                                }
                            }

                        } else
                        {
                            errorHandler.ThrowError(1200, null, stringSerialized, null, null, expectedFormat);
                        }

                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "json_deserialize", null, null, null, expectedFormat);
                    }
                }
#endif
#if HASGUICODE
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
                /// gui_item_port name containerobject                      <- transfers GUI object to specified container (tab conrol, frame, panel, etc)
                /// </summary>
                if (split_input[0].Equals("gui_mode", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "gui_mode on/off/reset";
                    if (split_input[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        consoleDirector.runningPermision = true;
                        GUIModeEnabled = true;
                        consoleDirector.InitializeGUIWindow(this);
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
                    entry_made = true;
                    if (split_input[1].Equals("Button", StringComparison.OrdinalIgnoreCase))
                    {
                        // new_gui_item button Buttontext Tasklist x y width height
                        // This will create a new GUI button named 'Buttontext', when clicked will execute the tasklist 'Tasklist' by name
                        // The button's x y coordinates and width height are taken as the last 4 parameters (all integers)
                        string expectedFormat = "new_gui_item button newname taskname XY:0,0 HW:0,0 Textcolor:White Backcolor:Black Text:Click me|" + Environment.NewLine + "Minimum required parameters are newname and taskname, all following are optional.";
                        if (split_input.Length >= 4)
                        {
                            //new_gui_item Button name Taslklist
                            bool nameinuse = GUIObjectsInUse.ContainsKey(split_input[2]);
                            if (nameinuse == false)
                            {
                                nameinuse = NameInUse(split_input[2]);
                            }
                            string btnName = split_input[2];
                            bool validCharacters = ContainsOnlyLettersAndNumbers(btnName);
                            string assignedTask = split_input[3];
                            bool validTask = false;
                            if (validCharacters == false)
                            {
                                errorHandler.ThrowError(1600, btnName);
                                return;
                            }
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
                                        foreach (string s in split_input.Skip(4))
                                        {
                                            if (extracting == true)
                                            {
                                                string q = SetVariableValue(s);
                                                foreach (char c in q)
                                                {
                                                    if (c != '|')
                                                    {
                                                        text += c;
                                                    } else
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
                                                                errorHandler.ThrowError(1400, $"Y", null, b[1], "integer value", expectedFormat);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                        }

                                                    }
                                                    else
                                                    {
                                                        errorHandler.ThrowError(1500, null, null, null, "X,Y", expectedFormat);
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
                                                                errorHandler.ThrowError(1400, $"width", null, b[1], "integer value", expectedFormat);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            errorHandler.ThrowError(1400, $"height", null, b[0], "integer value", expectedFormat);
                                                        }

                                                    }
                                                    else
                                                    {
                                                        errorHandler.ThrowError(1500, null, null, null, "height,width", expectedFormat);
                                                    }
                                                }
                                                if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    text = "";
                                                    string _placeholder = s.Remove(0, 5);
                                                    string b = SetVariableValue(_placeholder);
                                                    extracting = true;
                                                    bool pipefound = false;
                                                    foreach (char c in b)
                                                    {
                                                        if (c != '|')
                                                        {
                                                            text += c;
                                                        } else
                                                        {
                                                            pipefound = true;
                                                            break;
                                                        }
                                                    }
                                                    if (pipefound == true)
                                                    {
                                                        extracting = false;
                                                    } else
                                                    {
                                                        text += " ";
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
                                        if (extracting == true)
                                        {
                                            errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                                            Console.WriteLine("Must terminate Text: with vertical pipe |");
                                            return;
                                        }


                                        GUI_Button newbutton = new GUI_Button(this, btnName, tsklist, text, x, y, wid, hei, foregrn, backgrn);
                                        consoleDirector.GUIButtonsToAdd.Add(newbutton);
                                        consoleDirector.viewobjects.Add(newbutton);
                                        GUIObjectsInUse.Add(btnName, newbutton);
                                        namesInUse.Add(btnName, objectClass.GUIObj);
                                        valid_command = true;
                                        break;
                                    }
                                }
                                if (validTask == false)
                                {
                                    errorHandler.ThrowError(1200, null, "task " + assignedTask, null, null, expectedFormat);
                                }
                            } else
                            {
                                errorHandler.ThrowError(1300, null, null, btnName, null, expectedFormat);
                            }

                        } else
                        {
                            errorHandler.ThrowError(1100, "new_gui_item button", null, null, null, expectedFormat);
                        }

                    }
                    if (split_input[1].Equals("Textfield", StringComparison.OrdinalIgnoreCase))
                    {
                        string expectedFormat = "new_gui_item textfield newname XY:0,0 HW:0,0 Textcolor:White Backcolor:Black Readonly:True/False Multiline:True/False Text:Default text|" + Environment.NewLine + "Minimum required parameters are newname, all following are optional.";
                        if (split_input.Length >= 3)
                        {
                            bool validName = GUIObjectsInUse.ContainsKey(split_input[2]);
                            if (validName == false)
                            {
                                validName = NameInUse(split_input[2]);
                            }
                            string txtFieldName = split_input[2];
                            bool validCharacters = ContainsOnlyLettersAndNumbers(txtFieldName);
                            if (validCharacters == false)
                            {
                                errorHandler.ThrowError(1600, txtFieldName);
                                return;
                            }
                            if (validName == false)
                            {
                                int x = 0;
                                int y = 0;
                                int wid = 20;
                                int hei = 20;
                                string text = "Text";
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
                                                        errorHandler.ThrowError(1400, $"Y", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "X,Y", expectedFormat);
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
                                                        errorHandler.ThrowError(1400, $"width", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"height", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "height,width", expectedFormat);
                                            }
                                        }
                                        if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            text = "";
                                            string _placeholder = s.Remove(0, 5);
                                            string b = SetVariableValue(_placeholder);
                                            extracting = true;
                                            bool pipefound = false;
                                            foreach (char c in b)
                                            {
                                                if (c != '|')
                                                {
                                                    text += c;
                                                }
                                                else
                                                {
                                                    pipefound = true;
                                                    break;
                                                }
                                            }
                                            if (pipefound == true)
                                            {
                                                extracting = false;
                                            }
                                            else
                                            {
                                                text += " ";
                                            }
                                        }
                                        if (s.StartsWith("Multiline:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 10);
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
                                                errorHandler.ThrowError(1400, $"Readonly:", null, q, "True/False", expectedFormat);
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
                                                errorHandler.ThrowError(1400, $"Readonly:", null, q, "True/False", expectedFormat);
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
                                if (extracting == true)
                                {
                                    errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                                    return;
                                }

                                GUI_textfield newtextfield = new GUI_textfield(txtFieldName, x, y, wid, hei, isMultiline, text, isReadonly, foregrn, backgrn);
                                consoleDirector.GUITextFieldsToAdd.Add(newtextfield);
                                consoleDirector.viewobjects.Add(newtextfield);
                                GUIObjectsInUse.Add(newtextfield.GUIObjName, newtextfield);
                                valid_command = true;

                            } else
                            {
                                errorHandler.ThrowError(1300, null, null, txtFieldName, null, expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1100, "new_gui_item textfield", null, null, null, expectedFormat);
                        }
                    }
                    if (split_input[1].Equals("Menubar", StringComparison.OrdinalIgnoreCase))
                    {
                        string expectedFormat = "new_gui_item menubar newname Menuitems:File Menutasks:File,Open,functiona-File,Save,functionb" + Environment.NewLine + "Minimum required parameters are newname and at least 1 list with 1 item.";
                        if (split_input.Length >= 3)
                        {
                            bool minimumList = false;
                            string menuBarName = split_input[2];
                            List<string> topItemsToPadd = new List<string>();
                            List<string> subItemsToPass = new List<string>();
                            bool goodValsFound = false, badValsFound = false;
                            int goodValCount = 0;
                            string badVals = "";


                            bool nameIsUsed = GUIObjectsInUse.ContainsKey(menuBarName);
                            if (nameIsUsed == false)
                            {
                                nameIsUsed = NameInUse(menuBarName);
                            }
                            bool validCharacters = ContainsOnlyLettersAndNumbers(menuBarName);
                            if (validCharacters == false)
                            {
                                errorHandler.ThrowError(1600, menuBarName);
                                return;
                            }
                            if (nameIsUsed == false)
                            {
                                foreach (string s in split_input.Skip(3))
                                {
                                    if (s.StartsWith("Menuitems:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 10);
                                        string[] _listname = _placeholder.Split('-');
                                        foreach(string q in _listname)
                                        {
                                            string[] temp_ = q.Split(',');
                                            if (temp_.Length == 1)
                                            {
                                                // We assume this is a top level menu bar item that will have child menu items
                                                topItemsToPadd.Add(temp_[0]);
                                                goodValsFound = true;
                                                minimumList = true;
                                                goodValCount++;
                                            }
                                        }

                                        
                                    }
                                    if (s.StartsWith("Menutasks:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string _placeholder = s.Remove(0, 10);
                                        string[] _listname = _placeholder.Split('-');
                                        // MenuItems: must proceed MenuTasks: or we throw an error
                                        if (minimumList == false)
                                        {
                                            errorHandler.ThrowError(1100, "new_gui_item menubar", null, null, null, "Menuitems: must chronologically proceed Menutasks:"); // Override typical expectedFormat string to clarify in this instance
                                            return;
                                        }
                                        Dictionary<int, string> indexToExpectedCall = new Dictionary<int, string>();
                                        int yy = 0;
                                        foreach (string expectedItem in _listname)
                                        {
                                            string expectedCommand = "";
                                            string expectedCall = "";
                                            string replacement = "";

                                            string[] postSplit = expectedItem.Split(',');
                                            if (postSplit.Length > 2) 
                                            {
                                                string expectedParentName = postSplit[0];
                                                expectedCommand += postSplit[2];
                                                bool validCommandCall = ((namesInUse[expectedCommand] == objectClass.TaskList) || (namesInUse[expectedCommand] == objectClass.Function));
                                                bool validParent = false;
                                                string seekParent = topItemsToPadd.Find(l => l == expectedParentName);
                                                if (seekParent != null)
                                                {
                                                    validParent = true;
                                                }

                                                if ((validCommandCall == true)&&(validParent == true))
                                                {
                                                    switch (namesInUse[expectedCommand])
                                                    {
                                                        case objectClass.TaskList:
                                                            expectedCall += "task_execute ";
                                                            break;
                                                        case objectClass.Function:
                                                            expectedCall += "function ";
                                                            break;
                                                    }
                                                    foreach (string a in postSplit.SkipLast(1))
                                                    {
                                                        replacement += $"{a},";
                                                    }
                                                    replacement += $"{expectedCall}{expectedCommand}";
                                                    subItemsToPass.Add(expectedItem);
                                                    indexToExpectedCall.Add(yy, replacement);
                                                    goodValCount++;

                                                }
                                                if (validCommandCall == false)
                                                {
                                                    badValsFound = true;
                                                    badVals += $"{expectedItem} ({expectedCommand} is not valid task or function) ";
                                                }
                                                yy++;
                                            }
                                            else
                                            {
                                                // Append bad values
                                                badValsFound = true;
                                                badVals += $"{expectedItem} (expecting parent,child,task/function) ";
                                            }
                                        }

                                        // We're checking values that have already been cleared to proceed
                                        for(int p = 0; p < subItemsToPass.Count; p++)
                                        {
                                            if (indexToExpectedCall.ContainsKey(p))
                                            {
                                                subItemsToPass[p] = indexToExpectedCall[p];
                                            }
                                        }
                                    }
                                }
                                if (minimumList == true)
                                {
                                    if (goodValsFound == true)
                                    {

                                        GUI_Menubar newmenubar = new GUI_Menubar(this, menuBarName, topItemsToPadd, subItemsToPass);
                                        consoleDirector.GUIMenuBarsToAdd.Add(newmenubar);
                                        GUIObjectsInUse.Add(newmenubar.GUIObjName, newmenubar);
                                        valid_command = true;
                                        if (badValsFound == true)
                                        {
                                            badVals += $"<- item(s) were ignored. {goodValCount} items successfully created.";
                                            errorHandler.ThrowError(1100, null, null, badVals, null, expectedFormat);
                                        }
                                    } else
                                    {

                                    }
                                } else
                                {
                                    Console.WriteLine("Menubar requires minimum 1 list with 1 item.");
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1300, null, null, menuBarName, null, expectedFormat);
                            }

                        } else
                        {
                            errorHandler.ThrowError(1100, "new_gui_item menubar", null, null, null, expectedFormat);
                        }
                    }
                    if (split_input[1].Equals("Label", StringComparison.OrdinalIgnoreCase))
                    {
                        string expectedFormat = "new_gui_item label newname XY:0,0 HW:0,0 Textcolor:White Backcolor:Black Text:Default text|" + Environment.NewLine + "Minimum required parameters are newname, all following are optional.";

                        if (split_input.Length >= 3)
                        {
                            bool nameinuse = GUIObjectsInUse.ContainsKey(split_input[2]);
                            if (nameinuse == false)
                            {
                                nameinuse = NameInUse(split_input[2]);
                            }
                            string labelName = split_input[2];
                            bool validCharacters = ContainsOnlyLettersAndNumbers(labelName);
                            if (validCharacters == false)
                            {
                                errorHandler.ThrowError(1600, labelName);
                                return;
                            }
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
                                                        errorHandler.ThrowError(1400, $"Y", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "X,Y", expectedFormat);
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
                                                        errorHandler.ThrowError(1400, $"width", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"height", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "height,width", expectedFormat);
                                            }
                                        }
                                        if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            text = "";
                                            string _placeholder = s.Remove(0, 5);
                                            string b = SetVariableValue(_placeholder);
                                            extracting = true;
                                            bool pipefound = false;
                                            foreach (char c in b)
                                            {
                                                if (c != '|')
                                                {
                                                    text += c;
                                                }
                                                else
                                                {
                                                    pipefound = true;
                                                    break;
                                                }
                                            }
                                            if (pipefound == true)
                                            {
                                                extracting = false;
                                            }
                                            else
                                            {
                                                text += " ";
                                            }
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

                                if (extracting == true)
                                {
                                    errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                                    return;
                                }

                                GUI_Label newlabel_ = new GUI_Label(labelName, text, x, y, wid, hei, foregrn, backgrn);
                                consoleDirector.GUILabelsToAdd.Add(newlabel_);
                                consoleDirector.viewobjects.Add(newlabel_);
                                GUIObjectsInUse.Add(newlabel_.GUIObjName, newlabel_);
                                valid_command = true;

                            }
                            else
                            {
                                errorHandler.ThrowError(1300, null, null, labelName, null, expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1100, "new_gui_item label", null, null, null, expectedFormat);
                        }
                    }
                    if (split_input[1].Equals("Checkbox", StringComparison.OrdinalIgnoreCase))
                    {
                        string expectedFormat = "new_gui_item checkbox newname XY:0,0 HW:0,0 Textcolor:White Backcolor:Black LinkBool:boolvariable Checked:True/False Text:Default text|" + Environment.NewLine + "Minimum required parameters are newname, all following are optional.";

                        if (split_input.Length >= 3)
                        {
                            string expectedName = split_input[2];
                            bool nameInUse = GUIObjectsInUse.ContainsKey(expectedName);
                            if (nameInUse == false)
                            {
                                nameInUse = NameInUse(split_input[2]);
                            }
                            bool validCharacters = ContainsOnlyLettersAndNumbers(expectedName);
                            if (validCharacters == false)
                            {
                                errorHandler.ThrowError(1600, expectedName);
                                return;
                            }
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
                                                            errorHandler.ThrowError(1400, $"LinkBool:", null, expectedBoolVar, "bool variable", expectedFormat);
                                                        }
                                                    } else
                                                    {
                                                        errorHandler.ThrowError(1200, null, expectedBoolVar, null, null, expectedFormat);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1700, null, "1 bool variable", null, null, expectedFormat);
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
                                                        errorHandler.ThrowError(1400, $"Y", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "X,Y", expectedFormat);
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
                                                        errorHandler.ThrowError(1400, $"height", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "height,width", expectedFormat);
                                            }
                                        }
                                        if (s.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            text = "";
                                            string _placeholder = s.Remove(0, 5);
                                            string b = SetVariableValue(_placeholder);
                                            extracting = true;
                                            bool pipefound = false;
                                            foreach (char c in b)
                                            {
                                                if (c != '|')
                                                {
                                                    text += c;
                                                }
                                                else
                                                {
                                                    pipefound = true;
                                                    break;
                                                }
                                            }
                                            if (pipefound == true)
                                            {
                                                extracting = false;
                                            }
                                            else
                                            {
                                                text += " ";
                                            }
                                        }
                                        if (s.StartsWith("Checked:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string _placeholder = s.Remove(0, 8);
                                            string q = SetVariableValue(_placeholder).TrimEnd();
                                            string[] validValues = { "False", "false", "No", "no", "0", "True", "true", "Yes", "yes", "1" };
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
                                                errorHandler.ThrowError(1400, $"Checked:", null, q, "True/False", expectedFormat);
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

                                if (extracting == true)
                                {
                                    errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                                    return;
                                }
                                GUI_Checkbox newcheckbox = new GUI_Checkbox(this, expectedName, text, x, y, wid, hei, isChecked, hasLinkedBools, listOfBools_, foregrn, backgrn);
                                consoleDirector.GUICheckboxToAdd.Add(newcheckbox);
                                consoleDirector.viewobjects.Add(newcheckbox);
                                GUIObjectsInUse.Add(expectedName, newcheckbox);
                                valid_command = true;

                            } else
                            {
                                errorHandler.ThrowError(1300, null, null, expectedName, null, expectedFormat);
                            }
                        } else
                        {
                            errorHandler.ThrowError(1100, "new_gui_item checkbox", null, null, null, expectedFormat);
                        }
                    }
                    if (split_input[1].Equals("Tabcontainer", StringComparison.OrdinalIgnoreCase))
                    {
                        string expectedFormat = "new_gui_item tabcontainer newname XY:0,0 HW:0,0 Textcolor:White Backcolor:Black Tabs:name,text:name,text:name,text:etc..|" + Environment.NewLine + "Minimum required parameters are newname, all following are optional.";
                        if (split_input.Length > 3)
                        {
                            string expectedName = split_input[2];
                            bool nameInUse = GUIObjectsInUse.ContainsKey(expectedName);
                            if (nameInUse == false)
                            {
                                nameInUse = NameInUse(split_input[2]);
                            }
                            bool validCharacters = ContainsOnlyLettersAndNumbers(expectedName);
                            if (validCharacters == false)
                            {
                                errorHandler.ThrowError(1600, expectedName);
                                return;
                            }
                            if (nameInUse == false)
                            {
                                int x = 0;
                                int y = 0;
                                int wid = 10;
                                int hei = 8;
                                string text = "Checkbox";
                                Color backgrn = Color.Black;
                                Color foregrn = Color.White;
                                bool extracting = false;
                                bool foundBadVals = false, foundGoodVals = true;
                                int goodVals = 0;
                                string badVals = "";

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
                                                        errorHandler.ThrowError(1400, $"Y", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "X,Y", expectedFormat);
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
                                                        errorHandler.ThrowError(1400, $"height", null, b[1], "integer value", expectedFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    errorHandler.ThrowError(1400, $"X", null, b[0], "integer value", expectedFormat);
                                                }

                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1500, null, null, null, "height,width", expectedFormat);
                                            }
                                        }
                                        if (s.StartsWith("Tabs:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            text = "";
                                            string _placeholder = s.Remove(0, 5);
                                            string b = SetVariableValue(_placeholder);
                                            extracting = true;
                                            bool pipefound = false;
                                            foreach (char c in b)
                                            {
                                                if (c != '|')
                                                {
                                                    text += c;
                                                }
                                                else
                                                {
                                                    pipefound = true;
                                                    break;
                                                }
                                            }
                                            if (pipefound == true)
                                            {
                                                extracting = false;
                                            }
                                            else
                                            {
                                                text += " ";
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

                                if (extracting == true)
                                {
                                    errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                                    return;
                                }
                                string[] expectedNewTabs = text.Split('-');
                                // Brief check of names provided
                                List<int> badtabnames = new List<int>();
                                int xx = 0;
                                foreach(string q in expectedNewTabs)
                                {
                                    string[] temp_ = q.Split(',');
                                    if (GUIObjectsInUse.ContainsKey(temp_[0]))
                                    {
                                        // Bad tab name
                                        badtabnames.Add(xx);
                                        foundBadVals = true;
                                        badVals += $"{q} ";
                                    } else
                                    {
                                        foundGoodVals = true;
                                        goodVals++;
                                    }
                                    xx++;
                                }
                                foreach(int ii in badtabnames)
                                {
                                    expectedNewTabs[ii] = "BADVALUE";
                                }

                                if (foundGoodVals == true)
                                {
                                    GUI_TabContainer newtabcontainer = new GUI_TabContainer(this, expectedName, expectedNewTabs, x, y, wid, hei, foregrn, backgrn);
                                    consoleDirector.viewobjects.Add(newtabcontainer);
                                    GUIObjectsInUse.Add(expectedName, newtabcontainer);
                                    valid_command = true;
                                    if (foundBadVals == true)
                                    {
                                        badVals += $"<- item(s) were ignored. {goodVals} items successfully created.";
                                        errorHandler.ThrowError(1300, null, null, badVals, null, expectedFormat);
                                    }
                                } else
                                {
                                    errorHandler.ThrowError(1700, null, "properly formatted tab items where name is not already taken", null, null, expectedFormat);
                                }

                            }
                            else
                            {
                                errorHandler.ThrowError(1300, null, null, expectedName, null, expectedFormat);
                            }

                        } else
                        {
                            // Throw error invalid format
                            errorHandler.ThrowError(1100, "new_gui_item tabcontainer", null, null, null, expectedFormat);
                        }
                    }
                }
                if (split_input[0].Equals("gui_item_setwidth"))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_setwidth objectname number/percent/fill 1";
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
                                    foreach (GUI_BaseItem targetObject_ in consoleDirector.viewobjects)
                                    {
                                        if (targetObject_.GUIObjName == guiObjectName)
                                        {
                                            if (consoleDirector.objectCanRepos.ContainsKey(targetObject_.GUIObjectType))
                                            {
                                                targetObject_.SetWidth(xx, filval);
                                                foundAndChangedWidth = true;
                                                break;
                                            }
                                            else
                                            {
                                                // Throw error of wrong type then return;
                                                string badtype = GetDescription(targetObject_.GUIObjectType);
                                                errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                                return;
                                            }
                                        }
                                    }


                                    if (foundAndChangedWidth == false)
                                    {
                                        // Throw error of wrong type
                                        string badtype = GetDescription(guiobjecttype);
                                        errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                    }
                                    else
                                    {
                                        valid_command = true;
                                    }

                                }
                                else
                                {
                                    errorHandler.ThrowError(1400, "width", null, split_input[3], "integer value", expectedFormat);
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1400, "modifier", null, fv, "percent, fill, center", expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "gui_item_setwidth", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("gui_item_setheight"))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_setheight objectname number/percent/fill 1";
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
                                    foreach (GUI_BaseItem targetObject_ in consoleDirector.viewobjects)
                                    {
                                        if (targetObject_.GUIObjName == guiObjectName)
                                        {
                                            if (consoleDirector.objectCanRepos.ContainsKey(targetObject_.GUIObjectType))
                                            {
                                                targetObject_.SetHeight(xx, filval);
                                                foundAndChangedHeight = true;
                                                break;
                                            } else
                                            {
                                                // Throw error of wrong type then return;
                                                string badtype = GetDescription(targetObject_.GUIObjectType);
                                                errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                                return;
                                            }
                                        }
                                    }

                                    if (foundAndChangedHeight == false)
                                    {
                                        // Throw error of wrong type
                                        string badtype = GetDescription(guiobjecttype);
                                        errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);

                                    }
                                    else
                                    {
                                        valid_command = true;
                                    }

                                }
                                else
                                {
                                    errorHandler.ThrowError(1400, "height", null, split_input[3], "integer value", expectedFormat);
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1400, "modifier", null, fv, "percent, fill, center", expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "gui_item_setheight", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("gui_item_setx", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_setx objectname number/percent/fill/leftof/rightof 1/object" + Environment.NewLine + "number/percent/fill expects integer value, leftof/rightof expects an object name";
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
                                    string a_ = SetVariableValue(split_input[3]);
                                    bool validNumber = IsNumeric(a_);
                                    int xx = Int32.Parse(a_);
                                    if (validNumber == true)
                                    {
                                        bool foundAndChangedX = false;
                                        foreach (GUI_BaseItem targetObject_ in consoleDirector.viewobjects)
                                        {
                                            if (targetObject_.GUIObjName == guiObjectName)
                                            {
                                                if (consoleDirector.objectCanRepos.ContainsKey(targetObject_.GUIObjectType))
                                                {
                                                    targetObject_.SetXCoord(xx, filval);
                                                    foundAndChangedX = true;
                                                    break;
                                                } else
                                                {
                                                    // Throw error of wrong type
                                                    string badtype = GetDescription(targetObject_.GUIObjectType);
                                                    errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                                }
                                            }
                                        }

                                        if (foundAndChangedX == false)
                                        {
                                            // Throw error of wrong type
                                            string badtype = GetDescription(guiobjecttype);
                                            errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                        } else
                                        {
                                            valid_command = true;
                                        }

                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(1400, "X", null, split_input[3], "integer value", expectedFormat);
                                    }
                                } else
                                {
                                    string guidingObject = split_input[3];
                                    bool guidingObjExists = GUIObjectsInUse.ContainsKey(guidingObject);
                                    // Holy shit I could have just used GUIObjectType guiobj = GUIObjectsInUse[guidingObject].GUIObjectType; and skipped this retarded ass nesting I did wtf I need to fix this later
                                    if (guidingObjExists == true)
                                    {
                                        foreach (GUI_BaseItem guidingobj_ in consoleDirector.viewobjects)
                                        {
                                            if (guidingobj_.GUIObjName == guidingObject)
                                            {
                                                if (consoleDirector.objectCanRepos.ContainsKey(guidingobj_.GUIObjectType))
                                                {
                                                    foreach (GUI_BaseItem targetObject in consoleDirector.viewobjects)
                                                    {
                                                        if (targetObject.GUIObjName == guiObjectName)
                                                        {
                                                            if (consoleDirector.objectCanRepos.ContainsKey(targetObject.GUIObjectType))
                                                            {
                                                                targetObject.SetToLeftOrRight(guidingobj_.objview, filval);
                                                                valid_command = true;
                                                                break;
                                                            } else
                                                            {
                                                                // Throw error for wrong type
                                                                string badtype = GetDescription(targetObject.GUIObjectType);
                                                                errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                                                return;

                                                            }
                                                        }
                                                    }

                                                } else
                                                {
                                                    // Throw error for wrong type
                                                    string badtype = GetDescription(guidingobj_.GUIObjectType);
                                                    errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                                    return;

                                                }
                                            }
                                        }

                                    } else
                                    {
                                        errorHandler.ThrowError(1200, null, guidingObject, null, null, expectedFormat);
                                    }
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1400, "modifier", null, fv, "percent, fill, center, leftof, rightof", expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "gui_item_setx", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("gui_item_sety", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_sety objectname number/percent/fill 1";
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
                                string a_ = SetVariableValue(split_input[3]);
                                bool validNumber = IsNumeric(a_);
                                int xx = Int32.Parse(a_);
                                if (validNumber == true)
                                {
                                    bool foundAndChangedY = false;
                                    foreach (GUI_BaseItem targetObject_ in consoleDirector.viewobjects)
                                    {
                                        if (targetObject_.GUIObjName == guiObjectName)
                                        {
                                            if (consoleDirector.objectCanRepos.ContainsKey(targetObject_.GUIObjectType))
                                            {
                                                targetObject_.SetYCoord(xx, filval);
                                                foundAndChangedY = true;
                                                break;
                                            } else
                                            {
                                                // Throw error of wrong type
                                                string badtype = GetDescription(targetObject_.GUIObjectType);
                                                errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                                return;
                                            }
                                        }
                                    }

                                    if (foundAndChangedY == false)
                                    {
                                        // Throw error of wrong type
                                        string badtype = GetDescription(guiobjecttype);
                                        errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);

                                    }
                                    else
                                    {
                                        valid_command = true;
                                    }

                                }
                                else
                                {
                                    errorHandler.ThrowError(1400, "width", null, split_input[3], "integer value", expectedFormat);
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1400, "modifier", null, fv, "percent, fill, center", expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "gui_item_sety", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("gui_item_gettext", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_gettext objectname strvariable";

                    if (split_input.Length == 3)
                    {
                        bool validVriable = false;
                        bool guiObjectExists = false;
                        string guiObjectName = split_input[1].TrimEnd();
                        string variableName = split_input[2].TrimEnd();
                        validVriable = LocalVariableExists(variableName);
                        guiObjectExists = GUIObjectsInUse.ContainsKey(guiObjectName);

                        if ((validVriable == true) && (guiObjectExists == true))
                        {

                            GUIObjectType objtype = GUIObjectsInUse[guiObjectName].GUIObjectType;

                            foreach (GUI_BaseItem targetObject in consoleDirector.viewobjects)
                            {
                                if (targetObject.GUIObjName == guiObjectName)
                                {
                                    if (consoleDirector.objectHasTextAccess.ContainsKey(targetObject.GUIObjectType))
                                    {
                                        LocalVariable expectedStringVar = local_variables.Find(u => u.Name == variableName);
                                        if (expectedStringVar != null)
                                        {
                                            if (expectedStringVar.Type == VariableType.String)
                                            {
                                                expectedStringVar.Value = targetObject.GetText();
                                                valid_command = true;
                                                break;
                                            } else
                                            {
                                                // Not a valid string variable
                                                errorHandler.ThrowError(2100, null, null, variableName, "string variable", expectedFormat);
                                                break;
                                            }
                                        } else
                                        {
                                            // Redundant error throw - item not found
                                            errorHandler.ThrowError(1200, null, variableName, null, null, expectedFormat);
                                            return;
                                        }
                                    } else
                                    {
                                        // Throw type error then return;
                                        string badtype = GetDescription(targetObject.GUIObjectType);
                                        errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                        return;
                                    }
                                }
                            }

                        } else if ((validVriable == true) && (guiObjectExists == false)) {
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                        } else if ((validVriable == false) && (guiObjectExists == true)) {
                            errorHandler.ThrowError(1200, null, variableName, null, null, expectedFormat);
                        } else
                        {
                            errorHandler.ThrowError(1200, null, guiObjectName + ", " + variableName, null, null, expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "gui_item_gettext", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("gui_item_settext", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_settext objectname value";
                    if (split_input.Length >= 3)
                    {
                        string guiObjectName = split_input[1];
                        string textToSetTo = "";
                        StringBuilder newstring = new StringBuilder();
                        if (split_input.Length >= 3)
                        {
                            int pos = 3;
                            int len = split_input.Length;
                            foreach (string s in split_input.Skip(2))
                            {
                                newstring.Append(SetVariableValue(s));
                                if (pos != len)
                                {
                                    newstring.Append(" ");
                                }
                                pos++;
                            }

                            textToSetTo = SetVariableValue(newstring.ToString());
                        } else if (split_input.Length == 3) {
                            textToSetTo = SetVariableValue(split_input[2]).TrimEnd();
                        }
                        bool guiObjectExists = (GUIObjectsInUse.ContainsKey(guiObjectName));
                        if (guiObjectExists == true)
                        {
                            foreach (GUI_BaseItem targetObject in consoleDirector.viewobjects)
                            {
                                if (targetObject.GUIObjName == guiObjectName)
                                {
                                    if (consoleDirector.objectHasTextAccess.ContainsKey(targetObject.GUIObjectType))
                                    {
                                        targetObject.SetText(textToSetTo);
                                        valid_command = true;
                                        break;
                                    }
                                    else
                                    {
                                        // Throw type error then return;
                                        string badtype = GetDescription(targetObject.GUIObjectType);
                                        errorHandler.ThrowError(2100, null, null, badtype, "GUI object with text field", expectedFormat);
                                        return;
                                    }
                                }
                            }

                        }
                        else if (guiObjectExists == false)
                        {
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1100, "gui_item_settext", null, null, null, expectedFormat);
                    }
                }
                if (split_input[0].Equals("gui_item_port", StringComparison.OrdinalIgnoreCase))
                {
                    entry_made = true;
                    string expectedFormat = "gui_item_port guiobject containerobject";
                    if (split_input.Length == 3)
                    {
                        string guiObjectName = split_input[1], containerObjectName = split_input[2];
                        bool guiObjectExists = (GUIObjectsInUse.ContainsKey(guiObjectName)), containerExists = (GUIObjectsInUse.ContainsKey(guiObjectName));
                        GUIObjectType guiobjtype = default, containertype = default;
                        bool validType = false, validContainer = false;
                        if (guiObjectExists == true)
                        {
                            guiobjtype = GUIObjectsInUse[guiObjectName].GUIObjectType;
                            validType = consoleDirector.objectCanRepos.ContainsKey(guiobjtype);
                            if (validType != true)
                            {
                                // Throw error wrong type
                                string badtype = GetDescription(guiobjtype);
                                errorHandler.ThrowError(2100, null, null, badtype, "portable GUI object", expectedFormat);
                            }
                        } else
                        {
                            // Throw error object does not exist
                            errorHandler.ThrowError(1200, null, guiObjectName, null, null, expectedFormat);
                            return;
                        }
                        if (containerExists == true)
                        {
                            containertype = GUIObjectsInUse[containerObjectName].GUIObjectType;
                            validContainer = consoleDirector.objectIsContainer.ContainsKey(containertype);
                            if (validType != true)
                            {
                                // Throw error wrong type
                                string badtype_ = GetDescription(containertype);
                                errorHandler.ThrowError(2100, null, null, badtype_, "container object", expectedFormat);
                            }
                        } else
                        {
                            // Throw error object does not exist
                            errorHandler.ThrowError(1200, null, containerObjectName, null, null, expectedFormat);
                            return;
                        }

                        if ((validType == true) && (validContainer == true))
                        {
                            
                            foreach(GUI_BaseItem targetObject in consoleDirector.viewobjects)
                            {
                                if(targetObject.GUIObjName == guiObjectName)
                                {
                                    foreach(GUI_BaseItem newcontainer in consoleDirector.viewobjects)
                                    {
                                        if (newcontainer.GUIObjName == containerObjectName)
                                        {
                                            newcontainer.PortItem(targetObject);
                                            valid_command = true;
                                            return;
                                        }
                                    }

                                }
                            }


                        }
                    } else
                    {
                        errorHandler.ThrowError(1100, "gui_item_port", null, null, null, expectedFormat);
                    }

                }
                if (split_input[0].Equals("gui_keypress_event", StringComparison.OrdinalIgnoreCase))
                {
                entry_made = true;
                string expectedFormat = "gui_keypress_event taskname key";
                if (split_input.Length == 3)
                {
                    string expectedTaskName = split_input[1];
                    string keyInput = split_input[2].ToLower().TrimEnd();
                    Key keyToPass;
                    bool validTask = false;
                    bool validKey = true;
                    TaskList templist = tasklists_inuse.Find(x => x.taskName == expectedTaskName);
                    if (templist != null)
                    {
                        validTask = true;
                        switch (keyInput)
                        {
                            case "space":
                                keyToPass = Key.Space;
                                break;
                            case "enter":
                                keyToPass = Key.Enter;
                                break;
                            case "escape":
                                keyToPass = Key.Esc;
                                break;
                            case "tab":
                                keyToPass = Key.Tab;
                                break;
                            case "backspace":
                                keyToPass = Key.Backspace;
                                break;
                            case "delete":
                                keyToPass = Key.DeleteChar;
                                break;
                            case "insert":
                                keyToPass = Key.InsertChar;
                                break;
                            case "home":
                                keyToPass = Key.Home;
                                break;
                            case "end":
                                keyToPass = Key.End;
                                break;
                            case "pageup":
                                keyToPass = Key.PageUp;
                                break;
                            case "pagedown":
                                keyToPass = Key.PageDown;
                                break;
                            case "up":
                                keyToPass = Key.CursorUp;
                                break;
                            case "down":
                                keyToPass = Key.CursorDown;
                                break;
                            case "left":
                                keyToPass = Key.CursorLeft;
                                break;
                            case "right":
                                keyToPass = Key.CursorRight;
                                break;
                            case "f1":
                                keyToPass = Key.F1;
                                break;
                            case "f2":
                                keyToPass = Key.F2;
                                break;
                            case "f3":
                                keyToPass = Key.F3;
                                break;
                            case "f4":
                                keyToPass = Key.F4;
                                break;
                            case "f5":
                                keyToPass = Key.F5;
                                break;
                            case "f6":
                                keyToPass = Key.F6;
                                break;
                            case "f7":
                                keyToPass = Key.F7;
                                break;
                            case "f8":
                                keyToPass = Key.F8;
                                break;
                            case "f9":
                                keyToPass = Key.F9;
                                break;
                            case "f10":
                                keyToPass = Key.F10;
                                break;
                            case "f11":
                                keyToPass = Key.F11;
                                break;
                            case "f12":
                                keyToPass = Key.F12;
                                break;
                            case "f13":
                                keyToPass = Key.F13;
                                break;
                            case "f14":
                                keyToPass = Key.F14;
                                break;
                            case "f15":
                                keyToPass = Key.F15;
                                break;
                            case "a":
                                keyToPass = Key.A;
                                break;
                            case "b":
                                keyToPass = Key.B;
                                break;
                            case "c":
                                keyToPass = Key.C;
                                break;
                            case "d":
                                keyToPass = Key.D;
                                break;
                            case "e":
                                keyToPass = Key.E;
                                break;
                            case "f":
                                keyToPass = Key.F;
                                break;
                            case "g":
                                keyToPass = Key.G;
                                break;
                            case "h":
                                keyToPass = Key.H;
                                break;
                            case "i":
                                keyToPass = Key.I;
                                break;
                            case "j":
                                keyToPass = Key.J;
                                break;
                            case "k":
                                keyToPass = Key.K;
                                break;
                            case "l":
                                keyToPass = Key.L;
                                break;
                            case "m":
                                keyToPass = Key.M;
                                break;
                            case "n":
                                keyToPass = Key.N;
                                break;
                            case "o":
                                keyToPass = Key.O;
                                break;
                            case "p":
                                keyToPass = Key.P;
                                break;
                            case "q":
                                keyToPass = Key.Q;
                                break;
                            case "r":
                                keyToPass = Key.R;
                                break;
                            case "s":
                                keyToPass = Key.S;
                                break;
                            case "t":
                                keyToPass = Key.T;
                                break;
                            case "u":
                                keyToPass = Key.U;
                                break;
                            case "v":
                                keyToPass = Key.V;
                                break;
                            case "w":
                                keyToPass = Key.W;
                                break;
                            case "x":
                                keyToPass = Key.X;
                                break;
                            case "y":
                                keyToPass = Key.Y;
                                break;
                            case "z":
                                keyToPass = Key.Z;
                                break;
                            case "0":
                                keyToPass = Key.D0;
                                break;
                            case "1":
                                keyToPass = Key.D1;
                                break;
                            case "2":
                                keyToPass = Key.D2;
                                break;
                            case "3":
                                keyToPass = Key.D3;
                                break;
                            case "4":
                                keyToPass = Key.D4;
                                break;
                            case "5":
                                keyToPass = Key.D5;
                                break;
                            case "6":
                                keyToPass = Key.D6;
                                break;
                            case "7":
                                keyToPass = Key.D7;
                                break;
                            case "8":
                                keyToPass = Key.D8;
                                break;
                            case "9":
                                keyToPass = Key.D9;
                                break;
                            default:
                                // Handle unknown key
                                keyToPass = Key.Null;
                                validKey = false;
                                break;
                        }
                        if (validKey == false)
                        {
                            errorHandler.ThrowError(1200, null, "key " + keyInput, null, null, expectedFormat);
                        }

                        if (validTask == true && validKey == true)
                        {
                            consoleDirector.addKeyPressFunction(templist, keyToPass);
                            valid_command = true;
                        }

                    } else
                    {
                        errorHandler.ThrowError(1200, null, "task " + expectedTaskName, null, null, expectedFormat);
                    }

                } else
                {
                    errorHandler.ThrowError(1100, "gui_keypress_event", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("msgbox", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                if (GUIModeEnabled == true)
                {
                    string expectedFormat = "msgbox Text:Default message| Title:Default title| Buttons:OK/YESNO,boolvariable" + Environment.NewLine + "If button set it yesno, a bool variable is expected after comma. If button set is ok, nothing else is expected.";
                    bool extracting = false;
                    bool extractingTitle = false;
                    bool hasText = false;
                    bool hasTitle = false;
                    int[] hasButtons = { 0, 1, 2 }; // 0 is no, 1 is button "OK", 2 is button "YES,NO" which returns a value
                    int selectedButton = 0;

                    string expected_variable = "";
                    LocalVariable bool_forYesNo = null;
                    string variableName_ = "";

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
                                string b = SetVariableValue(_placeholder);
                                extracting = true;
                                extractingTitle = false;
                                hasText = true;
                                bool pipefound = false;
                                foreach (char c in b)
                                {
                                    if (c != '|')
                                    {
                                        text += c;
                                    }
                                    else
                                    {
                                        pipefound = true;
                                        break;
                                    }
                                }
                                if (pipefound == true)
                                {
                                    extracting = false;
                                }
                                else
                                {
                                    text += " ";
                                }
                            }
                            if (s.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                            {
                                string _placeholder = s.Remove(0, 6);
                                string b = SetVariableValue(_placeholder);
                                extractingTitle = true;
                                extracting = true;
                                hasTitle = true;
                                bool pipefound = false;
                                foreach (char c in b)
                                {
                                    if (c != '|')
                                    {
                                        title += c;
                                    }
                                    else
                                    {
                                        pipefound = true;
                                        break;
                                    }
                                }
                                if (pipefound == true)
                                {
                                    extracting = false;
                                    extractingTitle = false;
                                }
                                else
                                {
                                    title += " ";
                                }
                            }
                            if (s.StartsWith("Buttons:", StringComparison.OrdinalIgnoreCase))
                            {
                                string _placeholder = s.Remove(0, 8);
                                string a = ConvertNumericalVariable(_placeholder);
                                if (a.StartsWith("YESNO,", StringComparison.OrdinalIgnoreCase))
                                {
                                    selectedButton = 2;
                                    expected_variable = a.Remove(0, 6).TrimEnd();
                                    bool validValue = LocalVariableExists(expected_variable);
                                    if (validValue == true)
                                    {
                                        LocalVariable var_totakevalue = local_variables.Find(locvar => locvar.Name == expected_variable);
                                        if (var_totakevalue != null)
                                        {
                                            if (var_totakevalue.Type != VariableType.Boolean)
                                            {
                                                errorHandler.ThrowError(2100, null, null, expected_variable, "bool variable", expectedFormat);
                                                break;
                                            } else
                                            {
                                                bool_forYesNo = var_totakevalue;
                                            }
                                        }
                                    } else
                                    {
                                        errorHandler.ThrowError(1200, null, expected_variable, null, null, expectedFormat);
                                        break;
                                    }
                                }
                                if (a.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                                {
                                    selectedButton = 1;
                                }

                                if (selectedButton <= 0)
                                {
                                    errorHandler.ThrowError(1400, $"Buttons:", null, _placeholder, "OK YESNO,bool", expectedFormat);
                                }

                            }
                        }
                    }
                    if (extracting == true)
                    {
                        errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                        return;
                    }
                    if (extractingTitle == true)
                    {
                        errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                        return;
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
                                        valid_command = true;
                                        Terminal.Gui.Application.MainLoop.Invoke(() =>
                                            {
                                                consoleDirector.ok_msgbox(title, text);
                                            });
                                        break;
                                    case 2:
                                        valid_command = true;
                                        string varname = bool_forYesNo.Name;
                                        Terminal.Gui.Application.MainLoop.Invoke(() =>
                                        {
                                            consoleDirector.yesno_msgbox(title, text, varname);
                                        });
                                        break;
                                }

                            } else
                            {
                                // This shouldn't hit but there's never a thing as too much redundancy
                                errorHandler.ThrowError(1400, $"Buttons:", null, "provided value", "OK YESNO,bool", expectedFormat);
                            }
                        } else
                        {
                            errorHandler.ThrowError(1700, null, "Title:", null, null, expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1700, null, "Text:", null, null, expectedFormat);
                    }

                } else
                {
                    errorHandler.ThrowError(1800, null, null, null, null, null);
                }


            }
                if (split_input[0].Equals("gui_savedialog", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                if (GUIModeEnabled == true)
                {
                    string expectedFormat = "gui_savedialog strvariable Text:Default text| Title:Default title| Filetypes:listname";
                    bool extracting = false;
                    bool extractingTitle = false;
                    bool hasText = false;
                    bool hasTitle = false;
                    bool validVariable = false;

                    LocalList expectedFileTypes = null;

                    string expectedVariableName_ = SetVariableValue(split_input[1]);
                    string text = "";
                    string title = "";

                    validVariable = LocalVariableExists(expectedVariableName_);

                    if (validVariable == true)
                    {
                        LocalVariable temp_ = local_variables.Find(e => e.Name == expectedVariableName_);
                        if (temp_.Type != VariableType.String)
                        {
                            errorHandler.ThrowError(2100, null, null, expectedVariableName_, "string variable", expectedFormat);
                            return;
                        }

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
                                        }
                                        else if (extractingTitle == false)
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
                                if (s.StartsWith("Filetypes:", StringComparison.OrdinalIgnoreCase))
                                {
                                    string _placeholder = s.Remove(0, 10);
                                    bool listExists, listIsStringList = false;
                                    LocalList templist = local_lists.Find(x => x.Name == _placeholder);
                                    if (templist != null)
                                    {
                                        listExists = true;
                                        if (templist.arrayType == ArrayType.String) { listIsStringList = true; }

                                        if ((listExists == true) && (listIsStringList == true))
                                        {
                                            expectedFileTypes = templist;
                                        }

                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(1200, null, _placeholder, null, null, expectedFormat);
                                    }

                                }

                            }
                        }

                        if (extracting == true)
                        {
                            errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                            return;
                        }
                        if (extractingTitle == true)
                        {
                            errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                            return;
                        }

                        if (hasText == true)
                        {
                            if (hasTitle == true)
                            {
                                valid_command = true;
                                consoleDirector.showsaveDialog(title, text, expectedVariableName_, expectedFileTypes);
                            }
                            else
                            {
                                errorHandler.ThrowError(1700, null, "Title:", null, null, null);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1700, null, "Text:", null, null, null);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1200, null, expectedVariableName_, null, null, expectedFormat);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1800, null, null, null, null, null);
                }
            }
                if (split_input[0].Equals("gui_opendialog", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                if (GUIModeEnabled == true)
                {
                    string expectedFormat = "gui_savedialog strvariable Text:Default text| Title:Default title| Filetypes:listname";
                    bool extracting = false;
                    bool extractingTitle = false;
                    bool hasText = false;
                    bool hasTitle = false;
                    bool validVariable = false;

                    LocalList expectedFileTypes = null;

                    string expectedVariableName_ = SetVariableValue(split_input[1]);
                    string text = "";
                    string title = "";

                    validVariable = LocalVariableExists(expectedVariableName_);
                    if (validVariable == true)
                    {
                        LocalVariable temp_ = local_variables.Find(e => e.Name == expectedVariableName_);
                        if (temp_.Type != VariableType.String)
                        {
                            errorHandler.ThrowError(2100, null, null, expectedVariableName_, "string variable", expectedFormat);
                            return;
                        }


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
                                        }
                                        else if (extractingTitle == false)
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
                                if (s.StartsWith("Filetypes:", StringComparison.OrdinalIgnoreCase))
                                {
                                    string _placeholder = s.Remove(0, 10);
                                    bool listExists, listIsStringList = false;
                                    LocalList templist = local_lists.Find(x => x.Name == _placeholder);
                                    if (templist != null)
                                    {
                                        listExists = true;
                                        if (templist.arrayType == ArrayType.String) { listIsStringList = true; }

                                        if ((listExists == true) && (listIsStringList == true))
                                        {
                                            expectedFileTypes = templist;
                                        }

                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(1200, null, _placeholder, null, null, expectedFormat);
                                    }

                                }

                            }
                        }

                        if (extracting == true)
                        {
                            errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                            return;
                        }
                        if (extractingTitle == true)
                        {
                            errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                            return;
                        }

                        if (hasText == true)
                        {
                            if (hasTitle == true)
                            {
                                valid_command = true;
                                consoleDirector.showopenDialog(title, text, expectedVariableName_, expectedFileTypes);
                            }
                            else
                            {
                                errorHandler.ThrowError(1700, null, "Title:", null, null, null);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1700, null, "Text:", null, null, null);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1200, null, expectedVariableName_, null, null, expectedFormat);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1800, null, null, null, null, null);
                }
            }
#endif

#if HASLIST
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
                entry_made = true;
                string expectedFormat = "new_list variabletype newname";
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
                        if (properName == false)
                        {
                            errorHandler.ThrowError(1600, listName);
                            return;
                        }
                        if (alreadyExists == true)
                        {
                            errorHandler.ThrowError(1300, null, null, listName, null, expectedFormat);
                            return;
                        }
                        if ((properName == true) && (alreadyExists == false))
                        {
                            LocalList newArray = new LocalList();
                            newArray.Name = listName;
                            newArray.arrayType = new_arrayType;
                            local_lists.Add(newArray);
                            namesInUse.Add(listName, objectClass.List);
                            valid_command = true;
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1400, $"variablr type", null, arrayType, "string, int, float, bool", expectedFormat);
                    }

                }
                else
                {
                    errorHandler.ThrowError(1100, "new_list", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("list_add", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "list_add listname variablename(s)";
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
                        foreach (LocalList list in local_lists)
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
                                                valid_command = true;
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
                                    errorHandler.ThrowError(1200, null, badVariable, null, null, expectedFormat);
                                }
                                break;
                            }
                        }
                        if (foundList == false) { errorHandler.ThrowError(1200, null, listName, null, null, expectedFormat); }
                    }
                    else if (split_input.Length == 3)
                    {
                        if (varExists == true)
                        {
                            foreach (LocalList array in local_lists)
                            {
                                if (array.Name == listName)
                                {
                                    foreach (LocalVariable localVar in local_variables)
                                    {
                                        if (localVar.Name == varName)
                                        {
                                            valid_command = true;
                                            array.itemAdd(localVar);
                                            break;
                                        }
                                    }
                                    arrayExists = true;
                                }
                            }
                            if (arrayExists == false)
                            {
                                errorHandler.ThrowError(1200, null, listName, null, null, expectedFormat);
                            }
                        }
                        else
                        {
                            errorHandler.ThrowError(1200, null, varName, null, null, expectedFormat);
                        }
                    }
                    else { errorHandler.ThrowError(1100, "list_add", null, null, null, expectedFormat); }
                }
            }
                if (split_input[0].Equals("list_remove", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "list_remove listname variablename";
                if (split_input.Length >= 3)
                {
                    string listName = split_input[1];
                    string varName = split_input[2];
                    bool varExists = LocalVariableExists(varName);
                    bool arrayExists = false;
                    if (varExists == true)
                    {
                        foreach (LocalList array in local_lists)
                        {
                            if (array.Name == listName)
                            {
                                array.itemRemove(varName);
                                valid_command = true;
                                break;
                            }
                        }
                        if (arrayExists == false)
                        {
                            errorHandler.ThrowError(1200, null, listName, null, null, expectedFormat);
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1200, null, varName, null, null, expectedFormat);
                    }
                } else
                {
                    errorHandler.ThrowError(1100, "list_remove", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("list_setall", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "list_setall listname value";
                if (split_input.Length >= 2)
                {
                    bool arrayExist = false;
                    string arrayName = split_input[1];

                    foreach (LocalList localLists in local_lists)
                    {
                        if (localLists.Name == arrayName)
                        {
                            switch (localLists.arrayType)
                            {
                                // The array type will determine how we handle the input
                                case ArrayType.Float:
                                    if (split_input.Length > 3)
                                    {
                                        errorHandler.ThrowError(1100, "list_setall", null, null, null, expectedFormat);
                                    }
                                    else
                                    {
                                        string a_ = SetVariableValue(split_input[2]);
                                        string b_ = ConvertNumericalVariable(a_);
                                        bool isfloat = float.TryParse(b_, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float result);
                                        if (isfloat == true)
                                        {
                                            localLists.SetAllWithValue(b_);
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, $"float", null, b_, "float value", expectedFormat);
                                        }
                                    }
                                    break;
                                case ArrayType.Int:
                                    if (split_input.Length > 3)
                                    {
                                        errorHandler.ThrowError(1100, "list_setall", null, null, null, expectedFormat);
                                    }
                                    else
                                    {
                                        string a_ = SetVariableValue(split_input[2]);
                                        string b_ = ConvertNumericalVariable(a_);
                                        bool isInt = IsNumeric(b_);
                                        if (isInt == true)
                                        {
                                            localLists.SetAllWithValue(b_);
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, $"integer", null, b_, "integer value", expectedFormat);
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
                                    valid_command = true;
                                    break;
                                case ArrayType.Boolean:
                                    if (split_input.Length > 3)
                                    {
                                        errorHandler.ThrowError(1100, "list_setall", null, null, null, expectedFormat);
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
                                            valid_command = true;
                                        }
                                        else
                                        {
                                            errorHandler.ThrowError(1400, $"bool", null, split_input[2], "True/False", expectedFormat);
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
                        errorHandler.ThrowError(1200, null, arrayName, null, null, expectedFormat);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "list_setall", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("list_printall", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "list_printall listname";
                string listName = split_input[1];
                if (split_input.Length == 2)
                {
                    bool listExists = false;
                    foreach (LocalList list_ in local_lists)
                    {
                        if (list_.Name == listName)
                        {
                            listExists = true;
                            list_.PrintAll();
                            valid_command = true;
                            break;
                        }
                    }
                    if (listExists == false)
                    {
                        errorHandler.ThrowError(1200, null, listName, null, null, expectedFormat);
                    }
                } else {
                    errorHandler.ThrowError(1100, "list_printall", null, null, null, expectedFormat);
                }
            }
#endif

#if HASIFILESYSTEM
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

                if (split_input[0].StartsWith("filesystem_", StringComparison.OrdinalIgnoreCase))
            {
                bool validCommand = false, hasOut = false, hasPlaceholder = false, expectsReturn = false, variableExists = false, validVariable = false;
                int inputLen = split_input.Length;
                string _placeholder = split_input[0].Remove(0, 11).ToLower(), writtenOut = "";
                string expectedFormat = "will work this soon";
                string input1 = SetVariableValue(split_input[1]);

                if (filesystem.commandDirectoryVoid.ContainsKey(_placeholder))
                {
                    entry_made = true;
                    if (inputLen < 2)
                    {
                        // Throw format error
                        errorHandler.ThrowError(1100, split_input[0], null, null, null, expectedFormat);
                        return;
                    }
                    if (filesystem.actionsWithPlaceholder.ContainsKey(_placeholder)) { hasPlaceholder = true; }
                    if (filesystem.outputsToFile.ContainsKey(_placeholder))
                    {
                        hasOut = true;
                        // Rebuild string
                        string temp_ = split_input[0] + " " + split_input[1] + " ";
                        string temp_2 = input.Replace(temp_, "");
                        writtenOut += SetVariableValue(temp_2);
                    }
                    if ((hasPlaceholder == false) && (inputLen < 3)) { errorHandler.ThrowError(1100, split_input[0], null, null, null, expectedFormat); return; } // <----- Throw format error

                    if (hasPlaceholder == true)
                    {
                        filesystem.commandDirectoryVoid[_placeholder].Invoke(input1, "");
                        valid_command = true;
                    } else if (hasOut == true)
                    {
                        filesystem.commandDirectoryVoid[_placeholder].Invoke(input1, writtenOut);
                        valid_command = true;
                    } else
                    {
                        string input2 = SetVariableValue(split_input[2]);
                        filesystem.commandDirectoryVoid[_placeholder].Invoke(input1,input2);
                        valid_command = true;
                    }
                }

                if (filesystem.commandDirectoryReturnString.ContainsKey(_placeholder))
                {
                    entry_made = true;
                    objectClass expectedReturnType = default;
                    string expectedObjectName = split_input[2];
                    string expectedPath = split_input[1];
                    switch (_placeholder)
                    {
                        case "readtolist":
                            expectedReturnType = objectClass.List;
                            string[] filecontents = filesystem.commandDirectoryReturnString[_placeholder].Invoke(expectedPath, "");
                            if (filecontents == null)
                            {
                                // Throw error then return
                                return;
                            }
                            string filename = filesystem.fileName(expectedPath);
                            LocalList expectList = local_lists.Find(str => str.Name == expectedObjectName);
                            if (expectList != null)
                            {
                                if (expectList.arrayType == ArrayType.String)
                                {
                                    if (expectList.items.Count == 0)
                                    {
                                        int x = 0;
                                        foreach (string w in filecontents)
                                        {
                                            LocalVariable temp_ = new LocalVariable();
                                            temp_.Name = $"{filename},line{x}";
                                            temp_.Value = w;
                                            expectList.items.Add(temp_);
                                            expectList.numberOfElements++;
                                            x++;
                                        }
                                        valid_command = true;
                                    } else
                                    {
                                        // Throw error bad type
                                        errorHandler.ThrowError(2100, null, null, expectedObjectName, "empty string list", expectedFormat);
                                        break;
                                    }
                                }
                                else
                                {
                                    // Throw error bad type
                                    errorHandler.ThrowError(2100, null, null, expectedObjectName, "empty string list", expectedFormat);
                                    break;
                                }
                            }
                            else
                            {
                                LocalList newlist = new LocalList();
                                newlist.arrayType = ArrayType.String;
                                newlist.Name = expectedObjectName;
                                int x = 0;
                                foreach (string w in filecontents)
                                {
                                    LocalVariable temp_ = new LocalVariable();
                                    temp_.Name = $"{filename},line{x}";
                                    temp_.Value = w;
                                    newlist.items.Add(temp_);
                                    newlist.numberOfElements++;
                                    x++;
                                }
                                namesInUse.Add(expectedObjectName, objectClass.List);
                                local_lists.Add(newlist);
                                valid_command = true;
                            }
                            break;
                        case "readall":
                            expectedReturnType = objectClass.Variable;
                            string[] filecontents_ = filesystem.commandDirectoryReturnString[_placeholder].Invoke(expectedPath, "");
                            LocalVariable expectString = local_variables.Find(str => str.Name == expectedObjectName);
                            if (expectString != null)
                            {
                                if (expectString.Type == VariableType.String)
                                {
                                    expectString.Value = "";
                                    foreach (string q in filecontents_)
                                    {
                                        expectString.Value += q + Environment.NewLine;
                                    }
                                    valid_command = true;
                                } else
                                {
                                    // Throw error bad type
                                    errorHandler.ThrowError(2100, null, null, expectedObjectName, "string variable", expectedFormat);
                                    break;
                                }
                            } else
                            {
                                // Throw error not found
                                errorHandler.ThrowError(1200, null, expectedObjectName, null, null, expectedFormat);
                                break;
                            }
                            break;
                        default:
                            // Throw error for bad parameter
                            break;
                    }
                }
            }
#endif

#if HASTASK          
                /// <summary>
                /// Tasks are a list of commands than can be executed as a background task (on a separate thread) or in-line with the main code.
                /// Tasks will run once in chronological order (unless a loop in the task keeps it alive)
                /// 
                /// SYNTAX EXAMPLES:
                /// new_task taskname 'inline'/'background' *integer      <- creates new task list, sets to inline/background, *integer is optional parameter to define the task's local script delay
                /// task_add taskname command(s) [...]                   <- appends new line of commands to task list
                /// task_remove taskname index                          <- removes task line at specified index
                /// task_insert taskname index command(s)[...]         <- interts new line of commands into index
                /// task_clearall taskname                            <- clears all tasks within task list
                /// task_printall                                     <- prints list of all task items
                /// task_setdelay name int:miliseconds               <- sets the local script delay of task
                /// task_execute taskname                           <- executes specified task
                /// </summary>
                if (split_input[0].Equals("new_task", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "new_task newname inline/background 100" + Environment.NewLine + "Final parameter should be integer and is optional.";
                string taskName_;
                TaskType taskType_;
                int scriptDelay_;
                bool error_raised = false;
                if (split_input.Length == 4)
                {
                    if (namesInUse.ContainsKey(split_input[1]))
                    {
                        error_raised = true;
                        errorHandler.ThrowError(1300, null, null, split_input[1], null, expectedFormat);
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
                                        namesInUse.Add(taskName_, objectClass.TaskList);
                                        valid_command = true;
                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(1400, $"script delay", null, split_input[3], "integer value", expectedFormat);
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
                                        namesInUse.Add(taskName_, objectClass.TaskList);
                                        valid_command = true;
                                    }
                                    else
                                    {
                                        errorHandler.ThrowError(1400, $"script delay", null, split_input[3], "integer value", expectedFormat);
                                    }
                                }

                            }
                            else
                            {
                                error_raised = true;
                                errorHandler.ThrowError(1400, $"task type", null, a_, "Inline/Background", expectedFormat);
                            }
                        }
                        else
                        {
                            error_raised = true;
                            errorHandler.ThrowError(1600, split_input[1]);
                        }
                    }
                }
                else if (split_input.Length == 3) // We are taking an optional final parameter (integer) for task's script delay
                {
                    if (namesInUse.ContainsKey(split_input[1]))
                    {
                        error_raised = true;
                        errorHandler.ThrowError(1300, null, null, split_input[1], null, expectedFormat);
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
                                        namesInUse.Add(taskName_, objectClass.TaskList);
                                        tasklists_inuse.Add(newTask);
                                    valid_command = true;
                                }
                                else if (a_ == "background")
                                {
                                    taskType_ = TaskType.BackgroundTask;
                                    TaskList newTask = new TaskList(taskName_, taskType_);
                                        namesInUse.Add(taskName_, objectClass.TaskList);
                                        tasklists_inuse.Add(newTask);
                                    valid_command = true;
                                }
                            }
                            else
                            {
                                error_raised = true;
                                errorHandler.ThrowError(1400, $"task type", null, a_, "Inline/Background", expectedFormat);
                            }
                        }
                        else
                        {
                            error_raised = true;
                            errorHandler.ThrowError(1600, split_input[1]);
                        }
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "new_task", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("task_add", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "task_add taskname command";
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
                        errorHandler.ThrowError(1200, null, taskname, null, null, expectedFormat);
                    } else
                    {
                        valid_command = true;
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "task_add", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("task_remove", StringComparison.OrdinalIgnoreCase))
            {

            }
                if (split_input[0].Equals("task_insert", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "task_insert taskname index command";
                string taskname = split_input[1];
                bool foundtasklist = false;
                string command = "";
                string expectedIndex = SetVariableValue(split_input[2]);
                if (split_input.Length > 3)
                {
                    foreach (string s in split_input.Skip(3))
                    {
                        command += s + " ";
                    }
                    command.Trim();
                } else if (split_input.Length < 4)
                {
                    errorHandler.ThrowError(1100, "task_insert", null, null, null, expectedFormat);
                    return;
                }
                else if (split_input.Length == 4)
                {
                    command = split_input[3];
                }
                bool validInteger = IsNumeric(expectedIndex); // must be valid number
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
                        errorHandler.ThrowError(1200, null, taskname, null, null, expectedFormat);
                    } else
                    {
                        valid_command = true;
                    }
                }
                else
                {
                    errorHandler.ThrowError(1400, "task_insert", null, expectedIndex, "a valid integer", expectedFormat);
                }
            }
                if (split_input[0].Equals("task_clearall", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "task_clearall taskname";
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
                        errorHandler.ThrowError(1200, null, taskname, null, null, expectedFormat);
                    } else
                    {
                        valid_command = true;
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "task_clearall", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("task_printall", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "task_printall taskname";
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
                        errorHandler.ThrowError(1200, null, taskname, null, null, expectedFormat);
                    } else
                    {
                        valid_command = true;
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "task_printall", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("task_setdelay", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "task_setdelay taskname 100";
                if (split_input.Length == 3)
                {
                    string taskname = split_input[1];
                    bool foundtasklist = false;
                    string expectedNumber = SetVariableValue(split_input[2]).TrimEnd();
                    bool validInteger = IsNumeric(expectedNumber);
                    if (validInteger == true)
                    {
                        int a_ = Int32.Parse(expectedNumber);
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
                            errorHandler.ThrowError(1200, null, taskname, null, null, expectedFormat);
                        } else
                        {
                            valid_command = true;
                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1400, $"script delay", null, split_input[2], "integer value", expectedFormat);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "task_setdelay", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("task_execute", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "task_execute taskname";
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
                        errorHandler.ThrowError(1200, null, taskname, null, null, expectedFormat);
                    }
                    else
                    {
                        // We proceed to execute the task
                        valid_command = true;
                        executeTask(commandsToPass, taskType_, scriptDelay_);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "task_execute", null, null, null, expectedFormat);
                }
            }
#endif
#if HASNETCODE
                ///<summary>
                /// TCP server and TCP client objects will enable network applications to communicate over the internet or locally.
                /// TCPClientProtocols and TCPServerProtocols execute specific to the event and will execute an associated task.
                /// There are no default task lists associated with protocols. When a client or server runs a protocol, the info
                /// is passed to string eventMessage_ which details the event and allows code to be executed with the passed info.
                /// 
                /// TCP SERVER SYNTAX:
                /// new_tcp_server servername                                       <- creates new server 
                /// tcp_server_start servername                                    <- starts server
                /// 
                /// TCP CLIENT SYNTAX:
                /// new_tcp_client clientname                                       <- creates new client
                /// tcp_client_connect clientname ipaddress                        <- attempts to connect client to ipaddress
                /// 
                /// DATA PACKET STUFF:
                /// new_datapacket ID:value| TCPObject:tcpclient/tcpserver Data:variable/list           <- creates new datapacket in specified TCP client/server with specified ID and data
                /// datapacket_send tcpclient/tcpserver packetID *all                                  <- send datapacket in outgoing list from TCP client/server with packet ID. Optionally specify 'All' if multiple IDs exists
                /// 
                /// </summary>
                // TCP Server
                if (split_input[0].Equals("new_tcp_server", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "new_tcp_server newname";
                if (split_input.Length >= 2)
                {
                    string expectedName = split_input[1].TrimEnd();
                    bool nameInUse = NameInUse(expectedName);
                    bool validCharacters = ContainsOnlyLettersAndNumbers(expectedName);
                    if (validCharacters == false)
                    {
                        errorHandler.ThrowError(1600, expectedName);
                        return;
                    }
                    if (nameInUse == false)
                    {
                        ServerSide newTCPServer = new ServerSide(this, expectedName);
                        activeTCPObjects.Add(newTCPServer);
                        activeServers.Add(newTCPServer);
                        namesInUse.Add(expectedName, objectClass.TCPNetObj);
                        valid_command = true;
                    }
                    else
                    {
                        errorHandler.ThrowError(1300, null, null, expectedName, null, expectedFormat);
                    }
                } else
                {
                    errorHandler.ThrowError(1100, "new_tcp_server", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("tcp_server_start", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "tcp_server_start servername";
                if (split_input.Length >= 2)
                {
                    IPAddress ipAddress;
                    string serverName = split_input[1].TrimEnd();
                    bool nameIsInUse = NameInUse(serverName);
                    bool isProperType = namesInUse[serverName] == objectClass.TCPNetObj;
                    bool clearToProceed = ((nameIsInUse == true) && (isProperType == true));
                    bool found_ = false;

                    try
                    {
                        if (clearToProceed == true)
                        {
                            foreach (ServerSide server_ in activeTCPObjects)
                            {
                                if (server_.serverName == serverName)
                                {
                                    found_ = true;
                                    server_.Start(); //.GetAwaiter().GetResult();
                                    valid_command = true;
                                    break;
                                }
                            }
                            if (found_ == false) { errorHandler.ThrowError(1200, null, serverName, null, null, expectedFormat); }
                        }
                        else if (nameIsInUse == false)
                        {
                            errorHandler.ThrowError(1200, null, serverName, null, null, expectedFormat);
                        }
                        else if (isProperType == false)
                        {
                            errorHandler.ThrowError(2100, null, null, serverName, "TCP server", expectedFormat);
                        }
                    }
                    catch
                    {

                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "tcp_server_start", null, null, null, expectedFormat);
                }
            }
                // TCP Client
                if (split_input[0].Equals("new_tcp_client", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "new_tcp_client newname";
                if (split_input.Length >= 2)
                {
                    string expectedName = split_input[1].TrimEnd();
                    bool nameInUse = NameInUse(expectedName);
                    bool validCharacters = ContainsOnlyLettersAndNumbers(expectedName);
                    if (validCharacters == false)
                    {
                        errorHandler.ThrowError(1600, expectedName);
                        return;
                    }
                    if (nameInUse == false)
                    {
                        ClientSide newTCPClient = new ClientSide(this, expectedName);
                        activeTCPObjects.Add(newTCPClient);
                        activeClients.Add(newTCPClient);
                        namesInUse.Add(expectedName, objectClass.TCPNetObj);
                        valid_command = true;
                    }
                    else
                    {
                        errorHandler.ThrowError(1300, null, null, expectedName, null, expectedFormat);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "new_tcp_client", null, null, null, expectedFormat);
                }
            }
                if (split_input[0].Equals("tcp_client_connect", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "tcp_client_connect tcpclient address";
                if (split_input.Length >= 3)
                {
                    IPAddress ipAddress;
                    string clientName = split_input[1].TrimEnd();
                    bool nameIsInUse = NameInUse(clientName);
                    bool isProperType = namesInUse[clientName] == objectClass.TCPNetObj;
                    bool clearToProceed = ((nameIsInUse == true) && (isProperType == true));
                    string expectedIP = split_input[2];
                    bool found_ = false;

                    if (IPAddress.TryParse(expectedIP, out ipAddress))
                    {
                        try
                        {
                            if (clearToProceed == true)
                            {
                                foreach (ClientSide client_ in activeTCPObjects)
                                {
                                    if (client_.name == clientName)
                                    {
                                        if (client_.hasStarted == false) { client_.Start(expectedIP); found_ = true; valid_command = true; } else { Console.WriteLine($"{clientName} already has connection."); }
                                        break;
                                    }
                                }
                                if (found_ == false) { errorHandler.ThrowError(1200, null, clientName, null, null, expectedFormat); }
                            } else if (nameIsInUse == false)
                            {
                                errorHandler.ThrowError(1200, null, clientName, null, null, expectedFormat);
                            } else if (isProperType == false)
                            {
                                errorHandler.ThrowError(2100, null, null, clientName, "TCP client", expectedFormat);
                            }
                        } catch
                        {

                        }
                    }
                    else
                    {
                        errorHandler.ThrowError(1400, "IP address", null, expectedIP, "IP address", expectedFormat);
                    }
                }
                else
                {
                    errorHandler.ThrowError(1100, "tcp_client_connect", null, null, null, expectedFormat);
                }
            }
                // TCP Client || TCP Server
                if ((split_input[0].Equals("tcp_client_assignprotocol", StringComparison.OrdinalIgnoreCase) || (split_input[0].Equals("tcp_server_assignprotocol", StringComparison.OrdinalIgnoreCase))))
            {
                entry_made = true;
                // tcp_client_assignprotocol tcpobj protocol task
                // tcp_server_assignprotocol tcpobj protocol task
                string expectedFormat = "tcp_client_assignprotocol clientname protocol taskname";
                string entryCommand = split_input[0];
                int serverORclient = -1; // 0 for client 1 for server
                if (split_input[0].Equals("tcp_client_assignprotocol", StringComparison.OrdinalIgnoreCase))
                {
                    serverORclient = 0;
                }
                else if (split_input[0].Equals("tcp_server_assignprotocol", StringComparison.OrdinalIgnoreCase))
                {
                    expectedFormat = "tcp_server_assignprotocol servername protocol taskname";
                    serverORclient = 1;
                }

                if (split_input.Length == 4)
                {
                    string[] serverProtocols = { "ServerStarted", "ReceivedDataPacket", "ClientConnected", "ClientDisconnected", "ClientRejected", "DataPacketAdded", "BroadcastDataPacket", "BroadcastAllOutDataPackets", "AttemptedConnection" };
                    string[] clientProtocols = { "ClientStarted", "ClientDisconnect", "ReceivedDataPacket", "BroadcastDataPacket", "BroadcastAllOutDataPackets", "" };
                    TCPClientProtocols clientProtocol = new TCPClientProtocols();
                    TCPServerProtocols serverProtocol = new TCPServerProtocols();

                    string expectedTCPobject = SetVariableValue(split_input[1].TrimEnd());
                    string expectedTCPprotocol = SetVariableValue(split_input[2].TrimEnd());
                    string expectedTask = SetVariableValue(split_input[3].TrimEnd());

                    bool validTask = false;
                    TaskList taskToAssign = tasklists_inuse.Find(taskx => taskx.taskName == expectedTask);
                    if (taskToAssign != null)
                    {
                        validTask = true;
                    } else
                    {
                        errorHandler.ThrowError(1200, null, expectedTask, null, null, expectedFormat);
                        return;
                    }

                    bool objectExists = NameInUse(expectedTCPobject);
                    if (objectExists == true)
                    {
                        bool validTCPobject = namesInUse[expectedTCPobject] == objectClass.TCPNetObj;
                        if (validTCPobject == true)
                        {
                            int x = 0;
                            bool matchFound = false;
                            if (serverORclient == 0)
                            {
                                for (x = 0; x < clientProtocols.Length; x++)
                                {
                                    if (expectedTCPprotocol.Equals(clientProtocols[x], StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchFound = true;
                                        switch (x)
                                        {
                                            case 0:
                                                clientProtocol = TCPClientProtocols.protocols_clientStarted;
                                                break;
                                            case 1:
                                                clientProtocol = TCPClientProtocols.protocols_clientDisconnect;
                                                break;
                                            case 2:
                                                clientProtocol = TCPClientProtocols.protocols_receiveDataPacket;
                                                break;
                                            case 3:
                                                clientProtocol = TCPClientProtocols.protocols_addDataPacket;
                                                break;
                                            case 4:
                                                clientProtocol = TCPClientProtocols.protocols_broadcastDatapacket;
                                                break;
                                            case 5:
                                                clientProtocol = TCPClientProtocols.protocols_broadcastAllOutgoing;
                                                break;
                                        }
                                        break;
                                    }

                                }
                            } else if (serverORclient == 1)
                            {
                                for (x = 0; x < serverProtocols.Length; x++)
                                {
                                    if (expectedTCPprotocol.Equals(serverProtocols[x], StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchFound = true;
                                        switch (x)
                                        {
                                            case 0:
                                                serverProtocol = TCPServerProtocols.protocols_serverStarted;
                                                break;
                                            case 1:
                                                serverProtocol = TCPServerProtocols.protocols_receiveDataPacket;
                                                break;
                                            case 2:
                                                serverProtocol = TCPServerProtocols.protocols_clientConnected;
                                                break;
                                            case 3:
                                                serverProtocol = TCPServerProtocols.protocols_clientDisconnected;
                                                break;
                                            case 4:
                                                serverProtocol = TCPServerProtocols.protocols_clientRejected;
                                                break;
                                            case 5:
                                                serverProtocol = TCPServerProtocols.protocols_addDataPacket;
                                                break;
                                            case 6:
                                                serverProtocol = TCPServerProtocols.protocols_broadcastDatapacket;
                                                break;
                                            case 7:
                                                serverProtocol = TCPServerProtocols.protocols_broadcastAllOutgoing;
                                                break;
                                            case 8:
                                                serverProtocol = TCPServerProtocols.protocols_attemptedConnection;
                                                break;
                                        }
                                        break;
                                    }

                                }
                            }
                            if (matchFound == true)
                            {
                                if (validTask == true)
                                {
                                    if (serverORclient == 0)
                                    {
                                        foreach (ClientSide client_ in activeClients)
                                        {
                                            if (client_.name == expectedTCPobject)
                                            {
                                                client_.AssignProtocol(clientProtocol, taskToAssign);
                                                valid_command = true;
                                                break;
                                            }
                                        }
                                    } else if (serverORclient == 1)
                                    {
                                        foreach (ServerSide server_ in activeServers)
                                        {
                                            if (server_.serverName == expectedTCPobject)
                                            {
                                                server_.AssignProtocol(serverProtocol, taskToAssign);
                                                valid_command = true;
                                                break;
                                            }
                                        }
                                    }


                                } else
                                {
                                    errorHandler.ThrowError(1200, null, expectedTask, null, null, expectedFormat);
                                    return; // logical redundancy
                                }
                            } else
                            {
                                string validValues = "";
                                if (serverORclient == 0)
                                {
                                    foreach (string s in clientProtocols)
                                    {
                                        validValues += s + " ";
                                    }
                                    errorHandler.ThrowError(1400, "client protocol", null, expectedTCPprotocol, validValues, expectedFormat);
                                }
                            }

                        } else
                        {
                            errorHandler.ThrowError(2100, null, null, expectedTCPobject, "TCP server/TCP client", expectedFormat);
                        }
                    } else
                    {
                        errorHandler.ThrowError(1200, null, expectedTCPobject, null, null, expectedFormat);
                    }


                } else
                {
                    errorHandler.ThrowError(1100, entryCommand, null, null, null, expectedFormat);
                }
            }
                if (
                ((split_input[0].StartsWith("tcp_client_", StringComparison.OrdinalIgnoreCase)) || (split_input[0].StartsWith("tcp_server_"))) &&
                ((split_input[0].EndsWith("_whitelist", StringComparison.OrdinalIgnoreCase)) || (split_input[0].EndsWith("blacklist")))
                )
            {
                entry_made = true;
                string designation = "tcp_client_whitelist", eaction = "";
                string expectedFormat = "tcp_client_whitelist clientname add/remove 127.0.0.1|127.0.0.2|etc", badVals = "";
                bool validTCPObject = false, foundObject = false, foundBadVal = false, minimum1goodval = false, hadAddOrRemove = false;
                int serverOrClient = -1, whiteOrblack = -1, addOrRemove = -1, validCount = 0, goodValsAdded = 0; // 0 for client, 1 for server ||| 0 for white, 1 for black ||| 0 for add, 1 for remove

                // Did user specify a server or client
                string srv_clnt = split_input[0].ToLower();
                switch (srv_clnt)
                {
                    case { } when srv_clnt.StartsWith("tcp_client_", StringComparison.OrdinalIgnoreCase) && srv_clnt.EndsWith("_whitelist", StringComparison.OrdinalIgnoreCase):
                        serverOrClient = 0;
                        whiteOrblack = 0;
                        expectedFormat = "tcp_client_whitelist clientname add/remove 127.0.0.1|127.0.0.2|etc";
                        designation = "tcp_client_whitelist";
                        break;
                    case { } when srv_clnt.StartsWith("tcp_client_", StringComparison.OrdinalIgnoreCase) && srv_clnt.EndsWith("_blacklist", StringComparison.OrdinalIgnoreCase):
                        serverOrClient = 0;
                        whiteOrblack = 1;
                        expectedFormat = "tcp_client_blacklist clientname add/remove 127.0.0.1|127.0.0.2|etc";
                        designation = "tcp_client_blacklist";
                        break;
                    case { } when srv_clnt.StartsWith("tcp_server_", StringComparison.OrdinalIgnoreCase) && srv_clnt.EndsWith("_whitelist", StringComparison.OrdinalIgnoreCase):
                        serverOrClient = 1;
                        whiteOrblack = 0;
                        expectedFormat = "tcp_server_whitelist servername add/remove 127.0.0.1|127.0.0.2|etc";
                        designation = "tcp_server_whitelist";
                        break;
                    case { } when srv_clnt.StartsWith("tcp_server_", StringComparison.OrdinalIgnoreCase) && srv_clnt.EndsWith("_blacklist", StringComparison.OrdinalIgnoreCase):
                        serverOrClient = 1;
                        whiteOrblack = 1;
                        expectedFormat = "tcp_server_blacklist servername add/remove 127.0.0.1|127.0.0.2|etc";
                        designation = "tcp_server_blacklist";
                        break;
                    default:
                        // Throw 1100 format error kinda redundant
                        errorHandler.ThrowError(1100, "tcp_client_whitelist/blacklist or tcp_server_whitelist/blacklist", null, null, null, expectedFormat);
                        break;
                }

                // Minimum format check
                if (split_input.Length < 4)
                {
                    // Throw 1100 format error
                    errorHandler.ThrowError(1100, "tcp_client_whitelist/blacklist or tcp_server_whitelist/blacklist", null, null, null, expectedFormat);
                    return;
                }

                // Does the client/server exist
                string expectedTCPName = split_input[1];
                foundObject = NameInUse(expectedTCPName);
                if (foundObject == true) { validTCPObject = (namesInUse[expectedTCPName] == objectClass.TCPNetObj); }

                // Did user define add/remove
                string expectedAction = split_input[2].ToLower();
                switch (expectedAction)
                {
                    case "add":
                        hadAddOrRemove = true;
                        addOrRemove = 0;
                        eaction += "added";
                        break;
                    case "remove":
                        hadAddOrRemove = true;
                        addOrRemove = 1;
                        eaction += "removed";
                        break;
                    default:
                        // Throw 1400 bad value error
                        errorHandler.ThrowError(1400, designation, null, expectedAction, "add/remove", expectedFormat);
                        return;
                        break;
                }

                if (foundObject == true) { validCount++; } else { errorHandler.ThrowError(1200, null, expectedTCPName, null, null, expectedFormat); return; }
                if (validTCPObject == true) { validCount++; } else { errorHandler.ThrowError(2100, null, null, GetDescription(namesInUse[expectedTCPName]), "TCP object", expectedFormat); return; }
                if (hadAddOrRemove == true) { validCount++; } else { errorHandler.ThrowError(1400, designation, null, expectedAction, "add/remove", expectedFormat); return; } // Redundant error check

                if (validCount == 3)
                {
                    IPAddress ipAddress;
                    string[] expectedIPAddresses = split_input[3].Split('|');
                    IPStatus ipDesignation = default;
                    if (whiteOrblack == 0) { ipDesignation = IPStatus.Whitelist; } else if (whiteOrblack == 1) { ipDesignation = IPStatus.Blacklist; }
                    foreach (TCPNetSettings tcpobj in activeTCPObjects)
                    {
                        if (tcpobj.ParentName == expectedTCPName)
                        {
                            foreach (string ipAddressString in expectedIPAddresses)
                            {
                                if (IPAddress.TryParse(ipAddressString, out ipAddress))
                                {
                                    if (addOrRemove == 1)
                                    {
                                        if (tcpobj.IPAddressBook.ContainsKey(ipAddress))
                                        {
                                            tcpobj.IPAddressBook.Remove(ipAddress);
                                            minimum1goodval = true;
                                            valid_command = true;
                                            goodValsAdded++;
                                        }

                                    } else if (addOrRemove == 0)
                                    {
                                        tcpobj.IPAddressBook.Add(ipAddress, ipDesignation);
                                        minimum1goodval = true;
                                        valid_command = true;
                                        goodValsAdded++;
                                    }
                                }
                                else
                                {
                                    foundBadVal = true;
                                    badVals += ipAddressString + " ";
                                }
                            }
                        }
                    }

                    if (minimum1goodval == true)
                    {
                        badVals += $"- {goodValsAdded} items were successfully {eaction}.";
                        if (foundBadVal == true) {
                            errorHandler.ThrowError(1400, designation, null, badVals, "valid IP address", expectedFormat);
                        }
                    } else
                    {
                        if (foundBadVal == true) {
                            errorHandler.ThrowError(1400, designation, null, badVals, "valid IP address", expectedFormat);
                        } else
                        {
                            errorHandler.ThrowError(1700, null, "valid IP address", null, null, expectedFormat);

                        }
                    }

                }



            }
                // Data packet stuff
                if (split_input[0].Equals("new_datapacket", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "new_datapacket TCPObject:clientname/servername ID:value| Data:object" + Environment.NewLine + "ID must end with vertical pipe | and Data must refer to object by name.";
                bool hasDestination = false;
                bool hasData = false;
                bool hasID = false;

                List<string> receivingTCPobjects = new List<string>();
                objectClass outgoingObjType = default;
                string objName = "";
                string dpID = "";

                bool extracting = false;

                if (split_input.Length < 4)
                {
                    errorHandler.ThrowError(1100, "new_datapacket", null, null, null, expectedFormat);
                    return;
                }

                foreach (string s in split_input)
                {
                    if (extracting == true)
                    {
                        string q = SetVariableValue(s);
                        foreach (char c in q)
                        {
                            if (c != '|')
                            {
                                dpID += c;
                            }
                            else
                            {
                                hasID = true;
                                extracting = false;
                            }
                        }
                        if (extracting == true)
                        {
                            dpID += " ";
                        }
                    }
                    else
                    {
                        if (s.StartsWith("TCPObject:", StringComparison.OrdinalIgnoreCase))
                        {
                            string _placeholder = s.Remove(0, 10);
                            string a = ConvertNumericalVariable(_placeholder);
                            string[] b = a.Split(',');
                            if (b.Length > 0)
                            {
                                string invalidNames = "";
                                int validFinds = 0;
                                foreach (string c in b)
                                {
                                    bool goodfind = false;
                                    foreach (TCPNetSettings tcpobj_ in activeTCPObjects)
                                    {
                                        if (tcpobj_.ParentName == c)
                                        {
                                            validFinds++;
                                            goodfind = true;
                                            receivingTCPobjects.Add(c);
                                        }
                                    }
                                    if (goodfind == false)
                                    {
                                        invalidNames += c + " ";
                                    }
                                }
                                if (validFinds > 0)
                                {
                                    // proceed
                                    hasDestination = true;

                                }
                                else
                                {
                                    Console.WriteLine($"Invalid TCP object(s): {invalidNames}");
                                    break;
                                }


                            }
                            else
                            {
                                Console.WriteLine("Expecting minimum 1 TCP client or server.");
                                break;
                            }
                        }
                        if (s.StartsWith("Data:", StringComparison.OrdinalIgnoreCase))
                        {
                            string _placeholder = s.Remove(0, 5);
                            string a = ConvertNumericalVariable(_placeholder);
                            bool validObject = NameInUse(a);
                            bool validObjType = false;
                            if (validObject == true)
                            {
                                switch (namesInUse[a])
                                {
                                    case objectClass.Variable:
                                        validObjType = true;
                                        outgoingObjType = namesInUse[a];
                                        objName = a;
                                        break;
                                    case objectClass.EnvironmentalVariable:
                                        validObjType = false; // redundant
                                        break;
                                    case objectClass.List:
                                        validObjType = true;
                                        outgoingObjType = namesInUse[a];
                                        objName = a;
                                        break;
                                    case objectClass.TCPNetObj:
                                        validObjType = false; // redundant
                                        break;
                                    case objectClass.DataPacket:
                                        validObjType = false; // redundant
                                        break;
                                }


                                if (validObjType == true)
                                {
                                    // proceed
                                    hasData = true;
                                }
                                else if (validObjType == false)
                                {
                                    Console.WriteLine($"Object {a} is invalid type. Expecting object type of variable or list.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Object does not exist: {a}. Expecting object type of variable or list.");
                                break;
                            }

                        }
                        if (s.StartsWith("ID:", StringComparison.OrdinalIgnoreCase))
                        {
                            string _placeholder = s.Remove(0, 3);
                            string b = SetVariableValue(_placeholder);

                            extracting = true;
                            bool pipefound = false;
                            foreach (char c in b)
                            {
                                if (c != '|')
                                {
                                    dpID += c;

                                }
                                else
                                {
                                    hasID = true;
                                    pipefound = true;
                                    break;
                                }
                            }
                            if (pipefound == true)
                            {
                                extracting = false;
                            }
                            else
                            {
                                dpID += " ";
                            }
                        }
                    }
                }

                if (extracting == true)
                {
                    errorHandler.ThrowError(2300, null, null, null, null, expectedFormat);
                }

                int canProceed = 0;
                if (hasDestination == true) { canProceed++; }
                if (hasData == true) { canProceed++; }
                if (hasID == true) { canProceed++; }

                if (canProceed == 3)
                {
                    dataPacket outgoingDP = new dataPacket();
                    switch (outgoingObjType)
                    {
                        case objectClass.Variable:
                            LocalVariable temp_ = local_variables.Find(x => x.Name == objName);
                            if (temp_ != null)
                            {
                                switch (temp_.Type)
                                {
                                    case VariableType.String:
                                        outgoingDP.objType = NetObjType.ObjString;
                                        break;
                                    case VariableType.Int:
                                        outgoingDP.objType = NetObjType.ObjInt;
                                        break;
                                    case VariableType.Float:
                                        outgoingDP.objType = NetObjType.ObjFloat;
                                        break;
                                    case VariableType.Boolean:
                                        outgoingDP.objType = NetObjType.ObjBool;
                                        break;
                                }
                                outgoingDP.objData = temp_.Value;
                            } else
                            {
                                errorHandler.ThrowError(1200, null, objName, null, null, expectedFormat);
                                break;
                            }
                            break;
                        case objectClass.List:
                            LocalList temp2_ = local_lists.Find(y => y.Name == objName);
                            if (temp2_ != null)
                            {
                                outgoingDP.objType = NetObjType.ObjList;
                                int xx = temp2_.items.Count;
                                foreach (LocalVariable var in temp2_.items)
                                {
                                    outgoingDP.objData += var.Name + ":" + var.Value;
                                    xx--;
                                    if (xx > temp2_.items.Count)
                                    {
                                        outgoingDP.objData += ",";
                                    }
                                }
                            }
                            else
                            {
                                errorHandler.ThrowError(1200, null, "list " + objName, null, null, expectedFormat);
                                break;
                            }
                            break;
                    }

                    outgoingDP.ID = dpID.TrimEnd();
                    outgoingDP.senderAddress = "127.0.0.1";

                    foreach (string receiverName in receivingTCPobjects)
                    {
                        foreach (TCPNetSettings targetObject in activeTCPObjects)
                        {
                            if (targetObject.ParentName == receiverName)
                            {
                                targetObject.outgoingDataPackets.Add(outgoingDP);
                            }
                        }
                        valid_command = true;
                    }
                }
            }
                if (split_input[0].Equals("datapacket_send", StringComparison.OrdinalIgnoreCase))
            {
                entry_made = true;
                string expectedFormat = "datapacket_send clientname/servername packetid *all" + Environment.NewLine + "Final parameter all is optional and will send all datapackets with specified ID. If not included, first datapacket found with ID is sent.";
                if ((split_input.Length >= 3) && (split_input.Length < 5))
                {
                    bool sendingAll = false;
                    bool goodFind = false;
                    if (split_input.Length == 4)
                    {
                        if (split_input[3].Equals("all", StringComparison.OrdinalIgnoreCase))
                        {
                            sendingAll = true;
                        } else {
                            errorHandler.ThrowError(1400, $"final parameter", null, split_input[3], "either 'all' as optional parameter or no value expected", expectedFormat);
                            return;
                        }
                    }
                    string expectedTCPobject = SetVariableValue(split_input[1]).TrimEnd();
                    string expectedDPID = SetVariableValue(split_input[2]);
                    int tcpobject_type = -1; // 0 for client, 1 for server

                    foreach (TCPNetSettings tcpobj_ in activeTCPObjects)
                    {
                        if (tcpobj_.ParentName == expectedTCPobject)
                        {
                            goodFind = true;
                            tcpobject_type = tcpobj_.tcpobj_type;
                            break;
                        }
                    }
                    if (goodFind == true)
                    {
                        switch (tcpobject_type)
                        {
                            case 0:
                                foreach (ClientSide TCPClient in activeClients)
                                {
                                    if (TCPClient.ParentName == expectedTCPobject)
                                    {
                                        if (sendingAll == true)
                                        {
                                            List<dataPacket> dpsToSend = TCPClient.outgoingDataPackets.FindAll(y => y.ID == expectedDPID);
                                            if (dpsToSend.Count > 0)
                                            {
                                                foreach (dataPacket dp in dpsToSend)
                                                {
                                                    dp.senderAddress = TCPClient.thisClientIP;
                                                    TCPClient.SendDatapacket(dp);
                                                    dpsToSend.Remove(dp);
                                                }
                                                valid_command = true;
                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1200, null, $"datapacket with ID " + expectedDPID, null, null, expectedFormat);
                                                return;
                                            }
                                        } else
                                        {
                                            dataPacket dpToSend = TCPClient.outgoingDataPackets.Find(x => x.ID == expectedDPID);
                                            if (dpToSend != null)
                                            {
                                                TCPClient.SendDatapacket(dpToSend);
                                                TCPClient.outgoingDataPackets.Remove(dpToSend);
                                                valid_command = true;
                                            } else
                                            {
                                                errorHandler.ThrowError(1200, null, $"datapacket with ID " + expectedDPID, null, null, expectedFormat);
                                                return;
                                            }
                                        }
                                    }
                                }
                                break;
                            case 1:
                                foreach (ServerSide TCPServer in activeServers)
                                {
                                    if (TCPServer.ParentName == expectedTCPobject)
                                    {
                                        if (sendingAll == true)
                                        {
                                            List<dataPacket> dpsToSend = TCPServer.outgoingDataPackets.FindAll(y => y.ID == expectedDPID);
                                            if (dpsToSend.Count > 0)
                                            {
                                                foreach (dataPacket dp in dpsToSend)
                                                {
                                                    dp.senderAddress = TCPServer.serverName;
                                                    TCPServer.BroadcastPacket(dp, null);
                                                    dpsToSend.Remove(dp);
                                                }
                                                valid_command = true;
                                            } else
                                            {
                                                errorHandler.ThrowError(1200, null, $"datapacket with ID " + expectedDPID, null, null, expectedFormat);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            dataPacket dpToSend = TCPServer.outgoingDataPackets.Find(x => x.ID == expectedDPID);
                                            if (dpToSend != null)
                                            {
                                                TCPServer.BroadcastPacket(dpToSend, null);
                                                TCPServer.outgoingDataPackets.Remove(dpToSend);
                                                valid_command = true;
                                            }
                                            else
                                            {
                                                errorHandler.ThrowError(1200, null, $"datapacket with ID " + expectedDPID, null, null, expectedFormat);
                                                return;
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    } else
                    {
                        errorHandler.ThrowError(1200, null, expectedTCPobject, null, null, expectedFormat);
                    }
                } else
                {
                    errorHandler.ThrowError(1100, "datapacket_send", null, null, null, expectedFormat);
                }

            }
#endif
#if is_PARSER
                if (split_input[0].Equals("SETUP"))
            {
                string expectedSyntax = "SETUP";
                entry_made = true;
                if (split_input.Length > 1)
                {
                    errorHandler.ThrowError(1100, "GyroPrompt setup", null, null, null, expectedSyntax);
                    return;
                }
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
                        valid_command = true;
                    }
                }
            }
#endif
                if ((valid_command == false) && (entry_made == false))
            {
                if (input != "")
                {
                    errorHandler.ThrowError(1000, null, null, input, null, null);
                }
            }

        } catch (Exception error){ Console.WriteLine($"Fatal error encountered."); }
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
            max_lines = Lines.Count();
            while(current_line < max_lines)
            {
                parse(Lines[current_line]);
                current_line++;
                Thread.Sleep(ScriptDelay);
            }

            // Revert to pre-script settings
            //local_variables.Clear();
            //environmental_variables.Clear();
            //local_variables = local_variables_backup;
            //environmental_variables = environmental_variables_backup;
            //setConsoleStatus(info);

            running_script = false; // Tell parser we are not actively running a script
            current_line = 0; // Redundant reset
            max_lines = 0;
        }

        /// <summary>
        /// taskInfo is a struct used to pass List<string> and integer scriptdelay into new thread as object (if task is background and not in-line)
        /// If TaskType is inline, then we'll just process it in the executeTask method
        /// </summary>
 #if HASTASK

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
#endif
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
                if (capturing == false) 
                {
                    a = a + currentChar; 
                }
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
                // Check if object is in range of
                if (capturedText.StartsWith("InRange:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 8);
                    bool validVar = false, validInt = false, validMin = false, validMax = false, clearToProcees = false;
                    string lowestval = "", highestval = "", expectedVariable = "", compstr = ""; int minval = -1, maxval = -1, compval = -1, validChecks = 0;
                    if (!_placeholder.Contains(","))
                    {
                        Console.WriteLine("Expecting integer name, lowest value, and highest value separated by comma.");
                        return;
                    }
                    string[] placeholderSplit = _placeholder.Split(',');
                    if (placeholderSplit.Length != 3)
                    {
                        Console.WriteLine("Cannot take more or less than 3 comma-separated value.");
                        return;
                    } else
                    {
                        expectedVariable = SetVariableValue(placeholderSplit[0]);
                        validVar = LocalVariableExists(expectedVariable);
                        if (validVar == true)
                        {
                            LocalVariable expectedNumericalVar = local_variables.Find(o => o.Name == expectedVariable);
                            if (IsNumeric(ConvertNumericalVariable(expectedNumericalVar.Value.TrimEnd()))) { compstr = ConvertNumericalVariable(expectedNumericalVar.Value.TrimEnd()); validInt = true; } else { Console.WriteLine($"Not a valid number: {ConvertNumericalVariable(placeholderSplit[1])}"); return; }
                        } else
                        {
                            Console.WriteLine($"Could not find or locate {expectedVariable}"); return;
                        }
                        lowestval = ConvertNumericalVariable(placeholderSplit[1]);
                        highestval = ConvertNumericalVariable(placeholderSplit[2]);
                        validMin = IsNumeric(lowestval);
                        validMax = IsNumeric(highestval);
                        
                        if (validInt == true) { compval = Int32.Parse(compstr); validChecks++; } else { Console.WriteLine($"Not a valid number: {compval}."); return; }
                        if (validMin == true) { minval = Int32.Parse(lowestval); validChecks++; } else { Console.WriteLine($"Not a valid number: {lowestval}. If you are referencing a variable within [ ] brackets, make sure to use {{ }} brackets."); return; }
                        if (validMax == true) { maxval = Int32.Parse(highestval); validChecks++; } else { Console.WriteLine($"Not a valid number: {highestval}. If you are referencing a variable within [ ] brackets, make sure to use {{ }} brackets."); return; }
                        if (minval >= maxval) { Console.WriteLine($"Expecting lower value then higher value. {minval} is not less than {maxval}"); return; }
                        if ((minval < maxval) && (minval != maxval)) { validChecks++; }

                        if (validChecks == 4)
                        {
                            if ((compval < maxval) && (compval > minval))
                            {
                                a += "True";
                            } else
                            {
                                a += "False";
                            }
                        }
                    }

                }
                // Check is object contains
                if (capturedText.StartsWith("Contains:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 9);
                    bool validVar = false, caseSensitive = false;
                    string expectedVariable = "", comparingValue = "";
                    if ((_placeholder.StartsWith("CS:", StringComparison.OrdinalIgnoreCase))||(_placeholder.StartsWith("NCS:", StringComparison.OrdinalIgnoreCase)))
                    {
                        string placeholder2_ = "";
                        if (_placeholder.StartsWith("CS:", StringComparison.OrdinalIgnoreCase))
                        {
                            caseSensitive = true;
                            placeholder2_ += _placeholder.Remove(0, 3);
                        } else if (_placeholder.StartsWith("NCS:", StringComparison.OrdinalIgnoreCase))
                        {
                            caseSensitive = false;
                            placeholder2_ += _placeholder.Remove(0, 4);
                        }

                        if (placeholder2_.Contains(','))
                        {
                            string[] placeholderSplit = placeholder2_.Split(',');
                            if (placeholderSplit.Length == 2)
                            {
                                expectedVariable += placeholderSplit[0];
                                comparingValue += SetVariableValue(placeholderSplit[1]);
                                validVar = LocalVariableExists(expectedVariable);
                            } else
                            {
                                Console.WriteLine("Cannot take more or less than 2 comma-separated value."); return;
                            }
                        } else
                        {
                            Console.WriteLine("Expecting variable name and comparing value separated by comma."); return;
                        }

                        if (validVar == true)
                        {
                            LocalVariable tempvariable_ = local_variables.Find(i => i.Name ==  expectedVariable);
                            if (tempvariable_ != null)
                            {
                                string temp__ = tempvariable_.Value;
                                if (caseSensitive == true)
                                {
                                    if (temp__.Contains(comparingValue, StringComparison.Ordinal)) { a += "True"; } else { a += "False"; }
                                } else
                                {
                                    if (temp__.Contains(comparingValue, StringComparison.OrdinalIgnoreCase)) { a += "True"; } else { a += "False"; }
                                }
                            } else
                            {
                                // Redundancy never hurt anyone
                                Console.WriteLine($"Could not find or locate {expectedVariable}"); return;
                            }
                        } else
                        {
                            Console.WriteLine($"Could not find or locate {expectedVariable}"); return;
                        }

                    } else
                    {
                        Console.WriteLine("Contains: must be followed with CS: for case-sensitive or NCS: for not case-sensitive."); return;
                    }
                }
                // Then check for any equations to calculate
                if (capturedText.StartsWith("Calculate:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 10);
                    string b_ = ConvertNumericalVariable(_placeholder);
                    string b = calculate.calculate_string(b_).TrimEnd();
                    a += b;
                }
                // Then check for a randomizer
                if (capturedText.StartsWith("RandomizeInt:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 13);
                    if (_placeholder.Contains(','))
                    {
                        string run_vars = ConvertNumericalVariable(_placeholder);
                        string[] value_ = run_vars.Split(',');
                        string c = ConvertNumericalVariable(value_[0]);
                        string b = ConvertNumericalVariable(value_[1].TrimEnd());
                        if (value_.Length == 2)
                        {
                            bool first_valid = IsNumeric(c);
                            bool second_valid = IsNumeric(b);
                            if (first_valid && second_valid)
                            {
                                int a_ = Int32.Parse(c);
                                int b_ = Int32.Parse(b);
                                if (a_ < b_)
                                {
                                    string random_int = randomizer.randomizeInt(c, b).TrimEnd();
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

#if HASARRAY
                // Then check for array
                if (capturedText.StartsWith("Array:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] nonIntValues = { "Length" };
                    string _placeholder = capturedText.Remove(0, 6), expectedArrayName = "", requestedIndex = "";
                    bool validName = false, validType = false, validIndex = false, nonIntegerValue = false;
                    Array_Type arraytype_ = Array_Type.None;
                    if (!_placeholder.Contains(","))
                    {
                        Console.WriteLine("Expecting array name and index position separated by comma.");
                        return;
                    }
                    else
                    {
                        string[] placeholderSplit = _placeholder.Split(",");
                        if (placeholderSplit.Length != 2)
                        {
                            Console.WriteLine("Cannot take more than 1 comma-separated value.");
                            return;
                        }
                        else
                        {
                            expectedArrayName = placeholderSplit[0];
                            requestedIndex = ConvertNumericalVariable(placeholderSplit[1]).TrimEnd();
                            validIndex = IsNumeric(requestedIndex);
                            if (namesInUse.ContainsKey(expectedArrayName)) { validName = true; } else { Console.WriteLine($"Could not find or locate {expectedArrayName}"); return; }
                            if (namesInUse[expectedArrayName] == objectClass.VarArray) { validType = true; } else { Console.WriteLine($"Wrong type of variable or object: {expectedArrayName}, expecting: array of type bool, int, string or float"); return; }
                        }

                        foreach (string s in nonIntValues)
                        {
                            if (requestedIndex.Equals(s, StringComparison.OrdinalIgnoreCase))
                            {
                                nonIntegerValue = true;
                                break;
                            }
                        }
                        if ((validIndex == false) && (nonIntegerValue == false))
                        {
                            Console.WriteLine($"Not a valid integer: {requestedIndex}. If you are referencing a variable within [ ] brackets, make sure to use {{ }} brackets");
                            return;
                        }

                        if ((nonIntegerValue == false) && (validIndex == true))
                        {
                            int indx = Int32.Parse(requestedIndex);

                            foreach (array_baseitem array_ in local_arrays)
                            {
                                if (array_.Name == expectedArrayName)
                                {
                                    if (indx > array_.numberOfElements)
                                    {
                                        Console.WriteLine($"Array {expectedArrayName} has {array_.numberOfElements.ToString()} elements, requested index {requestedIndex} is invalid.");
                                        return;
                                    }
                                    else
                                    {
                                        a += (array_.getElementAt(indx));
                                    }
                                }
                            }

                        }
                        else if ((nonIntegerValue == true) && (validIndex == false))
                        {
                            foreach (array_baseitem array_ in local_arrays)
                            {
                                if (array_.Name == expectedArrayName)
                                {
                                    switch (requestedIndex.ToLower())
                                    {
                                        case "length":
                                            a += (array_.numberOfElements.ToString());
                                            return;
                                            break;
                                        default:
                                            return;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
#endif
#if HASLIST

                // Then check for list items
                if (capturedText.StartsWith("List:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    if ((_placeholder.StartsWith("NameAt:", StringComparison.OrdinalIgnoreCase)) || (_placeholder.StartsWith("ValueAt:", StringComparison.OrdinalIgnoreCase)))
                    {
                        int charsToRemove = 7;
                        int nameOrValue = -1;
                        if (_placeholder.StartsWith("NameAt:", StringComparison.OrdinalIgnoreCase))
                        {
                            nameOrValue = 1;
                        } else if (_placeholder.StartsWith("ValueAt:", StringComparison.OrdinalIgnoreCase))
                        {
                            charsToRemove++;
                            nameOrValue = 2;
                        }

                            // Referencing an index position within the list
                        string place_ = _placeholder.Remove(0, charsToRemove);
                        string[] items_ = place_.Split(',');
                        if (items_.Length == 2)
                        {
                            bool validName = false;
                            bool isNumber = IsNumeric(items_[1].Trim());
                            
                            if (isNumber == true)
                            {
                                string a_ = ConvertNumericalVariable(items_[1]);
                                int indexednumber = Int32.Parse(a_);

                                foreach (LocalList list in local_lists)
                                {
                                    if (list.Name == items_[0])
                                    {
                                        string b = "";
                                        switch (nameOrValue)
                                        {
                                            case 1:
                                                b += list.GetNameAtIndex(indexednumber);
                                                break;
                                            case 2:
                                                b += list.GetValueAtIndex(indexednumber);
                                                break;
                                            default:
                                                break;
                                        }
                                        
                                        a += b;
                                        validName = true;
                                        break;
                                    }
                                }
                                if (validName == false)
                                {
                                    Console.WriteLine($"Could not locate list: {items_[0]}.");
                                }
                            } else
                            {
                                Console.WriteLine($"Not a valid number: {ConvertNumericalVariable(items_[1])}. If you are referencing a variable within [ ] brackets, make sure to use {{ }} brackets");
                            }

                        } else if (_placeholder.StartsWith("ValueAt:", StringComparison.OrdinalIgnoreCase))
                        {

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
                                foreach (LocalList list in local_lists)
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
                        foreach (LocalList list in local_lists)
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
#endif
#if HASIFILESYSTEM
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
#endif
#if HASGUICODE
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
                                    GUI_Checkbox checkboxitem = consoleDirector.GUICheckboxToAdd.Find(z => z.GUIObjName == expectedGUIObjName);
                                    if (checkboxitem != null)
                                    {
                                        switch (expectedReturnProperty)
                                        {
                                            case 0:
                                                a += checkboxitem.newCheckbox.Text.ToString();
                                                break;
                                            case 1:
                                                string x_ = checkboxitem.newCheckbox.X.ToString();
                                                string filteredString = new string(x_.Where(char.IsDigit).ToArray());
                                                a += filteredString;
                                                break;
                                            case 2:
                                                string y_ = checkboxitem.newCheckbox.Y.ToString();
                                                string filteredString1 = new string(y_.Where(char.IsDigit).ToArray());
                                                a += filteredString1;
                                                break;
                                            case 3:
                                                string hei_ = checkboxitem.newCheckbox.Height.ToString();
                                                string filteredString2 = new string(hei_.Where(char.IsDigit).ToArray());
                                                a += filteredString2;
                                                break;
                                            case 4:
                                                string wid_ = checkboxitem.newCheckbox.Width.ToString();
                                                string filteredString3 = new string(wid_.Where(char.IsDigit).ToArray());
                                                a += filteredString3;
                                                break;
                                            case 5:
                                                string checked_ = checkboxitem.newCheckbox.Checked.ToString();
                                                a += checked_;
                                                break;
                                        }
                                    }
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
#endif
#if HASNETCODE
                // Then check for data packet properties
                if (capturedText.StartsWith("DataPacket:", StringComparison.OrdinalIgnoreCase))
                {
                    string placeholder_ = capturedText.Remove(0, 11);
                    string soughtProperty = "";
                    int propertyAfter = -1;
                    bool validPropertyFound = false;
                    string[] validProperts = { "id:", "senderaddress:", "type:", "data:" };
                    for (int k = 0; k < validProperts.Length; k++)
                    {
                        if (placeholder_.StartsWith(validProperts[k], StringComparison.OrdinalIgnoreCase))
                        {
                            soughtProperty = validProperts[k].Trim(':');
                            placeholder_ = placeholder_.Remove(0, validProperts[k].Length).TrimEnd();
                            validPropertyFound = true;
                            propertyAfter = k;
                            break;
                        }
                    }

                    string[] items_ = placeholder_.Split(',');
                    bool validSplit = items_.Length == 2;
                    if (validSplit == true)
                    {
                        string expectedTCBObjName = SetVariableValue(items_[0]);
                        string expectedDataPacketIdentifier = SetVariableValue(items_[1]);
                        
                        if (validPropertyFound == true)
                        {
                            bool TCBObjectExists= NameInUse(expectedTCBObjName);
                            bool isTCBObj = namesInUse[expectedTCBObjName] == objectClass.TCPNetObj;
                            bool ValidTCBObject = ((TCBObjectExists == true) && (isTCBObj == true));

                            bool dataPacketExists = false;

                            if (ValidTCBObject == true)
                            {
                                objectClass objc = namesInUse[expectedTCBObjName];
                                foreach (TCPNetSettings TCPObj in activeTCPObjects)
                                {
                                    if (TCPObj.ParentName == expectedTCBObjName)
                                    {
                                        dataPacket dp = TCPObj.incomingDataPackets.Find(x => x.ID == expectedDataPacketIdentifier);
                                        if (dp != null)
                                        {
                                            dataPacketExists = true;
                                            switch (propertyAfter)
                                            {
                                                case 0:
                                                    a += dp.ID;
                                                    break;
                                                case 1:
                                                    a += dp.senderAddress;
                                                    break;
                                                case 2:
                                                    string e = TCPObj.GetDescription(dp.objType);
                                                    a += e;
                                                    break;
                                                case 3:
                                                        a += dp.objData;
                                                    break;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            dataPacket dp2 = TCPObj.outgoingDataPackets.Find(x => x.ID == expectedDataPacketIdentifier);
                                            if (dp2 != null)
                                            {
                                                dataPacketExists = true;
                                                switch (propertyAfter)
                                                {
                                                    case 0:
                                                        a += dp2.ID;
                                                        break;
                                                    case 1:
                                                        a += dp2.senderAddress;
                                                        break;
                                                    case 2:
                                                        string e = TCPObj.GetDescription(dp2.objType);
                                                        a += e;
                                                        break;
                                                    case 3:
                                                            a += dp2.objData;
                                                        break;
                                                }
                                                break;
                                            } else
                                            {
                                                Console.WriteLine($"There are no incoming or outgoing data packets in TCP object {expectedTCBObjName} with id {expectedDataPacketIdentifier}.");
                                            }
                                        
                                        
                                        }
                                    }
                                }

                            }
                            else if (TCBObjectExists == false)
                            {
                                Console.WriteLine($"Expected TCB object {expectedTCBObjName} not found.");
                            }
                            else if (ValidTCBObject == false)
                            {
                                Console.WriteLine($"{expectedTCBObjName} is not a valid TCB object.");
                            }
                        } else
                        {
                            Console.WriteLine($"{soughtProperty} is not a valid data packet property. Expecting: id, senderaddress, type, data.");
                        }
                    } else
                    {
                        Console.WriteLine("Expecting TCP object and packet ID separated by comma.");
                    }
                }
#endif
                // Finally, check for newline
                if (capturedText.Equals("nl", StringComparison.OrdinalIgnoreCase)) { a = a + Environment.NewLine; }
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
                // Check if object is in range of
                if (capturedText.StartsWith("InRange:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 8);
                    bool validVar = false, validInt = false, validMin = false, validMax = false, clearToProcees = false;
                    string lowestval = "", highestval = "", expectedVariable = "", compstr = ""; int minval = -1, maxval = -1, compval = -1, validChecks = 0;
                    if (!_placeholder.Contains(","))
                    {
                        Console.WriteLine("Expecting integer name, lowest value, and highest value separated by comma.");
                        return;
                    }
                    string[] placeholderSplit = _placeholder.Split(',');
                    if (placeholderSplit.Length != 3)
                    {
                        Console.WriteLine("Cannot take more or less than 3 comma-separated value.");
                        return;
                    }
                    else
                    {
                        expectedVariable = SetVariableValue(placeholderSplit[0]);
                        validVar = LocalVariableExists(expectedVariable);
                        if (validVar == true)
                        {
                            LocalVariable expectedNumericalVar = local_variables.Find(o => o.Name == expectedVariable);
                            if (IsNumeric(ConvertNumericalVariable(expectedNumericalVar.Value.TrimEnd()))) { compstr = ConvertNumericalVariable(expectedNumericalVar.Value.TrimEnd()); validInt = true; } else { Console.WriteLine($"Not a valid number: {ConvertNumericalVariable(placeholderSplit[1])}"); return; }
                        }
                        else
                        {
                            Console.WriteLine($"Could not find or locate {expectedVariable}"); return;
                        }
                        lowestval = ConvertNumericalVariable(placeholderSplit[1]);
                        highestval = ConvertNumericalVariable(placeholderSplit[2]);
                        validMin = IsNumeric(lowestval);
                        validMax = IsNumeric(highestval);

                        if (validInt == true) { compval = Int32.Parse(compstr); validChecks++; } else { Console.WriteLine($"Not a valid number: {compval}."); return; }
                        if (validMin == true) { minval = Int32.Parse(lowestval); validChecks++; } else { Console.WriteLine($"Not a valid number: {lowestval}. If you are referencing a variable within [ ] brackets, make sure to use {{ }} brackets."); return; }
                        if (validMax == true) { maxval = Int32.Parse(highestval); validChecks++; } else { Console.WriteLine($"Not a valid number: {highestval}. If you are referencing a variable within [ ] brackets, make sure to use {{ }} brackets."); return; }
                        if (minval >= maxval) { Console.WriteLine($"Expecting lower value then higher value. {minval} is not less than {maxval}"); return; }
                        if ((minval < maxval) && (minval != maxval)) { validChecks++; }

                        if (validChecks == 4)
                        {
                            if ((compval < maxval) && (compval > minval))
                            {
                                Console.Write("True");
                            }
                            else
                            {
                                Console.Write("False");
                            }
                        }
                    }

                }
                // Check is object contains
                if (capturedText.StartsWith("Contains:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 9);
                    bool validVar = false, caseSensitive = false;
                    string expectedVariable = "", comparingValue = "";
                    if ((_placeholder.StartsWith("CS:", StringComparison.OrdinalIgnoreCase)) || (_placeholder.StartsWith("NCS:", StringComparison.OrdinalIgnoreCase)))
                    {
                        string placeholder2_ = "";
                        if (_placeholder.StartsWith("CS:", StringComparison.OrdinalIgnoreCase))
                        {
                            caseSensitive = true;
                            placeholder2_ += _placeholder.Remove(0, 3);
                        }
                        else if (_placeholder.StartsWith("NCS:", StringComparison.OrdinalIgnoreCase))
                        {
                            caseSensitive = false;
                            placeholder2_ += _placeholder.Remove(0, 4);
                        }

                        if (placeholder2_.Contains(','))
                        {
                            string[] placeholderSplit = placeholder2_.Split(',');
                            if (placeholderSplit.Length == 2)
                            {
                                expectedVariable += placeholderSplit[0];
                                comparingValue += SetVariableValue(placeholderSplit[1]);
                                validVar = LocalVariableExists(expectedVariable);
                            }
                            else
                            {
                                Console.WriteLine("Cannot take more or less than 2 comma-separated value."); return;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Expecting variable name and comparing value separated by comma."); return;
                        }

                        if (validVar == true)
                        {
                            LocalVariable tempvariable_ = local_variables.Find(i => i.Name == expectedVariable);
                            if (tempvariable_ != null)
                            {
                                string temp__ = tempvariable_.Value;
                                if (caseSensitive == false)
                                {
                                    if (temp__.Contains(comparingValue, StringComparison.OrdinalIgnoreCase)) { Console.Write("True"); } else { Console.Write("False"); }
                                } else
                                {
                                    if (temp__.Contains(comparingValue, StringComparison.Ordinal)) { Console.Write("True"); } else { Console.Write("False"); }

                                }
                            }
                            else
                            {
                                // Redundancy never hurt anyone
                                Console.WriteLine($"Could not find or locate {expectedVariable}"); return;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not find or locate {expectedVariable}"); return;
                        }

                    }
                    else
                    {
                        Console.WriteLine("Contains: must be followed with CS: for case-sensitive or NCS: for not case-sensitive."); return;
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
                // Then check for date and time
                if (capturedText.StartsWith("Date:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    string processedTimeDate = timedate_handler.returnDateTime(_placeholder);
                    Console.Write(processedTimeDate);
                }

                // Then check for an array refernce
#if HASARRAY
                if (capturedText.StartsWith("Array:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] nonIntValues = { "Length" };
                    string _placeholder = capturedText.Remove(0, 6), expectedArrayName = "", requestedIndex = "";
                    bool validName = false, validType = false, validIndex = false, nonIntegerValue = false;
                    Array_Type arraytype_ = Array_Type.None;
                    if (!_placeholder.Contains(","))
                    {
                        Console.WriteLine("Expecting array name and index position separated by comma.");
                        return;
                    }
                    else
                    {
                        string[] placeholderSplit = _placeholder.Split(",");
                        if (placeholderSplit.Length != 2)
                        {
                            Console.WriteLine("Cannot take more than 1 comma-separated value.");
                            return;
                        } else
                        {
                            expectedArrayName = placeholderSplit[0];
                            requestedIndex = ConvertNumericalVariable(placeholderSplit[1]).TrimEnd();
                            validIndex = IsNumeric(requestedIndex);
                            if (namesInUse.ContainsKey(expectedArrayName)) { validName = true; } else { Console.WriteLine($"Could not find or locate {expectedArrayName}"); return; }
                            if (namesInUse[expectedArrayName] == objectClass.VarArray) { validType = true; } else { Console.WriteLine($"Wrong type of variable or object: {expectedArrayName}, expecting: array of type bool, int, string or float"); return; }
                        }

                        foreach (string s in nonIntValues)
                        {
                            if (requestedIndex.Equals(s, StringComparison.OrdinalIgnoreCase))
                            {
                                nonIntegerValue = true;
                                break;
                            }
                        }
                        if ((validIndex == false) && (nonIntegerValue == false))
                        {
                            Console.WriteLine($"Not a valid integer: {ConvertNumericalVariable(placeholderSplit[1])}");
                            return;
                        } 
                        
                        if ((nonIntegerValue == false) && (validIndex == true))
                        {
                            int indx = Int32.Parse(requestedIndex);

                            foreach (array_baseitem array_ in local_arrays) 
                            {
                                if (array_.Name == expectedArrayName)
                                {
                                    if (indx > array_.numberOfElements)
                                    {
                                        Console.WriteLine($"Array {expectedArrayName} has {array_.numberOfElements.ToString()} elements, requested index {requestedIndex} is invalid.");
                                        return;
                                    }
                                    else
                                    {
                                        Console.Write(array_.getElementAt(indx));
                                    }
                                }
                            }

                        } else if ((nonIntegerValue == true) && (validIndex == false))
                        {
                            foreach (array_baseitem array_ in local_arrays)
                            {
                                if (array_.Name == expectedArrayName)
                                {
                                    switch (requestedIndex.ToLower())
                                    {
                                        case "length":
                                            Console.Write(array_.numberOfElements.ToString());
                                            return;
                                            break;
                                        default:
                                            return;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
#endif 
                // Then check for a list reference
#if HASLIST
                if (capturedText.StartsWith("List:", StringComparison.OrdinalIgnoreCase))
                {
                    string _placeholder = capturedText.Remove(0, 5);
                    if ((_placeholder.StartsWith("NameAt:", StringComparison.OrdinalIgnoreCase)) || (_placeholder.StartsWith("ValueAt:", StringComparison.OrdinalIgnoreCase)))
                    {
                        int charsToRemove = 7;
                        int nameOrValue = -1;
                        if (_placeholder.StartsWith("NameAt:", StringComparison.OrdinalIgnoreCase))
                        {
                            nameOrValue = 1;
                        }
                        else if (_placeholder.StartsWith("ValueAt:", StringComparison.OrdinalIgnoreCase))
                        {
                            charsToRemove++;
                            nameOrValue = 2;
                        }

                        // Referencing an index position within the list
                        string place_ = _placeholder.Remove(0, charsToRemove);
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

                                foreach (LocalList list in local_lists)
                                {
                                    if (list.Name == items_[0])
                                    {
                                        string b = "";
                                        switch (nameOrValue)
                                        {
                                            case 1:
                                                b += list.GetNameAtIndex(indexednumber);
                                                break;
                                            case 2:
                                                b += list.GetValueAtIndex(indexednumber);
                                                break;
                                            default:
                                                break;
                                        }
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
                                foreach (LocalList list in local_lists)
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
                            foreach (LocalList list in local_lists)
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
                        foreach (LocalList list in local_lists)
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
#endif
                // Then check for filesystem
#if HASIFILESYSTEM

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

                // Then check for references to GUI items
#endif
#if HASGUICODE
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
#endif
                // Then check for data packet references
#if HASNETCODE

                if (capturedText.StartsWith("DataPacket:", StringComparison.OrdinalIgnoreCase))
                {
                    string placeholder_ = capturedText.Remove(0, 11);
                    string soughtProperty = "";
                    int propertyAfter = -1;
                    bool validPropertyFound = false;
                    string[] validProperts = { "id:", "senderaddress:", "type:", "data:" };
                    for (int k = 0; k < validProperts.Length; k++)
                    {
                        if (placeholder_.StartsWith(validProperts[k], StringComparison.OrdinalIgnoreCase))
                        {
                            soughtProperty = validProperts[k].Trim(':');
                            placeholder_ = placeholder_.Remove(0, validProperts[k].Length).TrimEnd();
                            validPropertyFound = true;
                            propertyAfter = k;
                            break;
                        }
                    }

                    string[] items_ = placeholder_.Split(',');
                    bool validSplit = items_.Length == 2;
                    if (validSplit == true)
                    {
                        string expectedTCBObjName = SetVariableValue(items_[0]).TrimEnd();
                        string expectedDataPacketIdentifier = SetVariableValue(items_[1].TrimEnd());
                       

                        if (validPropertyFound == true)
                        {
                            bool TCBObjectExists = NameInUse(expectedTCBObjName);
                            bool isTCBObj = false;
                            if (TCBObjectExists == true)
                            {
                                isTCBObj = namesInUse[expectedTCBObjName] == objectClass.TCPNetObj;
                            }
                            bool ValidTCBObject = ((TCBObjectExists == true) && (isTCBObj == true));

                            bool dataPacketExists = false;

                            if (ValidTCBObject == true)
                            {
                                foreach (TCPNetSettings TCPObj in activeTCPObjects)
                                {
                                    if (TCPObj.ParentName == expectedTCBObjName)
                                    {
                                        dataPacket dp = TCPObj.incomingDataPackets.Find(x => x.ID == expectedDataPacketIdentifier);
                                        if (dp != null)
                                        {
                                            dataPacketExists = true;
                                            switch (propertyAfter)
                                            {
                                                case 0:
                                                    Console.Write(dp.ID);
                                                    break;
                                                case 1:
                                                    Console.Write(dp.senderAddress);
                                                    break;
                                                case 2:
                                                    string e = TCPObj.GetDescription(dp.objType);
                                                    Console.Write(e);
                                                    break;
                                                case 3:
                                                    Console.Write(dp.objData);
                                                    break;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            dataPacket dp2 = TCPObj.outgoingDataPackets.Find(x => x.ID == expectedDataPacketIdentifier);
                                            if (dp2 != null)
                                            {
                                                dataPacketExists = true;
                                                switch (propertyAfter)
                                                {
                                                    case 0:
                                                        Console.Write(dp2.ID);
                                                        break;
                                                    case 1:
                                                        Console.Write(dp2.senderAddress);
                                                        break;
                                                    case 2:
                                                        string e = TCPObj.GetDescription(dp2.objType);
                                                        Console.Write(e);
                                                        break;
                                                    case 3:
                                                         Console.Write(dp2.objData);
                                                        break;
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"There are no incoming or outgoing data packets in TCP object {expectedTCBObjName} with id {expectedDataPacketIdentifier}.");
                                            }


                                        }
                                    }
                                }

                            }
                            else if (TCBObjectExists == false)
                            {
                                Console.WriteLine($"Expected TCB object {expectedTCBObjName} not found.");
                            }
                            else if (ValidTCBObject == false)
                            {
                                Console.WriteLine($"{expectedTCBObjName} is not a valid TCB object.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{ soughtProperty} is not a valid data packet property.Expecting: id, senderaddress, type, data.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Expecting TCP object and packet ID separated by comma.");
                    }
                }
#endif
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
            } else if (environmentalVars.ContainsKey(name))
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
                if (environmentalVars.ContainsKey(capturedText))
                {
                    bool validInt = true;
                    switch (capturedText) 
                    {
                        case "Backcolor":
                            validInt = false;
                            break;
                        case "Forecolor":
                            validInt = false;
                            break;
                        case "Title":
                            validInt = false;
                            break;
                    }
                    if (validInt == true)
                    {
                        a += environmentalVars[capturedText];
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
        ///////////////////////////////////////////////////
#if HASARRAY

        public Array_Type getArrayType(string arrayName)
        {
            foreach (int_array a in local_arrays)
            {
                if (a.Name == arrayName)
                {
                    return a.arrayType;
                }
            }
            foreach (float_array b in local_arrays)
            {
                if (b.Name == arrayName)
                {
                    return b.arrayType;
                }
            }
            foreach (bool_array c in local_arrays)
            {
                if(c.Name == arrayName)
                {
                    return c.arrayType;
                }
            }
            foreach(str_array d in local_arrays)
            {
                if(d.Name == arrayName)
                {
                    return d.arrayType;
                }
            }
            return Array_Type.None;
        }
#endif
        public string GetDescription(Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           System.Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
}
            }
            return null;
        }

    }
}