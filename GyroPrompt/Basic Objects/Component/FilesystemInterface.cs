using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;

namespace GyroPrompt.Basic_Objects.Component
{
    public class FilesystemInterface
    {
       
        // File read write move copy
        public void WriteOverFile(string path, string contents)
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
        public void AppendToFile(string path, string contents)
        {

                File.AppendAllText(path, contents);

        }
        public void WriteListToFile(string path, LocalList lineList)
        {
            try
            {
                foreach(LocalVariable variable in lineList.items)
                {
                    File.AppendText(variable.Value + "\n");
                }
            }
            catch
            {
                Console.WriteLine($"There was an error writing list to file.");
            }
        }
        public string ReadEntireFile(string path)
        {
            try
            {
                return File.ReadAllText(path);
            } catch
            {
                Console.WriteLine($"Could not read file {path}");
                return null;
            }
        }
        public string ReadFileLine(string path, int linenumber)
        {
            try
            {
                return File.ReadLines(path).Skip(linenumber - 1).FirstOrDefault();
            } catch
            {
                Console.WriteLine($"Could not read line number {linenumber} in file {path}");
                return null;
            }
        }
        public LocalList ReadFileToList(string path, string lstname)
        {
            LocalList list = new LocalList();
            if (File.Exists(path))
            {
                try
                {
                    string[] filecontents = File.ReadAllLines(path);
                    FileInfo fileInfo = new FileInfo(path);
                    string filename = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    int x = 1;
                    foreach (string line in filecontents)
                    {
                        StringVariable newline = new StringVariable();
                        newline.Name = $"{filename}line{x}";
                        newline.Type = VariableType.String;
                        newline.Value = line;
                        list.items.Add(newline);
                        list.numberOfElements++;
                        x++;
                    }
                    list.Name = $"{lstname}";
                    list.arrayType = ArrayType.String;
                    return list;
                }
                catch
                {
                    Console.WriteLine($"Error reading file {path} to list.");
                    return null;
                }
            } else
            {
                Console.WriteLine($"Could not read file {path}");
                return null;
            }
        }
        public void DeleteFile(string path)
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
        public void CopyFileToLocation(string path, string pathDestination)
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
        public void MoveFileToLocation(string path, string pathDestination)
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
        public void SetFileToHidden(string path)
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
        public void SetHiddenFileToVisible(string path)
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
        public void CreateDirectory(string path)
        {
            try
            {
                CreateDirectory(path);
            }
            catch
            {
                Console.WriteLine($"Could not create directory {path}");
            }
        }
        public void RemoveDirectory(string path)
        {
            try
            {
                RemoveDirectory(path);
            }
            catch
            {
                Console.WriteLine($"Could not remove directory {path}");
            }
        }
        public void CopyDirectoryToLocation(string path, string DestinationPath)
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
        public void MoveDirectoryToLocation(string path, string pathDestination)
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
        private string GetStartupFolderPath()
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
