using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace GyroPrompt.BootSystem
{
    class InitializeWindows
    {
        public bool Clear = false;

        public bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }
        public bool SystemsCheck()
        {
            bool IsAdmin = IsAdministrator();

            RegistryKey rkSubKey = Registry.ClassesRoot.OpenSubKey(".gs", false);
            if (rkSubKey == null)
            {
                if (IsAdmin == true)
                {
                    try
                    {
                        string extention = ".gs";
                        RegistryKey key = Registry.ClassesRoot.CreateSubKey(extention);
                        key.SetValue("", "GyroPrompt Script");
                        key.Close();

                        key = Registry.ClassesRoot.CreateSubKey(extention + "\\Shell\\Open\\command");
                        //key = key.CreateSubKey("command");

                        key.SetValue("", "\"" + Directory.GetCurrentDirectory() + "\\GyroPrompt.exe" + "\" \"%L\"");
                        key.Close();

                        key = Registry.ClassesRoot.CreateSubKey(extention + "\\DefaultIcon");
                        if (File.Exists(Directory.GetCurrentDirectory() + "\\gyro_icon.ico"))
                        {
                            key.SetValue("", Directory.GetCurrentDirectory() + "\\gyro_icon.ico");
                            key.Close();
                            if (rkSubKey != null) { Clear = true; }
                        } else
                        {
                            Console.WriteLine("Could not locate gyro_icon.ico file required for initialization.");
                            Clear = false;
                        }
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to put GyroPrompt Script file type into registry.");
                        Clear = false;
                    }
                } else
                {
                    Console.WriteLine("Please restart and run GyroPrompt as administrator.\nThis is only required for the first time.");
                    Clear = false;
                }
            }
            else
            {
                Clear = true;
            }
            return Clear;
        }
    }
}