using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;
using System.Reflection.Metadata;
using NStack;

namespace GyroPrompt.Basic_Objects.Component
{

    public class FilesystemInterface
    {
        public Parser topparse;


        public Dictionary<string, Action<string, string>> commandDirectoryVoid = new Dictionary<string, Action<string, string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Func<string, string, string[]>> commandDirectoryReturnString = new Dictionary<string, Func<string, string, string[]>>(StringComparer.OrdinalIgnoreCase);

        // Action uses placeholder
        public Dictionary<string, bool> actionsWithPlaceholder = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            {"delete", true}, {"sethidden", true}, {"setvisible", true}, {"mkdir", true}, {"rmdir", true}, {"readall", true}
        };
        public Dictionary<string, bool> outputsToFile = new Dictionary<string, bool>()
        {
            {"write", true }, {"append", true }
        };

        // Action declarations
        public Action<string, string> WriteOver = new Action<string, string>(WriteOverFile);
        public Action<string, string> AppendTo = new Action<string, string>(AppendToFile);
        public Action<string, string> Delete = new Action<string, string>(DeleteFile);
        public Action<string, string> CopyTo = new Action<string, string>(CopyFileToLocation);
        public Action<string, string> MoveTo = new Action<string, string>(MoveFileToLocation);
        public Action<string, string> SetHidden = new Action<string, string>(SetFileToHidden);
        public Action<string, string> SetVisible = new Action<string, string>(SetHiddenFileToVisible);
        public Action<string, string> CreateDir = new Action<string, string>(CreateDirectory);
        public Action<string, string> DelDir = new Action<string, string>(RemoveDirectory);
        public Action<string, string> CopyDir = new Action<string, string>(CopyDirectoryToLocation);
        public Action<string, string> MoveDir = new Action<string, string>(MoveDirectoryToLocation);
        // Func declarations
        public Func<string, string, string[]> ReadFile = new Func<string, string, string[]>(ReadEntireFile);
        public Func<string, string, string[]> ReadFileIntoList = new Func<string, string, string[]>(ReadFileToList);
        // Predicate declarations

        public void LoadComDict()
        {
            // Actions
            commandDirectoryVoid.Add("write", WriteOver);
            commandDirectoryVoid.Add("append", AppendTo);
            commandDirectoryVoid.Add("delete", Delete);
            commandDirectoryVoid.Add("copy", CopyTo);
            commandDirectoryVoid.Add("move", MoveTo);
            commandDirectoryVoid.Add("sethidden", SetHidden);
            commandDirectoryVoid.Add("setvisible", SetVisible);
            commandDirectoryVoid.Add("mkdir", CreateDir);
            commandDirectoryVoid.Add("rmdir", DelDir);
            commandDirectoryVoid.Add("copydir", CopyDir);
            commandDirectoryVoid.Add("movedir", MoveDir);

            // Functions
            commandDirectoryReturnString.Add("realall", ReadFile);
            commandDirectoryReturnString.Add("readtolist", ReadFileIntoList);

        }
        public bool HasOutputToFile(string command)
        {
            if (outputsToFile.ContainsKey(command))
            {
                return true;
            } else
            {
                return false;
            }
        }
        public string fileName(string path)
        {
            return Path.GetFileName(path);
        }

        // File read write move copy
        static void WriteOverFile(string path, string contents)
        {
            try
            {
                File.WriteAllText(path, contents);
            }
            catch
            {
                Console.WriteLine($"Could not write to file {path}");
            }
        }
        static void AppendToFile(string path, string contents)
        {

                File.AppendAllText(path, contents);

        }
        static void WriteListToFile(string path, LocalList lineList)
        {
            try
            {
                foreach(Variables.LocalVariable variable in lineList.items)
                {
                    File.AppendText(variable.Value + Environment.NewLine);
                }
            }
            catch
            {
                Console.WriteLine($"There was an error writing list to file.");
            }
        }
        static string[] ReadEntireFile(string path, string placeholder)
        {
            try
            {
                string[] filecontents = File.ReadAllLines(path);
                return filecontents;
            } catch
            {
                Console.WriteLine($"Could not read file {path}");
                return null;
            }
        }
        static string ReadFileLine(string path, string linenumber_)
        {
            int linenumber = Int32.Parse(linenumber_);
            try
            {
                return File.ReadLines(path).Skip(linenumber - 1).FirstOrDefault();
            } catch
            {
                Console.WriteLine($"Could not read line number {linenumber} in file {path}");
                return null;
            }
        }
        static string[] ReadFileToList(string path, string placeholder)
        {
            try
            {
                string[] filecontents = File.ReadAllLines(path);
                return filecontents;
            }
            catch
            {
                Console.WriteLine($"Could not read file {path}");
                return null;
            }
        }

        static void DeleteFile(string path, string placeholder)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                Console.WriteLine($"Could not delete file {path}");
            }
        }
        static void CopyFileToLocation(string path, string pathDestination)
        {
            try
            {
                File.Copy(path, pathDestination);
            }
            catch
            {
                Console.WriteLine($"Could not copy file {path} to {pathDestination}");
            }
        }
        static void MoveFileToLocation(string path, string pathDestination)
        {
            try
            {
                File.Move(path, pathDestination);
            }
            catch
            {
                Console.WriteLine($"Could not move file {path} to {pathDestination}");
            }
        }
        static void SetFileToHidden(string path, string placeholder)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    attributes |= FileAttributes.Hidden;
                    File.SetAttributes(path, attributes);
                } else
                {
                    Console.WriteLine($"Could not locate {path}");
                }
            } catch
            {
                Console.WriteLine($"Could not set {path} to hidden");
            }
        }
        static void SetHiddenFileToVisible(string path, string placeholder)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    attributes &= ~FileAttributes.Hidden;
                    File.SetAttributes(path, attributes);
                } else
                {
                    Console.WriteLine($"Could not locate {path}");
                }
            }
            catch
            {
                Console.WriteLine($"Could not set {path} to visible");
            }
        }

        // Directories read write move copy
        static void CreateDirectory(string path, string placeholder)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch
            {
                Console.WriteLine($"Could not create directory {path}");
            }
        }
        static void RemoveDirectory(string path, string placeholder)
        {
            try
            {
                
                Directory.Delete(path);
            }
            catch
            {
                Console.WriteLine($"Could not remove directory {path}");
            }
        }
        static void CopyDirectoryToLocation(string path, string DestinationPath)
        {
            try
            {
                CopyDirectoryToLocation (path, DestinationPath);
            }
            catch
            {
                Console.WriteLine($"Could not move directory {path} to {DestinationPath}");
            }
        }
        static void MoveDirectoryToLocation(string path, string pathDestination)
        {
            try
            {
                Directory.Move(path, pathDestination);
            }
            catch
            {
                Console.WriteLine($"Could not move directory {path} to {pathDestination}");
            }
        }

        // Return unique directories
        private string GetStartupFolderPath(string a, string b)
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return startupFolderPath;
        }

        // Does exists
        public bool DoesDirectoryExist(string path)
        {
            return Directory.Exists(path);
        }
        public bool DoesFileExist(string path)
        {
            return File.Exists(path);
        }
    }
}
