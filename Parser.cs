using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Buffers.Binary;
using GyroPrompt;
using System.Runtime.ExceptionServices;
using GyroPrompt.Functions;
using System.Drawing;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using GyroPrompt.NetworkObjects;
using GyroPrompt.GraphicalPrompt;
using BlockchainSpace;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Runtime.InteropServices;
using System.Net;
using System.Runtime.Versioning;
using GyroPrompt.IO;
using Microsoft.VisualBasic.FileIO;
using System.Net.NetworkInformation;
using System.Xml.Schema;

namespace GyroPromptNameSpace
{
    public class Parser
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            int dwDesireAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetCurrentConsoleFont(
            IntPtr hConsoleOutput,
            bool bMaximumWindow,
            [Out][MarshalAs(UnmanagedType.LPStruct)]ConsoleFontInfo lpConsoleCurrentFont);

        [StructLayout(LayoutKind.Sequential)]
        internal class ConsoleFontInfo
        {
            internal int nFont;
            internal Coord dwFontSize;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Coord
        {
            [FieldOffset(0)]
            internal short X;
            [FieldOffset(2)]
            internal short Y;
        }

        private const int GENERIC_READ = unchecked((int)0x80000000);
        private const int GENERIC_WRITE = 0x40000000;
        private const int FILE_SHARE_READ = 1;
        private const int FILE_SHARE_WRITE = 2;
        private const int INVALID_HANDLE_VALUE = -1;
        private const int OPEN_EXISTING = 3;

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        public bool RunningScript = false;
        public int CurrentLine { get; set; }
        public bool AddPrompt = true;

        public List<Variable> ActiveVariables = new List<Variable>();
        public List<Variable> PlaceholderVariable = new List<Variable>(); // for running scripts

        public List<TimedFunction> ActiveFunctions = new List<TimedFunction>();
        public List<TimedFunction> PlaceholderFunctions = new List<TimedFunction>(); // for running scripts

        public List<CSV_Spreadsheet> ActiveSpreadsheets = new List<CSV_Spreadsheet>();
        public List<CSV_Spreadsheet> PlaceholderSpreadsheets = new List<CSV_Spreadsheet>(); // for running scripts

        public List<VariableList> ActiveVariableList = new List<VariableList>();

        public List<Blockchain> ActiveChains = new List<Blockchain>();

        public List<SerialPortChannel> ActivePortChannels = new List<SerialPortChannel>();

        public int MaxLines { get; set; }
        public List<string> AllLines = new List<string>();
        public string placeholder;

        public FileSystemIO GPFileSystem = new FileSystemIO();
        public GyroCloud gyroCloud = new GyroCloud();
        public List<string> CloudScript = new List<string>();

        public int ServerPort = 5555;

        public ConsoleColor textColor = new ConsoleColor(); 

        public bool cloudOn { get; set; }

        public void InitiateEnvironmentalVariables()
        {
            cloudOn = gyroCloud.Open();

            Variable SerialBuffer = new Variable(true, "0", "SERIALBUFFER");
            SerialBuffer.IsInteger = false;
            SerialBuffer.IsString = true;
            ActiveVariables.Add(SerialBuffer);

            Variable SocketBuffer = new Variable(true, "0", "SOCKETBUFFER");
            SocketBuffer.IsInteger = false;
            SocketBuffer.IsString = true;
            ActiveVariables.Add(SocketBuffer);

            Variable WindowHeight = new Variable(false, Console.WindowHeight.ToString(), "WINDOWHEIGHT");
            WindowHeight.Number = Console.WindowHeight;
            WindowHeight.IsInteger = true;
            WindowHeight.IsString = false;
            ActiveVariables.Add(WindowHeight);

            Variable WindowWidth = new Variable(false, Console.WindowWidth.ToString(), "WINDOWWIDTH");
            WindowWidth.Number = Console.WindowWidth;
            WindowWidth.IsInteger = true;
            WindowWidth.IsString = false;
            ActiveVariables.Add(WindowWidth);

            Variable CursorX = new Variable(false, Console.CursorLeft.ToString(), "CURSORX");
            CursorX.IsString = false;
            CursorX.IsInteger = true;
            ActiveVariables.Add(CursorX);

            Variable CursorY = new Variable(false, Console.CursorTop.ToString(), "CURSORY");
            CursorY.IsString = false;
            CursorY.IsInteger = true;
            ActiveVariables.Add(CursorY);

            Variable BufferDelay = new Variable(false, "200", "BUFFERDELAY");
            BufferDelay.IsInteger = true;
            BufferDelay.IsString = false;
            BufferDelay.Number = 200;
            ActiveVariables.Add(BufferDelay);

            Thread refreshing_environmental_variable = (new Thread(RefreshEnvVars));
            refreshing_environmental_variable.Start();
        }

        public void Parse(string Input)
        {
            try
            {
                if (Input.StartsWith(' '))
                {
                    Input = Input.TrimStart(' ');
                } // Trim any leading whitespace

                if (Input == "TEST")
                {
                    DogeCoinNetwork test = new DogeCoinNetwork();
                    test.Initialize();
                }

                if (Input.StartsWith ("JUJU "))
                {
                    string[] checker = Input.Split(' ');

                    gyroCloud.AvailableData_1gigabyte(checker[1]);
                    
                }


                if (Input.StartsWith(" CLOUD "))
                {
                    if (cloudOn == true)
                    {

                    }
                    else
                    {
                        SendError($"Currently not connected to GyroCloud.\n");
                        UserInquiry tryConnection = new UserInquiry("Attempt to establish connection to GyroCloud?", 0);
                        string result = tryConnection.Response();
                        if (result == "1")
                        {
                            cloudOn = gyroCloud.Open();
                            if (cloudOn == false)
                            {
                                SendError("Unable to establish connection.");
                            }
                            else
                            {
                                Console.WriteLine("Connected to GyroCloud!");
                            }
                        }
                    } // if not connected, ask user to try to reconnect once
                }
                if (Input.StartsWith("/NEWUSER "))
                {
                    string[] string_array = Input.Split(' ', 3);
                    gyroCloud.CreateUser(string_array[1], string_array[2]);
                }
                if (Input.StartsWith("/UPLOAD "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    if (File.Exists(string_array[1]))
                    {
                        gyroCloud.UploadFile(string_array[1]);
                    }
                    else
                    {
                        SendError($"Could not located file {string_array[1]}.");
                    }
                }
                if (Input.StartsWith("/UPLOADSCRIPT "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    gyroCloud.UploadScript(string_array[1]);
                }
                if (Input.StartsWith("/DOWNLOAD "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    gyroCloud.DownloadFile(string_array[1]);
                }
                if (Input.StartsWith("/RUNSCRIPT "))
                {
                    string name = "";
                    string[] checker_array = Input.Split(' ', 2);
                    string test = gyroCloud.RunScript(checker_array[1], ref name);
                    string[] script_content = test.Split('\n');
                    if (name != "")
                    {
                        RunScriptFromCloud(script_content, name);
                    }
                    else
                    {
                        SendError($"Could not located script {checker_array[1]}.");
                    }
                }
                if (Input.StartsWith("/SENDDOGE "))
                {
                    string[] checker = Input.Split(' ', 5);
                    string user = checker[1];
                    string pass = checker[2];
                    int qty = Convert.ToInt32(checker[3]);
                    string recipient = checker[4];
                    gyroCloud.DogeCoinTransfer_UserToUser(user, pass, qty, recipient);
                }

                if (Input.StartsWith("/NEWCSV"))
                {
                    string[] data = Input.Split(' ', 3);
                    CSV_Spreadsheet new_sheet = new CSV_Spreadsheet(data[1], data[2]);
                    ActiveSpreadsheets.Add(new_sheet);
                }
                if (Input.StartsWith("/CSV_NEWCOLUMN"))
                {
                    string[] data = Input.Split(' ', 3);
                    foreach(CSV_Spreadsheet _spreadsheet in ActiveSpreadsheets)
                    {
                        if (_spreadsheet.Spreadsheet_Name == data[1])
                        {
                            _spreadsheet.AddColumn(data[2]);
                        }
                    }
                }
                if (Input.StartsWith("/CSV_PRINT"))
                {
                    string[] data = Input.Split(' ', 2);
                    foreach(CSV_Spreadsheet _spreadsheet in ActiveSpreadsheets)
                    {
                        if (_spreadsheet.Spreadsheet_Name == data[1])
                        {
                            _spreadsheet.PrintSpreadsheet();
                        }
                    }
                }


                if (Input.StartsWith("TOGGLE "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    if (string_array.Length == 2)
                    {
                        bool FoundVar = false;
                        bool Toggled = false;
                        foreach (Variable var in ActiveVariables)
                        {
                            if (var.VarName == string_array[1])
                            {
                                if (var.Message == "0" || var.Number == 0)
                                {
                                    var.Number = 1;
                                    var.Message = "1";
                                    Toggled = true;
                                }
                                else if (var.Message == "1" || var.Number == 1)
                                {
                                    var.Message = "0";
                                    var.Number = 0;
                                    Toggled = true;
                                }
                                FoundVar = true;
                                break;
                            }
                        }
                        if (Toggled == false && FoundVar == true)
                        {
                            SendError($"Variable {string_array[1]} must have value as 0 or 1 to treat like boolean to toggle.");
                        }
                        if (FoundVar == false)
                        {
                            SendError($"Could not locate variable {string_array[1]}.");
                        }
                    }
                    else
                    {
                        SendError("Incorrect syntax for toggle.");
                    }
                }

                if (Input.StartsWith("APIGET "))
                {
                    string[] _checker = Input.Split(' ', 3);
                    if (_checker[1].StartsWith('_') && _checker.Length == 3)
                    {
                        bool success = true;
                        string responseString = "";
                        string _apicall = _checker[2];
                        try
                        {
                            var request = (HttpWebRequest)WebRequest.Create(_apicall);
                            var response = (HttpWebResponse)request.GetResponse();
                            using (var stream = response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    responseString = reader.ReadToEnd();
                                }
                            }

                            //set variable to equal response string
                        }
                        catch
                        {
                            success = false;
                        }
                        if (success == true)
                        {
                            bool isFound = false;
                            StringBuilder _varbuild = new StringBuilder();
                            _varbuild.Append(_checker[1]);
                            _varbuild.Remove(0, 1);
                            string varName = _varbuild.ToString();
                            foreach (Variable _var in ActiveVariables)
                            {
                                if (_var.VarName == varName)
                                {
                                    isFound = true;
                                    _var.Message = responseString.ToString();
                                    break;
                                }
                            }
                            if (isFound == false)
                            {
                                SendError($"Could not locate variable {varName}.");
                            }
                        }
                        else
                        {
                            SendError("Invalid API call.");
                        }
                    }
                    else
                    {
                        SendError("Incorrect syntax for API call.");
                    }
                }

                if (Input.StartsWith("WOLFRAM "))
                {
                    string[] _checker = Input.Split(' ', 3);
                    if (_checker[1].StartsWith('_') && _checker.Length >= 3)
                    {
                        string[] string_array = Input.Split(' ', 3);
                        string query = string_array[2].Replace(' ', '+');
                        string outputVariable = string_array[1].Remove(0, 1);

                        bool validVariable = false;

                        foreach (Variable var in ActiveVariables)
                        {
                            if (var.VarName == outputVariable)
                            {
                                validVariable = true;
                                if (var.IsString == true)
                                {
                                    try
                                    {
                                        var request = (HttpWebRequest)WebRequest.Create($"http://api.wolframalpha.com/v1/result?appid=4RAYGE-UYK4UELLKV&i={query}%3f");
                                        var response = (HttpWebResponse)request.GetResponse();
                                        string responseString;
                                        using (var stream = response.GetResponseStream())
                                        {
                                            using (var reader = new StreamReader(stream))
                                            {
                                                responseString = reader.ReadToEnd();
                                            }
                                        }
                                        var.Message = responseString;
                                    }
                                    catch
                                    {
                                        SendError($"Query failed. Check connection.");
                                    }
                                }
                                else
                                {
                                    SendError($"Output variable {outputVariable} is not a string.");
                                }
                                break;
                            }
                        }


                    }
                    else if (_checker.Length >= 3)
                    {
                        string[] string_array = Input.Split(' ', 2);
                        string query = string_array[1].Replace(' ', '+');

                        try
                        {
                            var request = (HttpWebRequest)WebRequest.Create($"http://api.wolframalpha.com/v1/result?appid=4RAYGE-UYK4UELLKV&i={query}%3f");
                            var response = (HttpWebResponse)request.GetResponse();
                            string responseString;
                            using (var stream = response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    responseString = reader.ReadToEnd();
                                }
                            }
                            Console.WriteLine($"\nWolfram Response: {responseString}\n");
                        }
                        catch
                        {
                            SendError($"Query failed. Check connection.");
                        }

                    }
                    else
                    {
                        SendError("Invalid question for WOLFRAM query.");
                    }
                }

                if (Input.StartsWith("HOST"))
                {
                    if (Input.Length == 4)
                    {
                        DNSInfo dnsInfo = new DNSInfo();
                        dnsInfo.ShowNetInfo();
                    }
                    else
                    {
                        SendError("Invalid syntax for host information.");
                    }
                }

                if (Input.StartsWith("PING "))
                {
                    string[] checker_ = Input.Split(' ', 3);
                    if (checker_[1].StartsWith('_')) // Will output ping result to variable 
                    {
                        string[] string_array = Input.Split(' ', 6);
                        if (string_array.Length == 6)
                        {
                            StringBuilder _outvar = new StringBuilder();
                            _outvar.Append(string_array[1]);
                            _outvar.Remove(0, 1);

                            bool allInteger = false;
                            int to = 0;
                            int cnt = 0;
                            string pingData = ConvertAllVariable(string_array[5], true);
                            if (string_array[3].All(char.IsDigit) && string_array[4].All(char.IsDigit))
                            {
                                to = Convert.ToInt32(string_array[3]);
                                cnt = Convert.ToInt32(string_array[4]);
                                allInteger = true;
                            }
                            string outputVar = Convert.ToString(_outvar);

                            bool validVar = false;
                            if (allInteger == true)
                            {
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (var.VarName == outputVar)
                                    {
                                        validVar = true;
                                        NetworkPing newping = new NetworkPing();
                                        StringBuilder newStr = new StringBuilder();
                                        int x = 1;
                                        for (; cnt > 0; cnt--)
                                        {
                                            newStr.Append($"Ping attempt: {x}\t");
                                            newStr.Append(newping.PingAddress(string_array[2], to, pingData, cnt));
                                            x++;
                                        }

                                        var.Message = Convert.ToString(newStr);
                                        if (newping.IsSuccess == true) { var.Number = 1; } else { var.Number = 0; }
                                        break;
                                    }
                                }
                                if (validVar == false)
                                {
                                    SendError($"Could not locate variable {outputVar}.");
                                }
                            }
                            else
                            {
                                SendError("Timeout and count must be all integers.");
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for ping.");
                        }
                    }
                    else // Will not output ping result to variable
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            bool allInteger = false;
                            int to = 0;
                            int cnt = 0;
                            string pingData = ConvertAllVariable(string_array[4], true);
                            if (string_array[2].All(char.IsDigit) && string_array[3].All(char.IsDigit))
                            {
                                to = Convert.ToInt32(string_array[2]);
                                cnt = Convert.ToInt32(string_array[3]);
                                allInteger = true;
                            }

                            if (allInteger == true)
                            {
                                NetworkPing netping = new NetworkPing();
                                int x = 1;
                                for (; cnt > 0; cnt--)
                                {
                                    string _ping = netping.PingAddress(string_array[1], to, pingData, cnt);
                                    Console.Write($"Ping attempt: {x}\t");
                                    Console.WriteLine(_ping);
                                    x++;
                                }
                            }
                            else
                            {
                                SendError("Timeout and count must be all integers.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for ping.");
                        }
                    }
                }

                if (Input.StartsWith("LIST "))
                {
                    string[] checker_array = Input.Split(' ');
                    if (checker_array[1] == "NEW")
                    {
                        if (checker_array.Length == 3)
                        {
                            string[] string_array = Input.Split(' ', 3);
                            bool validName = true;
                            foreach (VariableList varlist in ActiveVariableList)
                            {
                                if (varlist.Name == string_array[2])
                                {
                                    validName = false;
                                    SendError($"Unable to create variable list {string_array[2]}, name already in use.");
                                    break;
                                }
                            }
                            if (validName == true)
                            {
                                VariableList varList = new VariableList(string_array[2]);
                                ActiveVariableList.Add(varList);
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for LIST command.");
                        }
                    }

                    if (checker_array[1] == "ADD")
                    {
                        if (checker_array.Length == 4)
                        {
                            string[] string_array = Input.Split(' ', 4);
                            bool validName = false;
                            bool validVar = false;
                            foreach (VariableList varlist in ActiveVariableList)
                            {
                                if (varlist.Name == string_array[2])
                                {
                                    validName = true;
                                    foreach (Variable var in ActiveVariables)
                                    {
                                        if (var.VarName == string_array[3])
                                        {
                                            bool alreadyExists = false;
                                            foreach (Variable theVar in varlist.VarList)
                                            {
                                                if (theVar.VarName == var.VarName)
                                                {
                                                    alreadyExists = true;
                                                    break;
                                                }
                                            }

                                            if (alreadyExists == false)
                                            {
                                                validVar = true;
                                                Variable obj;
                                                obj = var;
                                                varlist.AddItem(obj);
                                                break;
                                            }
                                            else
                                            {
                                                SendError($"Variable {var.VarName} already exists in list {varlist.Name}.");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                            if (validName == false)
                            {
                                SendError($"Unable to find variable list {string_array[2]}, name not in use.");
                            }
                            if (validVar == false)
                            {
                                SendError($"Unable to find variable {string_array[3]}, name not in use.");
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for LIST command.");
                        }
                    }

                    if (checker_array[1] == "REMOVE")
                    {
                        if (checker_array.Length == 4)
                        {
                            string[] string_array = Input.Split(' ', 4);
                            bool validName = false;
                            bool validVar = false;
                            foreach (VariableList varlist in ActiveVariableList)
                            {
                                if (varlist.Name == string_array[2])
                                {
                                    validName = true;
                                    varlist.RemoveItem(string_array[3]);
                                    break;
                                }
                            }
                            if (validName == false)
                            {
                                SendError($"\nUnable to find variable list {string_array[2]}, name not in use.");
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for LIST command.");
                        }
                    }

                    if (checker_array[1] == "SET")
                    {
                        if (checker_array.Length == 4)
                        {
                            bool ValidList = false;
                            foreach (VariableList varlist in ActiveVariableList)
                            {
                                if (varlist.Name == checker_array[2])
                                {
                                    ValidList = true;
                                    string data1 = ConvertAllVariable(checker_array[3], true);
                                    string data = ConvertList(data1);
                                    if (data.All(char.IsDigit))
                                    {
                                        int x = 0;
                                        int y = 0;
                                        foreach (Variable var in varlist.VarList)
                                        {
                                            var.Number = Convert.ToInt32(checker_array[3]);
                                            var.Message = checker_array[3];
                                            if (var.IsString == true)
                                            {
                                                y++;
                                            }
                                            else
                                            {
                                                x++;
                                            }
                                        }
                                        Console.WriteLine($"\n{y} total strings and {x} total integers set to {checker_array[3]}.\n");
                                    }
                                    else
                                    {
                                        int x = 0;
                                        int y = 0;
                                        foreach (Variable var in varlist.VarList)
                                        {
                                            if (var.IsString == true)
                                            {
                                                var.Message = data;
                                                y++;
                                            }
                                            else
                                            {
                                                x++;
                                            }
                                        }
                                        Console.WriteLine($"\n{y} total strings set to {data}.\n{x} total integers ignored.\n");
                                    }

                                    break;
                                }
                            }
                            if (ValidList == false)
                            {
                                SendError("");
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for LIST command.");
                        }
                    }

                    if (checker_array[1] == "PRINT")
                    {
                        foreach (VariableList varlist in ActiveVariableList)
                        {
                            if (varlist.Name == checker_array[2])
                            {
                                Console.WriteLine(varlist.ToString());
                            }
                        }
                    }

                }

                if (Input.StartsWith("SERIALPORT "))
                {
                    string[] checker_array = Input.Split(' ', 5);
                    if (checker_array[1] == "NEWPORT")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            int x = 0;
                            foreach (string s in SerialPort.GetPortNames())
                            {
                                x++;
                            }
                            if (x == 0)
                            {
                                SendError("There are no open ports available.");
                            }
                            else
                            {
                                int bauds = 0;
                                bool NoErrors = true;
                                bool BufferFound = false;
                                if (string_array[3].All(char.IsDigit)) { bauds = Convert.ToInt32(string_array[3]); } else { SendError("Incorrect syntax for new serial port command. Baud rate must contain only digits."); NoErrors = false; }
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (var.VarName == string_array[4])
                                    {
                                        BufferFound = true;
                                        try
                                        {
                                            SerialPortChannel newPort = new SerialPortChannel(string_array[2], bauds, var.VarName);
                                            newPort.Initialize();
                                            ActivePortChannels.Add(newPort);
                                        }
                                        catch { SendError("Unable to create new serial port command. Please check syntax and available com ports."); }
                                        break;
                                    }
                                }
                                if (BufferFound == false && string_array[4] == "default")
                                {
                                    try
                                    {
                                        SerialPortChannel newPort = new SerialPortChannel(string_array[2], bauds, "SERIALBUFFER");
                                        newPort.Initialize();
                                        ActivePortChannels.Add(newPort);
                                        BufferFound = true;
                                    }
                                    catch { SendError("Unable to create new serial port command. Please check syntax and available com ports."); }

                                }
                                if (BufferFound == false) { SendError("Could not find " + string_array[4] + ", please use valid variable as buffer."); }
                            }
                        }
                    }
                    if (checker_array[1] == "SEND")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        bool ChannelExists = false;
                        foreach (SerialPortChannel channel in ActivePortChannels)
                        {
                            if (channel.PortName == string_array[2])
                            {
                                ChannelExists = true;
                                channel.SendData(string_array[3]);
                                break;
                            }
                        }
                        if (ChannelExists == false) { SendError("Could not find serial port channel " + string_array[2] + ".\n"); }
                    }
                    if (checker_array[1] == "ADD")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        if (string_array.Length == 4)
                        {
                            bool IsFound = false;
                            foreach (SerialPortChannel channel in ActivePortChannels)
                            {
                                if (channel.PortName == string_array[2])
                                {
                                    IsFound = true;
                                    channel.AddCommand(string_array[3]);
                                    break;
                                }
                            }
                            if (IsFound == false)
                            {
                                SendError("\nCould not find serial port channel " + string_array[2] + ".\n");
                            }
                        }
                        else { SendError("\nIncorrect syntax for adding command to serial port channel.\n"); }
                    }
                    if (checker_array[1] == "TOGGLE")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        bool Found = false;
                        foreach (SerialPortChannel channel in ActivePortChannels)
                        {
                            if (channel.PortName == string_array[2])
                            {
                                channel.Toggle();
                                Found = true;
                                break;
                            }
                        }
                        if (Found == false) { SendError("Could not located serial port channel " + string_array[2] + ".\n"); }
                    }
                }

                if (Input.StartsWith("SERIALPORT ALLOPEN"))
                {
                    if (Input.Length == 18)
                    {
                        int x = 0;
                        Console.WriteLine("\nAll available ports:\n");
                        foreach (string s in SerialPort.GetPortNames())
                        {
                            Console.WriteLine("{0}", s);
                            x++;
                        }
                        Console.WriteLine("Total available ports: {0}\n", x);
                    }
                    else
                    {
                        SendError("Can not have trailing characters.");
                    }
                }

                if (Input == "GYROLOGO")
                {
                    LoadGyro();
                }

                if (Input.StartsWith("SETCURSOR "))
                {
                    string[] string_array = Input.Split(' ', 3);
                    if (string_array.Length == 3)
                    {
                        string aa = ConvertAllVariable(string_array[1], false);
                        string bb = ConvertAllVariable(string_array[2], false);
                        StringBuilder removeBrackets = new StringBuilder();
                        removeBrackets.Append(aa);
                        removeBrackets.Replace("{", "");
                        removeBrackets.Replace("}", "");
                        aa = removeBrackets.ToString();
                        removeBrackets.Clear();
                        removeBrackets.Append(bb);
                        removeBrackets.Replace("{", "");
                        removeBrackets.Replace("}", "");
                        bb = removeBrackets.ToString();

                        string_array[1] = aa;
                        string_array[2] = bb;
                        if (string_array[1].All(char.IsDigit) && string_array[2].All(char.IsDigit))
                        {
                            int a = Convert.ToInt32(string_array[1]);
                            int b = Convert.ToInt32(string_array[2]);

                            if (a <= Console.BufferWidth && b <= Console.BufferHeight)
                            {
                                Console.SetCursorPosition(a, b);
                            }
                            else
                            {
                                SendError("Cannot set cursor out of bounds.");
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for setting cursor position.");
                        }
                    }
                    else if (string_array.Length == 2)
                    {
                        if (string_array[1] == "ORIGIN")
                        {
                            Console.SetCursorPosition(0, 0);
                        }
                        else
                        {
                            SendError("Incorrect syntax for setting cursor coordinates.");
                        }
                    }
                }

                if (Input.StartsWith("BLOCKCHAIN "))
                {
                    string[] checker_array = Input.Split(' ');
                    if (checker_array[1] == "ERECT")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            if (string_array[2].All(char.IsLetterOrDigit))
                            {
                                Blockchain chain = new Blockchain();
                                chain.BlockchainName = string_array[2];
                                ActiveChains.Add(chain);
                            }
                            else
                            {
                                SendError("Incompatible name for new blockchain " + string_array[2] + ", digits and letters only.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for blockchain command.");
                        }
                    }
                    if (checker_array[1] == "NEWBLOCK")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        if (string_array.Length == 4)
                        {
                            bool FoundChain = false;
                            foreach (Blockchain chain in ActiveChains)
                            {
                                if (chain.BlockchainName == string_array[2])
                                {
                                    string a = ConvertAllVariable(string_array[3], true);
                                    string data = ConvertList(a);
                                    Block newBlock = new Block(DateTime.Now, null, data);
                                    chain.AddBlock(newBlock);
                                    FoundChain = true;
                                    break;
                                }
                            }
                            if (FoundChain == false) { SendError("Could not locate active blockchain " + string_array[2] + ", please check syntax."); }
                        }
                        else
                        {
                            SendError("Incorrect syntax for blockchain command.");
                        }
                    }
                    if (checker_array[1] == "VIEW")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        bool ValidMode = false;
                        if (string_array.Length == 4)
                        {
                            if (string_array[2] == "RAW")
                            {
                                ValidMode = true;
                                bool ValidChain = false;
                                foreach (Blockchain chain in ActiveChains)
                                {
                                    if (chain.BlockchainName == string_array[3])
                                    {
                                        ValidChain = true;
                                        Console.WriteLine("\n" + JsonSerializer.Serialize(chain, default) + "\n");
                                        break;
                                    }
                                }
                                if (ValidChain == false) { SendError("Could no locate active blockchain " + string_array[3] + ", please check syntax."); }
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for blockchain command.");
                        }

                        if (string_array[2] == "BLOCKS")
                        {
                            ValidMode = true;
                            bool ValidChain = false;
                            foreach (Blockchain chain in ActiveChains)
                            {
                                if (chain.BlockchainName == string_array[3])
                                {
                                    ValidChain = true;
                                    int x = 0;
                                    Console.WriteLine("\n" + chain.BlockchainName + " blocks:");
                                    foreach (Block _block in chain.Chain)
                                    {
                                        Console.Write("Block {0}: ", x);
                                        Console.WriteLine(_block.Data);
                                        x++;
                                    }
                                    break;
                                }
                            }
                            Console.Write("\n");
                        }
                        if (ValidMode == false) { SendError("Incorrect syntax for view mode."); }
                    }
                    if (checker_array[1] == "EXPORT")
                    {
                        bool WasFound = false;
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            foreach (Blockchain chain in ActiveChains)
                            {
                                if (chain.BlockchainName == string_array[2])
                                {
                                    File.WriteAllText(string_array[2] + "_blockchain", JsonSerializer.Serialize(chain));
                                    WasFound = true;
                                    break;
                                }
                            }
                            if (WasFound == false) { SendError("Could no locate active blockchain " + string_array[3] + ", please check syntax."); }
                        }
                        else
                        {
                            SendError("Incorrect syntax for blockchain command.");
                        }
                    }
                    if (checker_array[1] == "IMPORT")
                    {

                        bool Duplicate = false;
                        bool WasFound = false;
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            foreach (Blockchain chain in ActiveChains)
                            {
                                if (chain.BlockchainName == string_array[2])
                                {
                                    Duplicate = true;
                                    break;
                                }
                            }
                            if (File.Exists(string_array[2] + "_blockchain") && Duplicate == false)
                            {
                                object[] a = File.ReadAllLines(string_array[2] + "_blockchain");
                                Blockchain chain = new Blockchain();
                                //chain = JsonSerializer.Deserialize(a, null);
                            }
                            else
                            {
                                if (Duplicate == true) { SendError("Can not have duplicate blockchain names."); }
                                if (!File.Exists(string_array[2] + "_blockchain")) { SendError("Could not locate local blockchain."); }
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for blockchain command.");
                        }

                    }
                }

                if (Input.StartsWith("PROMPT "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    bool IsValid = false;
                    if (string_array.Length == 2)
                    {
                        switch (string_array[1])
                        {
                            case "ON":
                                AddPrompt = true;
                                IsValid = true;
                                break;
                            case "OFF":
                                AddPrompt = false;
                                IsValid = true;
                                break;
                            case "TOGGLE":
                                if (AddPrompt == true) { AddPrompt = false; } else if (AddPrompt == false) { AddPrompt = true; }
                                IsValid = true;
                                break;
                        }
                        if (IsValid == false) { SendError("Incorrect syntax for command PROMPT."); }
                    }
                    else
                    {
                        SendError("Incorrect syntax for command PROMPT.");
                    }
                }

                if (Input.StartsWith("TITLE "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    string new_title = ConvertAllVariable(string_array[1], false);
                    string removed_new_title = new_title.Remove(new_title.Length - 1, 1);
                    Console.Title = removed_new_title;
                }

                if (Input.StartsWith("TASK "))
                {
                    ///<summary>
                    /// TASK NEW {NAME} {INT}
                    ///     > creates new task
                    /// TASK ADD {NAME} {COMMAND}
                    ///     > adds command to specific task
                    /// TASK START {NAME}
                    ///     > start task
                    /// TASK STOP {NAME}
                    ///     > stop task
                    /// TASK ADJ {VARIABLE} {INT}
                    ///     > change task's interval
                    /// </summary>
                    string[] checker_array = Input.Split(' ');
                    if (checker_array[1] == "NEW")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        bool ValidName = false;
                        bool ValidNumeric = false;
                        int interval = 0;
                        if (string_array[2].All(char.IsLetterOrDigit))
                        {
                            ValidName = true;
                        }
                        else
                        {
                            SendError("Incorrect syntax for creating new TASK. Name must be only numbers and digits.");
                        }

                        if (string_array[3].All(char.IsDigit))
                        {
                            interval = Convert.ToInt32(string_array[3]);
                            ValidNumeric = true;
                        }
                        else
                        {
                            SendError("Incorrect syntax for creating a new TASK. Integer must only be numbers.");
                        }

                        if (ValidNumeric == true && ValidName == true)
                        {
                            TimedFunction timedFunction = new TimedFunction(interval, string_array[2]);
                            timedFunction.Initiate();
                            ActiveFunctions.Add(timedFunction);
                        }

                    }
                    if (checker_array[1] == "ADD")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        bool NameExists = false;
                        foreach (TimedFunction timedFunction in ActiveFunctions)
                        {
                            if (string_array[2] == timedFunction.Name)
                            {
                                timedFunction.Add(string_array[3]);
                                NameExists = true;
                                break;
                            }
                        }

                        if (NameExists == false)
                        {
                            SendError("Could not locate task " + string_array[2] + ".");
                        }

                    }
                    if (checker_array[1] == "START")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        bool NameExists = false;
                        foreach (TimedFunction timedFunction in ActiveFunctions)
                        {
                            if (string_array[2] == timedFunction.Name)
                            {
                                timedFunction.Active = true;
                                NameExists = true;
                                break;
                            }
                        }

                        if (NameExists == false)
                        {
                            SendError("Could not locate task " + string_array[2] + ".");
                        }
                    }
                    if (checker_array[1] == "STOP")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        bool NameExists = false;
                        foreach (TimedFunction timedFunction in ActiveFunctions)
                        {
                            if (string_array[2] == timedFunction.Name)
                            {
                                timedFunction.Active = false;
                                NameExists = true;
                                break;
                            }
                        }

                        if (NameExists == false)
                        {
                            SendError("Could not locate task " + string_array[2] + ".");
                        }
                    }
                    if (checker_array[1] == "ADJ")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        bool NameExists = false;
                        bool IsNumeric = false;
                        int x = 0;
                        if (string_array[3].All(char.IsDigit))
                        {
                            x = Convert.ToInt32(string_array[3]);
                            IsNumeric = true;
                        }
                        else { SendError("Incorrect syntax for task asjust command. Must provide new number for task timer in seconds."); }
                        foreach (TimedFunction timedFunction in ActiveFunctions)
                        {
                            if (string_array[2] == timedFunction.Name)
                            {
                                if (IsNumeric == true)
                                {
                                    timedFunction.Interval = x;
                                    NameExists = true;
                                    break;
                                }
                                break;
                            }
                        }

                        if (NameExists == false)
                        {
                            SendError("Could not locate task " + string_array[2] + ".");
                        }
                    }
                }

                if (Input.StartsWith("SLEEP "))
                {
                    string[] checkery_array = Input.Split(' ', 2);
                    bool isValid = false;
                    bool isDigit = false;
                    string sleepTime = ConvertAllVariable(checkery_array[1], true);
                    if (checkery_array.Length == 2)
                    {
                        isValid = true;
                        if (sleepTime.All(char.IsDigit))
                        {
                            isDigit = true;
                            try
                            {
                                if (isDigit == true && isValid == true)
                                {
                                    int _x = Convert.ToInt32(sleepTime);
                                    Thread.Sleep(_x);
                                }
                            }
                            catch
                            {
                                SendError("Incorrect syntax for sleep.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for sleep. Must provide all digits.");
                        }
                    }
                    else
                    {
                        SendError("Incorrect syntax for sleep.");
                    }

                }

                if (Input.StartsWith("COLOR "))
                {
                    string[] string_array = Input.Split(' ', 3);
                    if (string_array.Length == 3)
                    {
                        string aa = ConvertAllVariable(string_array[1], true);
                        string bb = ConvertAllVariable(string_array[2], true);
                        int temp1 = Convert.ToInt32(aa);
                        int temp2 = Convert.ToInt32(bb);

                        if (temp1 < 16 && temp2 < 16)
                        {
                            if (aa.All(char.IsDigit) && bb.All(char.IsDigit))
                            {
                                NewConsoleColor(temp1, temp2);
                            }
                            else
                            {
                                SendError("Values must be all digits.");
                            }
                        }
                        else
                        {
                            SendError("Value must be less than or equal to 15.");
                        }
                    }
                    else if (string_array.Length == 2 && string_array[1] == "DEFAULT")
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                        textColor = ConsoleColor.White;
                    }
                    else
                    {
                        SendError("Incorrect syntax for command COLOR.");
                    }
                }

                if (Input.StartsWith("FILESYSTEM "))
                {
                    ///<summary>
                    /// FILESYSTEM READ {INT/"ALL"} {FILEPATH} {VARIABLE}
                    ///     > reads line number/all lines from filepath into variable
                    /// FILESYSTEM WRITE {FILEPATH} {DATA}
                    ///     > writes data to filepath
                    /// FILESYSTEM DEL {FILEPATH}
                    ///     > deletes specified file
                    /// FILESYSTEM MKDIR {FILEPATH}
                    ///     > create new directory
                    /// FILESYSTEM RMDIR {FILEPATH}
                    ///     > removes specified directory + all sub directories and files!
                    /// FILESYSTEM MVDIR {FILEPATH} {DESTINATION FILEPATH}
                    ///     > moves the specified directory to the destination path
                    /// </summary>

                    string[] checker = Input.Split(' ', 3);
                    bool OneValid = false;

                    if (checker[1] == "WRITE")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        if (string_array.Length == 4)
                        {
                            OneValid = true;
                            string placeholder = ConvertAllVariable(string_array[3], true);
                            string placeholder2 = ConvertList(placeholder);
                            //string_array[3] = placeholder;
                            GPFileSystem.WriteFile(string_array[2], placeholder2);
                        }
                    }
                    if (checker[1] == "READ")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            OneValid = true;
                            bool VariableExists = false;
                            bool SecondParameter = false;
                            bool AllLines = false;
                            int x = 0;

                            if (string_array[2].All(char.IsDigit))
                            {
                                SecondParameter = true;
                                x = Convert.ToInt32(string_array[2]);
                            }
                            if (string_array[2] == "ALL")
                            {
                                SecondParameter = true;
                                AllLines = true;
                            }

                            // It is not a digit or ALL so we will check for an int
                            if (SecondParameter == false && AllLines == false)
                            {
                                if (string_array[2].StartsWith("_"))
                                {

                                    string temp = string_array[2];
                                    StringBuilder second_temp = new StringBuilder("");
                                    second_temp.Append(temp.Remove(0, 1));

                                    foreach (Variable var in ActiveVariables)
                                    {
                                        if (var.VarName == second_temp.ToString() && var.IsInteger == true)
                                        {
                                            x = var.Number;
                                            SecondParameter = true;
                                            break;
                                        }
                                        if (var.VarName == "_" + second_temp && var.IsInteger == false)
                                        {
                                            SendError("Incompatiable variable type. Must be integer.");
                                            break;
                                        }
                                    }
                                }
                            }
                            if (SecondParameter == true && AllLines == false)
                            {
                                placeholder = GPFileSystem.ReadFile(string_array[3], x, false);
                            }
                            if (SecondParameter == true && AllLines == true)
                            {
                                placeholder = GPFileSystem.ReadFile(string_array[3], 0, true);
                            }

                            if (SecondParameter == true && placeholder != "")
                            {
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (string_array[4] == var.VarName)
                                    {
                                        var.Message = placeholder;
                                        VariableExists = true;
                                        break;
                                    }
                                }
                                if (VariableExists == false)
                                {
                                    Variable newVar = new Variable(true, placeholder, string_array[4]);
                                    newVar.IsInteger = false;
                                    newVar.IsString = true;
                                    ActiveVariables.Add(newVar);
                                }
                            }
                            else
                            {
                                SendError("Incorrect syntax for command FILESYSTEM READ.");
                            }
                        }

                    }
                    if (checker[1] == "DEL")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            OneValid = true;
                            GPFileSystem.DeleteFile(string_array[2]);
                        }
                    }
                    if (checker[1] == "MKDIR")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            OneValid = true;
                            GPFileSystem.CreateDirectory(string_array[2]);
                        }
                    }
                    if (checker[1] == "RMDIR")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            OneValid = true;
                            GPFileSystem.DeleteDirectory(string_array[2]);
                        }
                    }
                    if (checker[1] == "MVDIR")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        if (string_array.Length == 4)
                        {
                            OneValid = true;
                            GPFileSystem.MoveDirectory(string_array[2], string_array[3]);
                        }
                    }
                    if (checker[1] == "GETSIZE")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        if (string_array.Length == 4)
                        {
                            string varName = string_array[2];
                            string Path = string_array[3];
                            bool varFound = false;
                            if (FileSystem.FileExists(Path))
                            {
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (var.VarName == varName)
                                    {
                                        varFound = true;
                                        if (var.IsInteger == true)
                                        {
                                            var.Message = GPFileSystem.ReturnFileSize(Path);
                                            var.Number = Convert.ToInt32(var.Message);
                                        }
                                        if (var.IsString == true)
                                        {
                                            var.Message = GPFileSystem.ReturnFileSize(Path);
                                            var.Number = Convert.ToInt32(var.Message);
                                        }
                                        OneValid = true;
                                        break;
                                    }

                                }
                                if (varFound == false)
                                {
                                    SendError($"Unable to locate output variable {varName}.");
                                }
                            }
                            else
                            {
                                SendError($"Could not locate file {Path}.");
                            }
                        }
                        else
                        {
                            SendError($"Incorrect syntax for command FILESYSTEM.");
                        }
                    }

                    if (checker[1] == "ENCRYPT")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        if (string_array.Length == 3)
                        {
                            OneValid = true;
                            GPFileSystem.EncryptFile(string_array[2]);
                        }
                    }

                    if (OneValid == false)
                    {
                        SendError("Incorrect syntax for command FILESYSTEM.");
                    }
                }

                if (Input.StartsWith("IF "))
                {
                    // if variable = value then [COMMAND]
                    string VarValue = "";
                    string SecondVarVal = "";
                    bool IsValid = true;
                    bool IsString = false;
                    bool VarExist = false;
                    bool SecondVar = false;
                    bool ValuesMatch = false;
                    bool IsVar = false;
                    int Equals = 0;

                    string[] string_array = Input.Split(' ', 6);
                    if (string_array.Length == 6)
                    {
                        if (string_array[3].StartsWith("_"))
                        {
                            string holder = string_array[3].Remove(0, 1);
                            string_array[3] = holder;
                            IsVar = true;
                        }
                        switch (string_array[2])
                        {
                            case "=":
                                Equals = 1;
                                break;
                            case "!=":
                                Equals = 2;
                                break;
                            case ">":
                                Equals = 3;
                                break;
                            case "<":
                                Equals = 4;
                                break;
                            case ">=":
                                Equals = 5;
                                break;
                            case "<=":
                                Equals = 6;
                                break;
                        }

                        foreach (Variable var in ActiveVariables)
                        {
                            if (var.VarName == string_array[1])
                            {
                                VarExist = true;
                                VarValue = var.Message;
                                if (var.IsString == true)
                                {
                                    IsString = true;
                                }
                                break;
                            }

                        }
                        if (VarExist == true)
                        {
                            if (IsString == true)
                            {

                            }
                            if (IsVar == true)
                            {
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (var.VarName == string_array[3]) { SecondVar = true; SecondVarVal = var.Message; break; }
                                }
                                if (SecondVar == true)
                                {
                                    if (Equals == 0) { SendError("Incorrect syntax for IF condition, must use = or !="); }
                                    if (Equals == 1)
                                    {
                                        if (VarValue == SecondVarVal) { ValuesMatch = true; }
                                    }
                                    if (Equals == 2)
                                    {
                                        if (VarValue != SecondVarVal) { ValuesMatch = true; }
                                    }
                                    if (Equals > 2 && Equals < 7)
                                    {
                                        if ((VarValue.All(Char.IsDigit)) && SecondVarVal.All(Char.IsDigit))
                                        {
                                            int a = Convert.ToInt32(VarValue);
                                            int b = Convert.ToInt32(SecondVarVal);
                                            switch (Equals)
                                            {
                                                case 3:
                                                    if (a > b) { ValuesMatch = true; }
                                                    break;
                                                case 4:
                                                    if (a < b) { ValuesMatch = true; }
                                                    break;
                                                case 5:
                                                    if (a >= b) { ValuesMatch = true; }
                                                    break;
                                                case 6:
                                                    if (a <= b) { ValuesMatch = true; }
                                                    break;
                                            }

                                        }
                                        else { SendError("Invalud syntax for IF conditions when using mathematical parameter."); }
                                    }

                                }
                                else
                                {
                                    SendError("Could not locate variable " + string_array[3] + ".");
                                }
                            }
                            else
                            {
                                if (Equals == 0) { SendError("Incorrect syntax for IF condition."); }
                                if (Equals == 1)
                                {
                                    if (VarValue == string_array[3]) { ValuesMatch = true; }
                                }
                                if (Equals == 2)
                                {
                                    if (VarValue != string_array[3]) { ValuesMatch = true; }
                                }

                                if (Equals > 2 && Equals < 7)
                                {
                                    if ((VarValue.All(Char.IsDigit)) && string_array[3].All(Char.IsDigit))
                                    {
                                        int a = Convert.ToInt32(VarValue);
                                        int b = Convert.ToInt32(string_array[3]);
                                        switch (Equals)
                                        {
                                            case 3:
                                                if (a > b) { ValuesMatch = true; }
                                                break;
                                            case 4:
                                                if (a < b) { ValuesMatch = true; }
                                                break;
                                            case 5:
                                                if (a >= b) { ValuesMatch = true; }
                                                break;
                                            case 6:
                                                if (a <= b) { ValuesMatch = true; }
                                                break;
                                        }

                                    }
                                    else { SendError("Invalud syntax for IF conditions when using mathematical parameter."); }
                                }
                            }

                            if (ValuesMatch == true)
                            {
                                if (string_array[4] == "THEN")
                                {
                                    Parse(string_array[5]);
                                }
                                else
                                {
                                    SendError("Incorrect syntax for IF condition, must use THEN following comparison.");
                                }
                            }
                        }
                        else
                        {
                            SendError("Could not locate variable " + string_array[1] + ".");
                        }
                    }
                    else { SendError("Incorrect syntax for IF statement."); }
                }

                if (Input.StartsWith("PRINT "))
                {
                    Input.Remove(0, 6);
                    StringBuilder strng = new StringBuilder("");
                    string Placeholder = ConvertAllVariable(Input, false);
                    string Placeholder2 = ConvertList(Placeholder);
                    Input = Placeholder2;
                    char[] MessageToPrint = Input.ToCharArray();
                    int x = MessageToPrint.Length;
                    int y = 6;

                    while (y < x)
                    {
                        bool IsValid = false;
                        bool PrintChar = true;
                        bool once = false;
                        try
                        {
                            if (MessageToPrint[y] == '{' && MessageToPrint[y + 2] == '}')
                            {

                                switch (MessageToPrint[y + 1])
                                {
                                    case 'L':
                                        Console.WriteLine();
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                    case 'T':
                                        string right_time;
                                        right_time = DateTime.Now.ToString("h:mm tt");
                                        Console.Write(right_time);
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                    case 'D':
                                        string right_date;
                                        right_date = DateTime.Now.ToString("MM/dd/yyyy");
                                        Console.Write(right_date);
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                    case 'R':
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                    case 'B':
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                    case 'Y':
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                    case 'W':
                                        Console.ForegroundColor = ConsoleColor.White;
                                        y++;
                                        y++;
                                        y++;
                                        IsValid = true;
                                        break;
                                }
                                if (IsValid == false)
                                {
                                    y++;
                                    PrintChar = false;
                                }
                            }
                        }
                        catch { }
                        if (IsValid == false && PrintChar == true)
                        {
                            if (MessageToPrint[y] != '{' && MessageToPrint[y] != '}')
                            {
                                Console.Write(MessageToPrint[y]);
                            }
                            y++;
                        }
                    }
                    Console.WriteLine();
                    Console.ForegroundColor = textColor;
                }

                if (Input.StartsWith("TERMINATE"))
                {
                    if (Input.Length == 9) { Environment.Exit(0); } else { SendError("TERMINATE command can not contain any trailing characters."); }
                }

                if (Input.StartsWith("GOTO "))
                {
                    if (RunningScript == true)
                    {
                        string[] string_array = Input.Split(' ', 2);
                        if (string_array.Length == 2)
                        {
                            if (!string_array[1].All(char.IsDigit))
                            {
                                SendError("GOTO must point to specific line number.");
                            }
                            else
                            {
                                int x = Convert.ToInt32(string_array[1]);
                                if (x < MaxLines)
                                {
                                    CurrentLine = x;
                                }
                                else
                                {
                                    SendError("Invalid line number.");
                                }
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for GOTO command.");
                        }
                    }
                    else { SendError("Must be executing script to use GOTO command."); }
                }

                if (Input.StartsWith("CLEAR"))
                {
                    if (Input.Length == 5) { Console.Clear(); } else { SendError("CLEAR command can not contain any trailing characters."); }
                }

                if (Input.StartsWith("NEW "))
                {
                    bool IsValid = true;
                    bool IsString = false; ;
                    string varname = "";
                    string Svalue = "";
                    int Ivalue = 0;
                    string[] var_array = Input.Split(' ', 4);
                    if (var_array.Length == 4)
                    {
                        if (var_array.Length == 4)
                        {
                            if (var_array[1] == "STR" || var_array[1] == "INT")
                            {
                                if (var_array[1] == "STR") { IsString = true; Svalue = var_array[3]; } else if (var_array[1] == "INT") { IsString = false; }
                                if (var_array[2].All(Char.IsLetterOrDigit))
                                {
                                    varname = var_array[2];
                                }
                                else
                                {
                                    SendError("New variable name may only contain letters and number.");
                                    IsValid = false;
                                }

                                if (IsString == false)
                                {
                                    try
                                    {
                                        if (var_array[3].All(Char.IsDigit) && var_array[3] != null)
                                        {
                                            Ivalue = Convert.ToInt32(var_array[3]);
                                        }
                                        else
                                        {
                                            SendError("Integers may only contain numbers.");
                                            IsValid = false;
                                        }
                                    }
                                    catch
                                    {
                                        SendError("Incorrect syntax for new variable declaration.");
                                    }
                                }
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (var.VarName == varname) { IsValid = false; SendError("Variable named " + varname + " already exists."); }
                                }
                                switch (varname)
                                {
                                    case "L":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                    case "T":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                    case "D":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                    case "R":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                    case "B":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                    case "Y":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                    case "W":
                                        SendError("Invalid variable name " + varname + ". Reserved variable name.");
                                        IsValid = false;
                                        break;
                                }
                                if (IsValid == true)
                                {
                                    switch (IsString)
                                    {
                                        case true:
                                            Variable variable = new Variable(IsString, Svalue, varname);
                                            variable.IsString = true;
                                            variable.IsInteger = false;
                                            ActiveVariables.Add(variable);
                                            break;
                                        case false:
                                            Variable variable1 = new Variable(IsString, Ivalue.ToString(), varname);
                                            variable1.IsString = false;
                                            variable1.IsInteger = true;
                                            ActiveVariables.Add(variable1);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                SendError("New variable must be declared STR (string) or INT (integer).");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for new variable declaration.");
                        }
                    }
                    else { SendError("Incorrect syntax for new variable declaration."); }
                } // Need to address bug issue with NEW INT NAME [blank]

                if (Input.StartsWith("SET "))
                {
                    bool IsString = false;
                    bool WasFound = false;
                    bool IsValid = false;
                    string[] string_array = Input.Split(' ', 4);
                    if (string_array.Length == 4)
                    {
                        if (string_array[2] == "=") { IsValid = true; } else { SendError("Incorrect syntax for SET."); }
                        foreach (Variable var in ActiveVariables)
                        {
                            if (string_array[1] == var.VarName)
                            {
                                WasFound = true;
                                if (var.IsInteger == true && string_array[3].All(char.IsDigit)) { var.Message = string_array[3]; var.Number = Convert.ToInt32(string_array[3]); IsValid = true; break; }
                                if (var.IsInteger == true && !string_array[3].All(char.IsDigit)) { SendError("Incompatible value for integer."); break; }
                                if (var.IsString == true) { var.Message = ConvertAllVariable(string_array[3], true); IsValid = true; break; }
                                if (IsValid == false)
                                {
                                    SendError("Incorrect syntax to set value of " + var.VarName + ".");
                                }
                            }

                        }
                        if (WasFound == false) { SendError("Could not locate variable " + string_array[1] + "."); }
                    }
                    else { SendError("Incorrect syntax for command SET."); }

                }

                if (Input.StartsWith("WHILE "))
                {
                    int x = 0;
                    foreach (Variable var in ActiveVariables)
                    {
                        if (var.VarName == "BUFFERDELAY")
                        {
                            x = var.Number;
                            break;
                        }
                    }

                    string[] check_array = Input.Split(' ', 5);
                    if (check_array.Length == 5)
                    {
                        string firstVar = check_array[1];
                        bool firstValid = false;
                        string secondVar = check_array[3];
                        bool secondValid = false;
                        int symbol = 0; // 0 is null, 1 is equal to, 2 is greater than or equal to, 3 is less than or equal to, and 4 is NOT equal to
                        bool symbolValid = false;
                        if (check_array[4].StartsWith("{") && (check_array[4].EndsWith("}")))
                        {
                            check_array[4] = check_array[4].TrimStart('{');
                            check_array[4] = check_array[4].TrimEnd('}');
                        }
                        else
                        {
                            SendError("Incorrect syntax for While loop. All commands should be encapsulated in brackets.");
                        }
                        string _cmdlist = check_array[4];
                        string[] commandList = _cmdlist.Split(',');
                        bool conditionMet = false;

                        switch (check_array[2])
                        {
                            case "=":
                                symbol = 1;
                                symbolValid = true;
                                break;
                            case "!=":
                                symbol = 4;
                                symbolValid = true;
                                break;
                            case ">=":
                                symbol = 2;
                                symbolValid = true;
                                break;
                            case "<=":
                                symbol = 3;
                                symbolValid = true;
                                break;
                        } // verify correct comparative symbol is being used first

                        if (symbol != 0) // proceed to check first variable and second variable and that they do exist
                        {
                            foreach (Variable _var1 in ActiveVariables)
                            {
                                if (_var1.VarName == firstVar)
                                {
                                    firstValid = true;

                                    foreach (Variable _var2 in ActiveVariables)
                                    {
                                        if (_var2.VarName == secondVar)
                                        {
                                            secondValid = true;
                                            // both variables have been positively identified - they exist

                                            switch (symbol)
                                            {
                                                case 1: // equal to
                                                    conditionMet = true;
                                                    try
                                                    {
                                                        while (_var1.Message == _var2.Message || _var1.Number == _var2.Number)
                                                        {
                                                            foreach (string cmd in commandList)
                                                            {
                                                                Parse(cmd);
                                                                Thread.Sleep(x);
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        SendError("Failed to execute list of commands.");
                                                    }
                                                    break;
                                                case 2: // greater than, equal to
                                                    conditionMet = true;
                                                    if (_var1.IsInteger == true && _var2.IsInteger == true)
                                                    {
                                                        try
                                                        {
                                                            while (_var1.Number >= _var2.Number)
                                                            {
                                                                foreach (string cmd in commandList)
                                                                {
                                                                    Parse(cmd);
                                                                    Thread.Sleep(x);
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            SendError("Failed to execute list of commands.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        SendError("Both variables must be integers to use a comparative symbol such as >= or <=");
                                                    }
                                                    break;
                                                case 3: // less than, equal to
                                                    conditionMet = true;
                                                    if (_var1.IsInteger == true && _var2.IsInteger == true)
                                                    {
                                                        try
                                                        {
                                                            while (_var1.Number <= _var2.Number)
                                                            {
                                                                foreach (string cmd in commandList)
                                                                {
                                                                    Parse(cmd);
                                                                    Thread.Sleep(x);
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            SendError("Failed to execute list of commands.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        SendError("Both variables must be integers to use a comparative symbol such as >= or <=");
                                                    }
                                                    break;
                                                case 4: // NOT equal to
                                                    conditionMet = true;
                                                    try
                                                    {
                                                        while (_var1.Message != _var2.Message || _var1.Number != _var1.Number)
                                                        {
                                                            foreach (string cmd in commandList)
                                                            {
                                                                Parse(cmd);
                                                                Thread.Sleep(x);
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        SendError("Failed to execute list of commands.");
                                                    }
                                                    break;
                                            }
                                            break;
                                        }
                                    }
                                    if (secondValid == false)
                                    {
                                        SendError($"Could not locate second variable {secondVar}.");
                                    }
                                    break;
                                }
                            }
                            if (firstValid == false)
                            {
                                SendError($"Could not locate first variable {firstVar}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for While loop. Must use = != >= or <=");
                        }
                    }
                    else
                    {
                        SendError("Incorrect syntax for While loop.");
                    }
                }

                if (Input.StartsWith("STRING "))
                {
                    string[] checker_array = Input.Split(' ', 4);
                    if (checker_array[1] == "APPEND" || checker_array[1] == "_APPEND")
                    {
                        if (checker_array.Length == 4)
                        {
                            string[] string_array = Input.Split(' ', 4);
                            string variableName = string_array[2];
                            bool varFound = false;
                            bool varString = false;
                            foreach (Variable var in ActiveVariables)
                            {
                                if (var.VarName == variableName)
                                {
                                    varFound = true;
                                    if (var.IsString == true)
                                    {
                                        varString = true;
                                        string a1 = ConvertAllVariable(string_array[3], true);
                                        string a2 = ConvertList(a1);
                                        StringBuilder strbuild = new StringBuilder(var.Message);
                                        if (string_array[1] == "APPEND")
                                        {
                                            strbuild.Append(string_array[3]);
                                        }
                                        if (string_array[1] == "_APPEND")
                                        {
                                            strbuild.Append(a2);
                                        }

                                        var.Message = strbuild.ToString();
                                    }
                                    else
                                    {
                                        SendError($"Variable {variableName} is not a string.");
                                    }
                                }
                            }
                            if (varFound == false)
                            {
                                SendError($"Could not locate variable {variableName}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for appending string.");
                        }
                    }
                    if (checker_array[1] == "REPLACE")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        string[] set_get_array = string_array[3].Split("||", 2);
                        if (string_array.Length == 4 && set_get_array.Length == 2)
                        {
                            string variableName = string_array[2];
                            bool varFound = false;
                            bool varString = false;
                            foreach (Variable var in ActiveVariables)
                            {
                                if (var.VarName == variableName)
                                {
                                    varFound = true;
                                    if (var.IsString == true)
                                    {
                                        varString = true;
                                        StringBuilder strbuild = new StringBuilder(var.Message);
                                        strbuild.Replace($"{set_get_array[0]}", $"{set_get_array[1]}");
                                        var.Message = strbuild.ToString();
                                    }
                                    else
                                    {
                                        SendError($"Variable {variableName} is not a string.");
                                    }
                                }
                            }
                            if (varFound == false)
                            {
                                SendError($"Could not locate variable {variableName}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for replacing string values.");
                        }

                    }
                    if (checker_array[1] == "REMOVE")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        if (string_array.Length == 4)
                        {
                            string variableName = string_array[2];
                            bool varFound = false;
                            bool varString = false;
                            foreach (Variable var in ActiveVariables)
                            {
                                if (var.VarName == variableName)
                                {
                                    varFound = true;
                                    if (var.IsString == true)
                                    {
                                        varString = true;
                                        StringBuilder strbuild = new StringBuilder(var.Message);
                                        strbuild.Replace($"{string_array[3]}", "");
                                        var.Message = strbuild.ToString();
                                    }
                                    else
                                    {
                                        SendError($"Variable {variableName} is not a string.");
                                    }
                                }
                            }
                            if (varFound == false)
                            {
                                SendError($"Could not locate variable {variableName}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for removing values from string.");
                        }
                    }
                    if (checker_array[1] == "CONTAINS")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            string variableName = string_array[2];
                            string outputVariable = string_array[3];
                            bool varFound = false;
                            bool outputFound = false;
                            bool varString = false;
                            foreach (Variable var in ActiveVariables)
                            {
                                if (var.VarName == variableName)
                                {
                                    varFound = true;
                                    if (var.IsString == true)
                                    {
                                        if (outputVariable.StartsWith("BOOL_") || outputVariable.StartsWith("COUNT_"))
                                        {
                                            string _outputVar = "";
                                            int action = 0;
                                            if (outputVariable.StartsWith("BOOL_")) { _outputVar = outputVariable.Remove(0, 5); action = 1; }
                                            if (outputVariable.StartsWith("COUNT_")) { _outputVar = outputVariable.Remove(0, 6); action = 2; }
                                            bool foundOutputVar = false;
                                            foreach (Variable outputVar in ActiveVariables)
                                            {
                                                if (outputVar.VarName == _outputVar)
                                                {

                                                    foundOutputVar = true;
                                                    if (action == 1)
                                                    {
                                                        if (var.Message.Contains(string_array[4]))
                                                        {
                                                            outputVar.Number = 1;
                                                            outputVar.Message = "1";
                                                        }
                                                        else
                                                        {
                                                            outputVar.Number = 0;
                                                            outputVar.Message = "0";
                                                        }
                                                    }
                                                    else
                                                    if (action == 2)
                                                    {
                                                        int qty = Regex.Matches(var.Message, string_array[4]).Count();
                                                        outputVar.Number = qty;
                                                        outputVar.Message = qty.ToString();
                                                    }
                                                    break;
                                                }
                                            }
                                            if (foundOutputVar == false)
                                            {
                                                SendError($"Could not locate variable {_outputVar}. Must have an existing variable to output to.");
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            SendError("Incorrect syntax. BOOL_ or COUNT_ must precede your output variable.");
                                        }


                                    }
                                    else
                                    {
                                        SendError($"Variable {variableName} is not a string.");
                                    }
                                }
                            }
                            if (varFound == false)
                            {
                                SendError($"Could not locate variable {variableName}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for finding value within string.");
                        }
                    }
                }

                if (Input.StartsWith("RUN "))
                {
                    string[] string_array = Input.Split(' ', 2);
                    if (string_array.Length == 2)
                    { Initialize(string_array[1]); }
                    else { SendError("Incorrect syntax for RUN command."); }
                }

                if (Input.StartsWith("ADD1 "))
                {
                    bool FoundName = false;

                    string[] string_array = Input.Split(' ', 2);
                    if (string_array.Length == 2)
                    {
                        foreach (Variable var in ActiveVariables)
                        {
                            if (var.VarName == string_array[1])
                            {
                                FoundName = true;
                                if (var.IsInteger == true) { var.Number++; var.Message = var.Number.ToString(); break; }
                                if (var.IsInteger == false) { SendError("Can only increment integers."); break; }
                            }
                        }
                        if (FoundName == false) { SendError("Could not located variable " + string_array[1] + "."); }
                    }
                    else { SendError("Incorrect syntax for ADD1 command."); }
                }

                if (Input.StartsWith("SUB1 "))
                {
                    bool FoundName = false;

                    string[] string_array = Input.Split(' ', 2);
                    if (string_array.Length == 2)
                    {
                        foreach (Variable var in ActiveVariables)
                        {
                            if (var.VarName == string_array[1])
                            {
                                FoundName = true;
                                if (var.IsInteger == true) { var.Number--; var.Message = var.Number.ToString(); break; }
                                if (var.IsInteger == false) { SendError("Can only increment integers."); break; }
                            }
                        }
                        if (FoundName == false) { SendError("Could not located variable " + string_array[1] + "."); }
                    }
                    else { SendError("Incorrect syntax for ADD1 command."); }
                }

                if (Input.StartsWith("MATH "))
                {
                    string[] checker_array = Input.Split(' ', 5);
                    if (checker_array[1] == "+")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            string firstNumber = string_array[2];
                            bool validFirstNumber = false;
                            string secondNumber = string_array[3];
                            bool validSecondNumber = false;
                            string outputVariable = string_array[4];
                            bool validOutputVariable = false;
                            foreach (Variable firstVar in ActiveVariables)
                            {
                                if (firstVar.VarName == firstNumber)
                                {
                                    validFirstNumber = true;
                                    if (firstVar.IsInteger == true)
                                    {

                                        foreach (Variable secondVar in ActiveVariables)
                                        {
                                            if (secondVar.VarName == secondNumber)
                                            {
                                                validSecondNumber = true;
                                                if (secondVar.IsInteger == true)
                                                {
                                                    foreach (Variable outVar in ActiveVariables)
                                                    {
                                                        if (outVar.VarName == outputVariable)
                                                        {
                                                            validOutputVariable = true;
                                                            outVar.Number = (firstVar.Number + secondVar.Number);
                                                            outVar.Message = outVar.Number.ToString();
                                                            break;
                                                        }
                                                    }
                                                    if (validOutputVariable == false)
                                                    {
                                                        SendError($"Unable to locate output variable {outputVariable}.");
                                                    }

                                                }
                                                else
                                                {
                                                    SendError($"Variable {secondNumber} is not an integer.");
                                                }
                                            }

                                        }
                                        if (validSecondNumber == false)
                                        {
                                            SendError($"Unable to locate second integer {secondNumber}.");
                                        }
                                    }
                                    else
                                    {
                                        SendError($"Variable {firstNumber} is not an integer.");
                                    }
                                    break;
                                }

                            }
                            if (validFirstNumber == false)
                            {
                                SendError($"Unable to locate first integer {firstNumber}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for mathematical operation.");
                        }
                    }
                    if (checker_array[1] == "-")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            string firstNumber = string_array[2];
                            bool validFirstNumber = false;
                            string secondNumber = string_array[3];
                            bool validSecondNumber = false;
                            string outputVariable = string_array[4];
                            bool validOutputVariable = false;
                            foreach (Variable firstVar in ActiveVariables)
                            {
                                if (firstVar.VarName == firstNumber)
                                {
                                    validFirstNumber = true;
                                    if (firstVar.IsInteger == true)
                                    {

                                        foreach (Variable secondVar in ActiveVariables)
                                        {
                                            if (secondVar.VarName == secondNumber)
                                            {
                                                validSecondNumber = true;
                                                if (secondVar.IsInteger == true)
                                                {
                                                    foreach (Variable outVar in ActiveVariables)
                                                    {
                                                        if (outVar.VarName == outputVariable)
                                                        {
                                                            validOutputVariable = true;
                                                            outVar.Number = (firstVar.Number - secondVar.Number);
                                                            outVar.Message = outVar.Number.ToString();
                                                            break;
                                                        }
                                                    }
                                                    if (validOutputVariable == false)
                                                    {
                                                        SendError($"Unable to locate output variable {outputVariable}.");
                                                    }

                                                }
                                                else
                                                {
                                                    SendError($"Variable {secondNumber} is not an integer.");
                                                }
                                            }

                                        }
                                        if (validSecondNumber == false)
                                        {
                                            SendError($"Unable to locate second integer {secondNumber}.");
                                        }
                                    }
                                    else
                                    {
                                        SendError($"Variable {firstNumber} is not an integer.");
                                    }
                                    break;
                                }

                            }
                            if (validFirstNumber == false)
                            {
                                SendError($"Unable to locate first integer {firstNumber}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for mathematical operation.");
                        }
                    }
                    if (checker_array[1] == "*")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            string firstNumber = string_array[2];
                            bool validFirstNumber = false;
                            string secondNumber = string_array[3];
                            bool validSecondNumber = false;
                            string outputVariable = string_array[4];
                            bool validOutputVariable = false;
                            foreach (Variable firstVar in ActiveVariables)
                            {
                                if (firstVar.VarName == firstNumber)
                                {
                                    validFirstNumber = true;
                                    if (firstVar.IsInteger == true)
                                    {

                                        foreach (Variable secondVar in ActiveVariables)
                                        {
                                            if (secondVar.VarName == secondNumber)
                                            {
                                                validSecondNumber = true;
                                                if (secondVar.IsInteger == true)
                                                {
                                                    foreach (Variable outVar in ActiveVariables)
                                                    {
                                                        if (outVar.VarName == outputVariable)
                                                        {
                                                            validOutputVariable = true;
                                                            outVar.Number = (firstVar.Number * secondVar.Number);
                                                            outVar.Message = outVar.Number.ToString();
                                                            break;
                                                        }
                                                    }
                                                    if (validOutputVariable == false)
                                                    {
                                                        SendError($"Unable to locate output variable {outputVariable}.");
                                                    }

                                                }
                                                else
                                                {
                                                    SendError($"Variable {secondNumber} is not an integer.");
                                                }
                                            }

                                        }
                                        if (validSecondNumber == false)
                                        {
                                            SendError($"Unable to locate second integer {secondNumber}.");
                                        }
                                    }
                                    else
                                    {
                                        SendError($"Variable {firstNumber} is not an integer.");
                                    }
                                    break;
                                }

                            }
                            if (validFirstNumber == false)
                            {
                                SendError($"Unable to locate first integer {firstNumber}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for mathematical operation.");
                        }
                    }
                    if (checker_array[1] == "/")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            string firstNumber = string_array[2];
                            bool validFirstNumber = false;
                            string secondNumber = string_array[3];
                            bool validSecondNumber = false;
                            string outputVariable = string_array[4];
                            bool validOutputVariable = false;
                            foreach (Variable firstVar in ActiveVariables)
                            {
                                if (firstVar.VarName == firstNumber)
                                {
                                    validFirstNumber = true;
                                    if (firstVar.IsInteger == true)
                                    {

                                        foreach (Variable secondVar in ActiveVariables)
                                        {
                                            if (secondVar.VarName == secondNumber)
                                            {
                                                validSecondNumber = true;
                                                if (secondVar.IsInteger == true)
                                                {
                                                    foreach (Variable outVar in ActiveVariables)
                                                    {
                                                        if (outVar.VarName == outputVariable)
                                                        {
                                                            validOutputVariable = true;
                                                            Decimal a = Convert.ToDecimal(firstVar.Number);
                                                            Decimal b = Convert.ToDecimal(secondVar.Number);
                                                            Decimal c = (a / b);
                                                            outVar.Number = (firstVar.Number / secondVar.Number);
                                                            outVar.Message = Convert.ToString(c);
                                                            break;
                                                        }
                                                    }
                                                    if (validOutputVariable == false)
                                                    {
                                                        SendError($"Unable to locate output variable {outputVariable}.");
                                                    }

                                                }
                                                else
                                                {
                                                    SendError($"Variable {secondNumber} is not an integer.");
                                                }
                                            }

                                        }
                                        if (validSecondNumber == false)
                                        {
                                            SendError($"Unable to locate second integer {secondNumber}.");
                                        }
                                    }
                                    else
                                    {
                                        SendError($"Variable {firstNumber} is not an integer.");
                                    }
                                    break;
                                }

                            }
                            if (validFirstNumber == false)
                            {
                                SendError($"Unable to locate first integer {firstNumber}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for mathematical operation.");
                        }
                    }
                    if (checker_array[1] == "SQROOT")
                    {
                        string[] string_array = Input.Split(' ', 4);
                        string firstVariable = string_array[2];
                        string outputVariable = string_array[3];
                        bool foundFirst = false;
                        bool foundOutput = false;

                        if (string_array.Length == 4)
                        {
                            foreach (Variable firstVar in ActiveVariables)
                            {
                                if (firstVar.VarName == firstVariable)
                                {
                                    foundFirst = true;
                                    if (firstVar.IsInteger == true)
                                    {
                                        foreach (Variable outputVar in ActiveVariables)
                                        {
                                            if (outputVar.VarName == outputVariable)
                                            {
                                                foundOutput = true;
                                                Double a = Convert.ToDouble(firstVar.Number);
                                                Double b = Math.Sqrt(a);
                                                outputVar.Message = Convert.ToString(b);
                                                outputVar.Number = Convert.ToInt32(b);
                                                break;
                                            }
                                        }
                                        if (foundOutput == false)
                                        {
                                            SendError($"Unable to locate output variable {outputVariable}.");
                                        }
                                    }
                                    else
                                    {
                                        SendError($"Variable {firstVariable} is not an integer.");
                                    }
                                    break;
                                }
                            }
                            if (foundFirst == false)
                            {
                                SendError($"Unable to locate variable {firstVariable}.");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for finding square root.");
                        }
                    }
                    if (checker_array[1] == "COMP")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        if (string_array.Length == 5)
                        {
                            string varA = string_array[2];
                            string varB = string_array[3];
                            string outputVar = string_array[4];
                            bool foundA = false;
                            bool foundB = false;
                            bool foundOutput = false;

                            foreach (Variable var1 in ActiveVariables)
                            {
                                if (var1.VarName == varA)
                                {
                                    foundA = true;
                                    if (var1.IsInteger == true)
                                    {
                                        foreach (Variable var2 in ActiveVariables)
                                        {
                                            if (var2.VarName == varB)
                                            {
                                                foundB = true;
                                                if (var2.IsInteger == true)
                                                {
                                                    foreach (Variable outvar in ActiveVariables)
                                                    {
                                                        if (outvar.VarName == outputVar)
                                                        {
                                                            foundOutput = true;

                                                            if (var1.Number > var2.Number)
                                                            {
                                                                outvar.Number = var1.Number;
                                                                outvar.Message = var1.Number.ToString();
                                                            }
                                                            else if (var1.Number < var2.Number)
                                                            {
                                                                outvar.Number = var2.Number;
                                                                outvar.Message = var2.Number.ToString();
                                                            }
                                                            else
                                                            {
                                                                outvar.Number = var1.Number;
                                                                outvar.Message = var1.Number.ToString();
                                                            }

                                                            break;
                                                        }
                                                    }
                                                    if (foundOutput == false)
                                                    {
                                                        SendError($"Unable to locate variable {outputVar}.");
                                                    }

                                                }
                                                else
                                                {
                                                    SendError($"Variable {varB} is not an integer.");
                                                }
                                                break;
                                            }
                                        }
                                        if (foundB == false)
                                        {
                                            SendError($"Unable to locate variable {varB}.");
                                        }

                                    }
                                    else
                                    {
                                        SendError($"Variable {varA} is not an integer.");
                                    }
                                    break;
                                }
                            }
                            if (foundA == false)
                            {
                                SendError($"Unable to locate variable {varA}.");
                            }

                        }
                        else
                        {
                            SendError("Incorrect syntax for mathematical operation.");
                        }
                    }
                    if (checker_array[1] == "POW")
                    {

                    }
                }

                if (Input.StartsWith("RANDOMIZEINT "))
                {
                    string[] checker_array = Input.Split(' ', 4);
                    if (checker_array.Length == 4)
                    {
                        string varName = checker_array[1];
                        bool varExists = false;
                        bool varIsInt = false;
                        foreach (Variable var in ActiveVariables)
                        {
                            if (var.VarName == varName)
                            {
                                varExists = true;
                                if (var.IsInteger == true)
                                {
                                    varIsInt = true;
                                    bool validX = false;
                                    bool validY = false;
                                    string x_ = ConvertAllVariable(checker_array[2], true);
                                    string y_ = ConvertAllVariable(checker_array[3], true);
                                    int x = 0;
                                    int y = 0;

                                    if (x_.All(char.IsDigit))
                                    {
                                        x = Convert.ToInt32(x_);
                                        validX = true;
                                    }
                                    else
                                    {
                                        SendError("Must use all digits and integers for random range.");
                                    }

                                    if (y_.All(char.IsDigit))
                                    {
                                        y = Convert.ToInt32(y_);
                                        validY = true;
                                    }
                                    else
                                    {
                                        SendError("Must use all digits and integers for random range.");
                                    }

                                    if (validX == true && validY == true)
                                    {
                                        if (y > x)
                                        {
                                            int z = RandomInt(x, y);
                                            var.Number = z;
                                            var.Message = z.ToString();
                                        }
                                        else
                                        {
                                            SendError("First number must be greater than second number.");
                                        }
                                    }

                                }
                                else
                                {
                                    SendError($"Variable {varName} is not an integer.");
                                }
                                break;
                            }
                        }
                        if (varExists == false)
                        {
                            SendError($"Could not locate variable {varName}.");
                        }
                    }
                    else
                    {
                        SendError("Incorrect syntax for RANDOMIZE.");
                    }
                }

                if (Input.StartsWith("INQUIRY "))
                {
                    bool ValidSyntax = true;
                    bool ValidNumber = false;
                    bool IsVar = false;
                    bool VarExists = false;
                    string[] string_array = Input.Split(' ', 4);
                    if (string_array.Length == 4)
                    {
                        if (string_array[1] == "0") { ValidNumber = true; }
                        if (string_array[1] == "1") { ValidNumber = true; }
                        if (string_array[1] == "2") { ValidNumber = true; }
                        if (ValidNumber == false) { ValidSyntax = false; SendError("Incorrect syntax for INQUIRY."); }

                        if (ValidSyntax == true)
                        {
                            if (string_array[2].StartsWith("_"))
                            {
                                string placeholder = string_array[2].Remove(0, 1);
                                string_array[2] = placeholder;
                                IsVar = true;
                            }
                            else
                            {
                                IsVar = false;
                            }
                            if (IsVar == true)
                            {
                                UserInquiry userInquiry = new UserInquiry(string_array[3], Convert.ToInt32(string_array[1]));
                                foreach (Variable var in ActiveVariables)
                                {
                                    if (var.VarName == string_array[2])
                                    {
                                        if (var.IsInteger == true)
                                        {
                                            if (string_array[1] == "0")
                                            {
                                                var.Message = userInquiry.Response();
                                                if (var.Message == "1") { var.Number = 1; var.Message = var.Number.ToString(); }
                                                if (var.Message == "0") { var.Number = 0; var.Message = var.Number.ToString(); }
                                            }
                                            else
                                            {
                                                SendError("Incompatible value and variable type. Integer can only accept numbers.");
                                            }
                                        }
                                        else
                                        {
                                            var.Message = userInquiry.Response();
                                        }
                                        VarExists = true;
                                        break;
                                    }
                                }
                                if (VarExists == false)
                                {
                                    SendError("Could not locate variable " + string_array[2] + ".");
                                }
                            }
                            else
                            {
                                foreach (Variable vari in ActiveVariables)
                                {
                                    if (vari.VarName == string_array[2])
                                    {
                                        VarExists = true;
                                        break;
                                    }
                                }
                                if (VarExists == false)
                                {

                                    UserInquiry userInquiry = new UserInquiry(string_array[3], Convert.ToInt32(string_array[1]));
                                    string placeholder = userInquiry.Response();
                                    Variable newvar = new Variable(true, placeholder, string_array[2]);

                                    newvar.IsString = true;
                                    newvar.IsInteger = false;
                                    ActiveVariables.Add(newvar);
                                }
                                else
                                {
                                    SendError("Variable named " + string_array[2] + " already exists.");
                                }
                            }



                        }
                    }
                    else { SendError("Incorrect syntax for INQUIRY command."); }
                }

                if (Input.StartsWith("POPUP "))
                {
                    string[] checker_array = Input.Split(' ', 4);
                    if (checker_array[1] == "MESSAGE")
                    {
                        string[] string_array = Input.Split(' ', 3);
                        OKMessage okmsg = new OKMessage();
                        string Q = ConvertAllVariable(string_array[2], true);
                        okmsg.Draw(Q);
                    }
                    else if (checker_array[1] == "INQUIRY")
                    {
                        string[] string_array = Input.Split(' ', 5);
                        bool ValidSyntax = true;
                        bool ValidNumber = false;
                        bool IsVar = false;
                        bool VarExists = false;
                        if (string_array.Length == 5)
                        {
                            if (string_array[2] == "0") { ValidNumber = true; }
                            if (string_array[2] == "1") { ValidNumber = true; }
                            if (string_array[2] == "2") { ValidNumber = true; }
                            if (ValidNumber == false) { ValidSyntax = false; SendError("Incorrect syntax for POPUP ."); }
                            if (ValidNumber == true)
                            {
                                if (string_array[2] == "0")
                                {
                                    foreach (Variable var in ActiveVariables)
                                    {
                                        if (var.VarName == string_array[3])
                                        {
                                            YesNoPrompt ynmsg = new YesNoPrompt();
                                            var.Number = ynmsg.Draw(string_array[4]);
                                            var.Message = var.Number.ToString();
                                            VarExists = true;
                                            break;
                                        }
                                    }
                                    if (VarExists == false)
                                    {
                                        if (string_array[3].All(char.IsLetterOrDigit))
                                        {
                                            Variable var = new Variable(true, "0", string_array[3]);
                                            YesNoPrompt ynmsg = new YesNoPrompt();
                                            string Q = ConvertAllVariable(string_array[4], true);
                                            var.Number = ynmsg.Draw(Q);
                                            var.Message = var.Number.ToString();
                                            var.IsString = true;
                                            var.IsInteger = true;
                                            ActiveVariables.Add(var);
                                        }
                                        else { SendError("Invalid variable "); }
                                    }
                                }
                            }
                        }
                    }
                }

                if (Input.StartsWith("EXEC "))
                {
                    bool ExecFound = false;
                    string FuncCall = "";
                    string[] string_array = Input.Split(' ', 2);
                    if (RunningScript == true)
                    {
                        if (string_array.Length == 2)
                        {
                            int x = 0;
                            foreach (string str in AllLines)
                            {
                                if (str.StartsWith(':'))
                                {
                                    FuncCall = str.Remove(0, 1);
                                }
                                if (string_array[1] == FuncCall)
                                {
                                    ExecFound = true;
                                    CurrentLine = x;
                                    break;
                                }
                                x++;
                            }
                            foreach (string str in CloudScript)
                            {
                                if (str.StartsWith(':'))
                                {
                                    FuncCall = str.Remove(0, 1);
                                }
                                if (string_array[1] == FuncCall)
                                {
                                    ExecFound = true;
                                    CurrentLine = x;
                                    break;
                                }
                                x++;
                            }
                            if (ExecFound == false)
                            {
                                SendError("Could not locate function " + string_array[1] + ".");
                            }
                        }
                        else
                        {
                            SendError("Incorrect syntax for EXEC command.");
                        }
                    }
                    else { SendError("Must be executing script to use EXEC command."); }
                }
            } catch (Exception e)
            {
                SendError("GyroPrompt encountered an unexpected error.");
                Console.WriteLine(e);
            }
        }
    
        public string ConvertAllVariable(string Input, bool AndTags)
        {
            if (AndTags == false)
            {
                string[] placeholder = Input.Split('{', '}');
                string to_return = "";
                int ticker = placeholder.GetLength(0);
                int x = 0;
                int y = 0;
                int z = 0;
                while (x < ticker)
                {
                    foreach (Variable variable in ActiveVariables)
                    {
                        if (variable.VarName == placeholder[x])
                        {
                            placeholder[x] = variable.Message;
                        }
                    }
                    x++;
                }

                while (y < ticker)
                {
                    to_return = to_return + placeholder[y];
                    switch (z)
                    {
                        case 0:
                            to_return = to_return + '{';
                            z++;
                            break;
                        case 1:
                            to_return = to_return + '}';
                            z = 0;
                            break;
                    }
                    y++;
                }
                return to_return;
            } else 
            {
                System.Text.StringBuilder final_string = new System.Text.StringBuilder("");
                final_string.Append(Input);
                final_string.Replace("{T}", DateTime.Now.ToString("h:mm tt"));
                final_string.Replace("{D}", DateTime.Now.ToString("MM/dd/yyyy"));
                final_string.Replace("{L}", "\n") ;
                foreach(Variable var in ActiveVariables)
                {
                    final_string.Replace("{" + var.VarName + "}", var.Message);
                }
                string final = final_string.ToString();
                return final;
            }
        }

        public int RandomInt (int x, int y)
        {
            Random _BKRandy = new Random();
            int z = _BKRandy.Next(x, y);
            return z;
        }

        public string ConvertList(string Input)
        {
            StringBuilder final_string = new StringBuilder("");
            final_string.Append(Input);
            foreach(VariableList varlist in ActiveVariableList)
            {
                foreach(Variable var in varlist.VarList)
                {
                    final_string.Replace($"{{{varlist.Name}_{var.VarName}}}", var.Message);
                }
                final_string.Replace($"{{#{varlist.Name}}}", varlist.ToString());
            }
            string a = final_string.ToString();
            return a;
        }

        public void NewConsoleColor(int y, int z)
        {
            int w = z;
            int x = y;
            Console.BackgroundColor = (ConsoleColor)x;
            Console.ForegroundColor = (ConsoleColor)w;
            textColor = (ConsoleColor)w;
        }
        
        public void SendError(string ErrMessage)
        {
            Console.WriteLine();
            Console.WriteLine("GyroPrompt Script ‼ " + ErrMessage + "\n");
        }

        public void Initialize(string script)
        {
            bool FileE = false;
            if (File.Exists(script))
            { 
                Console.WriteLine("Initializing " + script);
                Console.WriteLine();
                FileE = true;
                Run(script);
            }
            if (File.Exists(script + ".gs"))
            {
                Console.WriteLine("Initializing " + script);
                Console.WriteLine();
                FileE = true;
                Run(script + ".gs");
            }

            if (FileE == false)
            {
                SendError("Specified file does not exist. Could not execute.");
            }
            
        }

        public void Run(string script)
        {
            RunningScript = true;
            int x = 200;
            foreach (Variable var in ActiveVariables)
            {
                if (var.VarName == "BUFFERDELAY")
                {
                    x = var.Number;
                    break;
                }
            }
            PlaceholderVariable = ActiveVariables;
            PlaceholderFunctions = ActiveFunctions;
            List<string> Lines = System.IO.File.ReadAllLines(script).ToList<string>();
            AllLines = Lines;
            MaxLines = Lines.Count();
            Thread taskTicker = new Thread(TaskTicker);
            taskTicker.Start();
            while (CurrentLine < Lines.Count)
            {
               Parse(Lines[CurrentLine]);
               CurrentLine++;
               Thread.Sleep(x);
               foreach(Variable var in ActiveVariables)
                {
                    if (var.VarName == "BUFFERDELAY")
                    {
                        x = var.Number;
                        break;
                    }
                }
            }
            CurrentLine = 0;  // Reset
            Console.WriteLine("Finished running script...");
            RunningScript = false;
            ActiveVariables.Clear();
            ActiveFunctions.Clear();
            ActiveVariables = PlaceholderVariable;
            ActiveFunctions = PlaceholderFunctions;
            AllLines.Clear();
            Lines.Clear();
            MaxLines = 0;
        }

        public void TaskTicker()
        {
            while (RunningScript == true)
            {
                foreach(TimedFunction task in ActiveFunctions)
                {
                    if (task.Active == true)
                    {
                        if (task.Counter == task.Interval)
                        {
                            TaskRun(task.FunctionCommands);
                            task.Counter = 0; // reset
                        }
                        else
                        {
                            task.Counter++; // increment
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }
        public void TaskRun(List<string> Commands)
        {
            foreach(string command in Commands)
            {
                Parse(command);
            }
        }

        public void RunScriptFromCloud(string[] script_data, string fileName)
        {
            int scriptLineCount = 0;
            
            foreach(string str in script_data)
            {
                CloudScript.Add(str);
                scriptLineCount++;
            }

            UserInquiry viewFirst = new UserInquiry("View contents of script before running?", 0);
            string view = viewFirst.Response();
            if (view == "1") { ViewScript(scriptLineCount, fileName); }

            UserInquiry proceed = new UserInquiry($"Proceed to run script {fileName}?", 0);
            string response = proceed.Response();
            if (response == "1")
            {
                RunningScript = true;
                int x = 200;
                foreach (Variable var in ActiveVariables)
                {
                    if (var.VarName == "BUFFERDELAY")
                    {
                        x = var.Number;
                        break;
                    }
                }
                PlaceholderVariable = ActiveVariables;
                PlaceholderFunctions = ActiveFunctions;

                while (CurrentLine < CloudScript.Count)
                {
                    Parse(CloudScript[CurrentLine]);
                    CurrentLine++;
                    Thread.Sleep(x);
                    foreach (Variable var in ActiveVariables)
                    {
                        if (var.VarName == "BUFFERDELAY")
                        {
                            x = var.Number;
                            break;
                        }
                    }
                }

                CurrentLine = 0;  // Reset
                Console.WriteLine("Finished running script...");
                RunningScript = false;
                ActiveVariables.Clear();
                ActiveFunctions.Clear();
                ActiveVariables = PlaceholderVariable;
                ActiveFunctions = PlaceholderFunctions;
                AllLines.Clear();
                CloudScript.Clear();
                MaxLines = 0;

            }
        }

        public void ViewScript(int lineCount, string name)
        {
            int lc = 1;
            Console.WriteLine($"\nScript: {name}\t {lineCount} total lines.");
            foreach(string str in CloudScript)
            {
                Console.WriteLine($"\t{lc}: {str}");
                lc++;
            }
            Console.WriteLine();
        }

        public void RefreshEnvVars()
        {
            bool Running = true;
            while (Running == true)
            {
                Thread.Sleep(50);
                foreach(Variable var in ActiveVariables)
                {
                    switch (var.VarName)
                    {
                        case "WINDOWWIDTH":
                            var.Message = Console.WindowWidth.ToString();
                            var.Number = Console.WindowWidth;
                            break;
                        case "WINDOWHEIGHT":
                            var.Message = Console.WindowHeight.ToString();
                            var.Number = Console.WindowHeight;
                            break;
                        case "CURSORX":
                            var.Message = Console.CursorLeft.ToString();
                            var.Number = Console.CursorLeft;
                            break;
                        case "CURSORY":
                            var.Message = Console.CursorTop.ToString();
                            var.Number = Console.CursorTop;
                            break;
                    }
                }
            }
        }

        private static Size GetConsoleFontSize()
        {
            IntPtr outHandle = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            int errorCode = Marshal.GetLastWin32Error();
            if (outHandle.ToInt32() == INVALID_HANDLE_VALUE)
            {
                throw new Exception("Error! CONOUT$");
            }
            ConsoleFontInfo cfi = new ConsoleFontInfo();
            if (!GetCurrentConsoleFont(outHandle,false,cfi))
            {
                throw new Exception("Error! CONOUT$");
            }

            return new Size(cfi.dwFontSize.X, cfi.dwFontSize.Y);
        }
        
        public void LoadGyro()
        {
            try
            {
                Point location = new Point(Console.WindowWidth - 20, Console.WindowHeight - 8);
                Size ImageSize = new Size(20, 8);

                using (Graphics g = Graphics.FromHwnd(GetConsoleWindow()))
                {
                    using (Bitmap logo = new Bitmap(GyroPrompt.Properties.Resources.gyro_logo))
                    {
                        Size fontSize = GetConsoleFontSize();
                        Rectangle imageRect = new Rectangle(

                        location.X * fontSize.Width,
                        location.Y * fontSize.Height,
                        ImageSize.Width * fontSize.Width,
                        ImageSize.Height * fontSize.Height);
                        g.DrawImage(logo, imageRect);
                    }
                }
            }
            catch 
            {
            }
        }

        public void PaintImage(string imagePath, int x, int y, int a, int b)
        {
            ///<summary>
            /// x and y is size
            /// a and b is location
            /// 
            /// Syntax:
            /// PUSHIMAGE X Y X Y image_path
            /// </summary>
            try
            {
                Point location = new Point(a, b);
                Size ImageSize = new Size(x, y);

                using (Graphics g = Graphics.FromHwnd(GetConsoleWindow()))
                {
                    using (Bitmap logo = new Bitmap(imagePath))
                    {
                        Size fontSize = GetConsoleFontSize();
                        Rectangle imageRect = new Rectangle(

                        location.X * fontSize.Width,
                        location.Y * fontSize.Height,
                        ImageSize.Width * fontSize.Width,
                        ImageSize.Height * fontSize.Height);
                        g.DrawImage(logo, imageRect);
                    }
                }
            } catch 
            { 
               
            }
        }

    }
}
