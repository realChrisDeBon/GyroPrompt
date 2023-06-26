using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.Text;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;

namespace GyroPrompt.Compiler
{
    /// <summary>
    /// lmao so this is such a dingleberry way to do it, so we just take the C# solution for GyroPrompt, package it into a .zip, then extract that .zip folder
    /// then using a handy dandy StringBuiler, we re-write the Main() in Program.cs to just run each line through the parser. Then we use 'dotnet' to publish 
    /// it as a standalone .exe lol. Not technically its own 'compiler' I guess it's a way to convert a script file to an executable though. It works. Just need
    /// to kind of modify a few things so the target machine doesn't require Dotnet being installed (via including a runtime and dependencies directory)
    /// </summary>

    public class ScriptCompiler
    {
        public void Compile(string scriptpath)
        {

            List<string> Lines = System.IO.File.ReadAllLines(scriptpath).ToList<string>(); // Create a list of string so the file can be read line-by-line
            FileInfo fileInfo = new FileInfo(scriptpath);
            string appname = Path.GetFileNameWithoutExtension(fileInfo.Name);
            StringBuilder allCommands = new StringBuilder();
            int currentind = 0;
            int last_ = Lines.Count - 1;
            foreach (string line in Lines)
            {
                allCommands.Append("\"" + line + "\"");
                if (currentind < last_)
                {
                    allCommands.Append(", ");
                }

            }
            allCommands.Append("};");

            string resourceName = "GyroPrompt.Output.zip";
            string outputDir = Environment.CurrentDirectory + "\\Output";
            // Extract .zip folder
            // Load the embedded resource
            ManualResetEvent extractionCompleted = new ManualResetEvent(false);

            using (Stream resourceStream = typeof(ScriptCompiler).Assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    Console.WriteLine("Embedded resource not found.");
                    return;
                }

                // Create the output directory if it doesn't exist
                if (!Directory.Exists(outputDir))
                {
                    DirectoryInfo directoryInfo = Directory.CreateDirectory(outputDir);
                    directoryInfo.Attributes |= FileAttributes.Hidden;
                }

                // Copy the resource stream to a temporary file
                string tempFilePath = Path.GetTempFileName();
                using (FileStream tempFileStream = File.OpenWrite(tempFilePath))
                {
                    resourceStream.CopyTo(tempFileStream);
                }

                // Extract the contents of the ZIP folder
                // Extract the contents of the ZIP folder in a separate thread
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        using (ZipArchive zipArchive = ZipFile.OpenRead(tempFilePath))
                        {
                            foreach (ZipArchiveEntry entry in zipArchive.Entries)
                            {
                                var extractedFileOrFolder = entry.FullName;
                                // Combine the output directory with the entry's name to get the full path
                                string outputPath = Path.Combine(outputDir, entry.FullName);
                                try
                                {
                                    var destDirPath = Path.GetDirectoryName(outputPath);
                                    Directory.CreateDirectory(destDirPath);
                                    entry.ExtractToFile(outputPath, true);
                                }
                                catch (Exception ex)
                                {

                                }

                            }
                        }
                        // Signal that the extraction is completed
                        extractionCompleted.Set();
                    }
                    catch (Exception ex)
                    {

                    }
                    // Delete the temporary file
                    File.Delete(tempFilePath); // temp files are like cheap hookers that you throw away once you're done
                });
            }
            // Wait for the extraction to complete
            extractionCompleted.WaitOne();

            //Console.WriteLine("ZIP folder extracted successfully.");

            bool fileExists = File.Exists(outputDir + "\\GyroPrompt\\GyroPrompt\\Program.cs");
            if (fileExists == true)
            {
                // We're going to ragtag Frankenstein-hack some stuff here with a StringBuilder fingers crossed
                StringBuilder mainclass = new StringBuilder();
                mainclass.AppendLine(@"using Terminal.Gui;

namespace GyroPrompt
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize console
            Parser parser = new Parser();
            parser.setenvironment();
            Console.Title = (""Application"");");

                mainclass.AppendLine(@"            string[] commands = new string[] {" + allCommands);

                mainclass.AppendLine(@"
            foreach(string s in commands){
            parser.parse(s);
            parser.parse(""pause 1000"");
            }
        }
    }
}");


                string projectFilePath = outputDir + "\\GyroPrompt\\GyroPrompt\\GyroPrompt.csproj";
                File.WriteAllText(outputDir + "\\GyroPrompt\\GyroPrompt\\Program.cs", mainclass.ToString());
                string outputDirectory = Environment.CurrentDirectory + "\\app";

                try
                {
                    ExecuteDotnetPublish(projectFilePath, outputDirectory, appname, outputDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
                {
                    var stringText = SourceText.From(text, Encoding.UTF8);
                    return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
                }


                static void ExecuteDotnetPublish(string projectFilePath, string outputDirectory, string applicationname, string deleteThisDir)
                {
                    string dotnetExePath = GetDotnetExePath();

                    if (dotnetExePath == null)
                    {
                        Console.WriteLine("dotnet executable not found.");
                        return;
                    }

                    string arguments = $"publish \"{projectFilePath}\" --output \"{outputDirectory}\" -r win-x86 -p:PublishSingleFile=true --self-contained true -p:AssemblyName={applicationname} -p:TrimUnusedDependencies=true";

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = dotnetExePath,
                        Arguments = arguments,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };

                    using (Process process = Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("Publish succeeded.");
                            Directory.Delete(deleteThisDir, true);
                        }
                        else
                        {
                            Console.WriteLine("Publish failed.");
                            Console.WriteLine(output);
                            Directory.Delete(deleteThisDir, true);
                        }
                    }

                    static string GetDotnetExePath()
                    {
                        string dotnetSdkPath = "C:\\Program Files\\dotnet";

                        if (string.IsNullOrEmpty(dotnetSdkPath))
                        {
                            return null;
                        }

                        string dotnetExePath = Path.Combine(dotnetSdkPath, "dotnet.exe");

                        if (File.Exists(dotnetExePath))
                        {
                            return dotnetExePath;
                        }
                        return null;
                    }

                }
            }
            else
            {
                Console.WriteLine("Error loading script file.");
            }
        } 

    }
}