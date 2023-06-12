using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GyroPrompt.Compiler
{
    /// <summary>
    /// lol so I for some reason decided like "Hey, I hate my life, might as well give GyroPrompt a compiler! What could possibly go wrong?!"
    /// and the idea is simple in theory - just parse through the .gs script files and translate it into corresponding C# code, then use either Roslyn or CodeDom
    /// to output a binary... right? Lol guess again retard. So this is here for the time being until I can get it up and working. 
    /// 
    /// Current snag: It successfully output the "output.exe" and "output.pbd" files, however, when executing the binary application I 
    /// am receiving the following error: Unhandled Exception: System.IO.FileNotFoundException: Could not load file or assembly 'System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e' or one of its dependencies. The system cannot find the file specified.
    /// I have been going through as much documentation as I can and there seems to be very little for similar instances of what I am attempting to do here, 
    /// especially for .Net 6.0
    /// 
    /// Most documentation suggests this is platform compatibility. I've checked my CodeDom NuGet release and it was stable and updated this year (like a month ago) and references
    /// that it is in support of .Net 6.0 I have also ensured 'System.Runtime.dll' is in the same working directory as our working binary and output binary, even experimenting to 
    /// see if manually putting a copy of 'System.Private.CoreLib.dll' in the working directory would impact anything (it didn't).
    /// 
    /// Another note: Running GyroPrompt from the repo /debug directory provides more results - we get the .exe to compile, but the .exe has runtime issues.
    /// However, if we publish GyroPrompt as a standalone .exe in a self-contained deployment mode, we are unable to compile the .exe at all and we get a 
    /// (Path) is Null kind of error... possibly suggesting the compilation method may need tweaking to remain portable
    /// </summary>

    // Monitoring: https://github.com/ligershark/WebOptimizer/issues/172
    // apparently others have experienced similar issues jumping from .Net 5.0 into .Net 6.0 so we might just be waiting for a fix idk fuck this 

    public class ScriptCompiler
    {
        public void Compile()
        {
            string basePath = Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location);
            string assemblyName = Path.GetRandomFileName();
            // Just some test code until we tweak the compiler settings to output flawlessly 
            var tree = CSharpSyntaxTree.ParseText(@"
        using System;

        namespace HelloWorldprogram
        {
        internal class Program
        {
            public static void Main()
            {
                Console.WriteLine(""Hello World!"");
                Console.ReadLine();
            }   
        }
        }");



            var root = tree.GetRoot() as CompilationUnitSyntax;
            var references = root.Usings;
            var referencePaths = new List<string> {
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                Path.Combine(basePath, "System.Runtime.dll"),
                Path.Combine(basePath, "System.Runtime.Extensions.dll"),
                Path.Combine(basePath, "mscorlib.dll")
            };
            referencePaths.AddRange(references.Select(x => Path.Combine(basePath, $"{x.Name}.dll")));
            var executableReferences = new List<MetadataReference>();
            foreach (var reference in referencePaths)
            {
                if (File.Exists(reference))
                {
                    executableReferences.Add(MetadataReference.CreateFromFile(reference));
                }
            }

            ///<remarks>
            /// Herein is where I'm pretty certain our problem is emerging. All the documentation I have come across suggests 'System.Private.CoreLib' should be embedded in the
            /// framework automatically, however, I have repeatedly been faced with the above mentioned issue. So I will continue to come back to this occasionally but I am not going
            /// to get fixated on this feature (that really should be more of a late stage feature anyways) and lose focus on expanding functionality and scope of parses
            /// </remarks>
            
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                platform: Platform.X86
                );
            var compilation = CSharpCompilation.Create(assemblyName,
                syntaxTrees: new[] { tree }, 
                references: executableReferences,
                options: compilationOptions);

            // Generate the output file paths
            var outputDir = AppDomain.CurrentDomain.BaseDirectory;
            string outputPath = Path.Combine(outputDir, "output.exe");
            string pdbPath = Path.Combine(outputDir, "output.pdb");
           
            // Compile the output binary
            var emitResult = compilation.Emit(outputPath, pdbPath);

            //If our compilation failed
            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Console.WriteLine(diagnostic.ToString());
                }
            }
        }

    }
}