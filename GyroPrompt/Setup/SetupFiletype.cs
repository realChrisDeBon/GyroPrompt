using Microsoft.Win32;
using System.Security.Principal;

namespace GyroPrompt.Setup
{
    /// <summary>
    /// This class will insert a registry key telling Windows to recognize the .gs file extension as a GyroScript, and will allow users to simply double-click the
    /// .gs files on their system and have them automatically execute in the GyroPrompt. GyroPrompt must be executed with administrative privileges in order to be 
    /// able to author a key into the Window's registry. Need to think of appropriate prompt on start up to implement this later on.
    /// </summary>
    internal class SetupFiletype
    {
        public bool Clear = false;

        // We must be able to determine if the user is running 'As Administrator'. If they are, we can proceed. Otherwise, we require elevated privileges.
        public bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }

        public bool SystemsCheck()
        {
            bool IsAdmin = IsAdministrator(); // Let's check for admin privileges

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
                        key.Close(); // We've created the SubKey for Windows to recognize the file type

                        key = Registry.ClassesRoot.CreateSubKey(extention + "\\Shell\\Open\\command");
                        key.SetValue("", "\"" + Directory.GetCurrentDirectory() + "\\GyroPrompt.exe" + "\" \"%L\"");
                        key.Close(); // We've told the SubKey to run the file with GyroPrompt.exe, providing GyroPrompt.exe's location within the file system

                        // Now we will tell Windows to assign a specific .ico file (the GyroPrompt icon) to all .gs files
                        key = Registry.ClassesRoot.CreateSubKey(extention + "\\DefaultIcon");
                        if (File.Exists(Directory.GetCurrentDirectory() + "\\Resources\\gyroscript.ico"))
                        {
                            key.SetValue("", Directory.GetCurrentDirectory() + "\\Resources\\gyroscropt.ico");
                            key.Close();
                            if (rkSubKey != null) { Clear = true; }
                        }
                        else
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
                }
                else
                {
                    // We do not have administrative privileges, so we are unable to tell Windows to recognize the new .gs file type
                    Console.WriteLine("Please restart and run GyroPrompt as administrator.\nThis is only required for the first time.");
                    Clear = false;
                }
            }
            else
            {
                Clear = true;
            }
            return Clear; // Give the all clear or not, 'false' means something went wrong, 'true' means we successfully put the new .gs file type in the system
        }
    }
}