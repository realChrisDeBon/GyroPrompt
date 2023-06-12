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
    /// </summary>

    public class ScriptCompiler
    {
        public void Compile()
        {
            // Just some test code until we tweak the compiler settings to output flawlessly 
            var tree = CSharpSyntaxTree.ParseText(@"
        using System;
        public class C
        {
            public static void Main()
            {
                Console.WriteLine(""Hello World!"");
                Console.ReadLine();
            }   
        }");

            ///<remarks>
            /// Herein is where I'm pretty certain our problem is emerging. All the documentation I have come across suggests 'System.Private.CoreLib' should be embedded in the
            /// framework automatically, however, I have repeatedly been faced with the above mentioned issue. So I will continue to come back to this occasionally but I am not going
            /// to get fixated on this feature (that really should be more of a late stage feature anyways) and lose focus on expanding functionality and scope of parses
            /// </remarks>
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location); // Required
            var test_ref = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location); // Required (throws lots of other errors without 'System.Runtime'
            var corelib = MetadataReference.CreateFromFile(typeof(string).Assembly.Location); // Required
            var conref = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location); // Absolutely required
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                platform: Platform.X86,
                mainTypeName: "C");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { mscorlib, conref, test_ref, corelib },
                compilationOptions);

            // Compile the output binary
            var emitResult = compilation.Emit("output.exe", "output.pdb");

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
