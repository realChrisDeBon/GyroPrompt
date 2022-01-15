using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GyroPrompt.Functions
{
    public class FileSystemIO
    {
        public List<string> LinesOfText = new List<string>();

        public void WriteFile(string path, string data)
        {
            try
            {
                File.AppendAllText(path, data);
            }
            catch
            {
                Console.WriteLine("\nGyroPrompt Script ‼ Unable to write file. Please double check location.\n");
            }
        }

        public string ReadFile(string path, int LineNumber, bool ReadAll)
        {
            if (File.Exists(path))
            {
                if (ReadAll == false)
                {
                    try
                    {
                        string a = File.ReadLines(path).Skip(LineNumber).Take(1).First();
                        return a;
                    }
                    catch
                    {
                        Console.WriteLine();
                        Console.WriteLine("\nGyroPrompt Script ‼ Could not read specified line number in " + path + ".\n");
                        string x = "";
                        return x;
                    }

                }
                else
                {
                    string[] b = File.ReadAllLines(path);
                    StringBuilder c = new StringBuilder("");
                    if (b.Length > 1)
                    {
                        foreach (string str in b)
                        {
                            c.Append(str + "{L}");
                        }
                    }
                    else
                    {
                        c.Append(b[0]);
                    }
                    string d = c.ToString();
                    return d;
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("\nGyroPrompt Script ‼ Specified file " + path + " could not be located.\n");
                return "";
            }
        }

        public void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch { Console.WriteLine("\nGyroPrompt Script ‼ Specified file" + path + " could not be located.\n"); }
        }

        public void CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch { Console.WriteLine("\nGyroPrompt Script ‼ Unable to create directory. Please double check location.\n"); }

        }

        public void DeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch { Console.WriteLine("\nGyroPrompt Script ‼ Unable to delete directory. Please double check location.\n"); }
        }

        public void MoveDirectory(string path, string destination)
        {
            try
            {
                Directory.Move(path, destination);
            }
            catch { Console.WriteLine("\nGyroPrompt Script ‼ Unable to move directory. Please double check location.\n"); }
        }

        public string ReturnFileSize(string path)
        {
            FileInfo fileinfo = new FileInfo(path);
            long length = fileinfo.Length;
            string returnString = Convert.ToString(length);
            return returnString;
        }

        public string ReturnFileExtension(string path)
        {
            FileInfo fileinfo = new FileInfo(path);
            string extension = fileinfo.Extension;
            return extension;
        }

        public void EncryptFile(string path)
        {
            try
            {
                File.Encrypt(path);
            }
            catch { Console.WriteLine("\nGyroPrompt Script ‼ Unable to encrypt " + path + ".\n"); }

        }
    }
}