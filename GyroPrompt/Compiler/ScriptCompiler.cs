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
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.Text;
using System.ComponentModel;

namespace GyroPrompt.Compiler
{
    /// <summary>
    /// lol so I for some reason decided like "Hey, I hate my life, might as well give GyroPrompt a compiler! What could possibly go wrong?!"
    /// and the idea is simple in theory - just parse through the .gs script files and translate it into corresponding C# code, then use either Roslyn or CodeDom
    /// to output a binary... right? Lol guess again retard. So this is here for the time being until I can get it up and working. 
    /// 
    /// Current snag: My IDE is only letting me use .Net 6.0, .NetStandard20, and .Net 4.7.2 even if I add the NuGet packages for like .Net50 or .Net70 (I'm sure this is a config thing)
    /// .Net60 will not produce a working .exe file. It can produce a .dll that can be interpreted in a .NetCore runtime environment, but is totally incapable of outputting
    /// a standalone .exe which is the problem because I am working in .Net 6.0
    /// For example, the string testContents works and successfully outputs a working .exe, but the second I add 'Thread.Sleep(500);' within the for loop, I receive error:
    /// System.Collections.Immutable.ImmutableArray`1[Microsoft.CodeAnalysis.Diagnostic]
    /// .... which implies compatibility problems 
    /// </summary>

    // Monitoring: https://github.com/ligershark/WebOptimizer/issues/172
    // https://stackoverflow.com/questions/32769630/how-to-compile-a-c-sharp-file-with-roslyn-programmatically?rq=4
    // https://github.com/dotnet/roslyn/issues/58540 of interest
    // https://github.com/dotnet/roslyn/issues/61655 of interest
    // https://stackoverflow.com/questions/41111826/emit-win32icon-with-roslyn maybe of interest idk?

    public class ScriptCompiler
    {
        static string ReadEmbeddedTextFile(string resourceName)
        {
            // We grab the CompilerSourceTemplate.txt contents and return it as a string
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public void Compile()
        {
            // We use the CompilerSourceTemplate.txt to hold our code in a more organized fashion NOTE: Not working with this finicky as fuck method
            string resourceName = "GyroPrompt.Compiler.CompilerSourceTemplate.txt"; // This grabs the CompilerSourceTemplate.txt contents
            string fileContents = ReadEmbeddedTextFile($"{resourceName}");
            string testContents = @"        
        using System;
        using System.Drawing;
        using System.Threading.Tasks;
        using System.Collections.Generic;
        using System.Linq;
        namespace HelloWorldprogram
        {
        internal class Program
        {
            public static void Main()
            {
                Console.WriteLine(""Hello World!"");
                for (int x = 0; x < 10; x++){
                    Console.WriteLine(x);
                }
                Console.ReadLine();
            }   
        }
        }";

            string FileName = "output_program";
            string ExePath = AppDomain.CurrentDomain.BaseDirectory + @"\" + FileName + ".exe";

            // Add all relevant references
            List<MetadataReference> References = new List<MetadataReference>();
            foreach (var item in ReferenceAssemblies.NetStandard20) 
                References.Add(item);

            // Delete the file if it already exists
            if (File.Exists(ExePath))
                File.Delete(ExePath);

            // Compiler options
            CSharpCompilationOptions DefaultCompilationOptions =
                new CSharpCompilationOptions(outputKind: OutputKind.ConsoleApplication, platform: Platform.AnyCpu)
                .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release);

            // Encode source code
            string sourceCode = SourceText.From(testContents, Encoding.UTF8).ToString();

            // CSharp options
            var parsedSyntaxTree = Parse(sourceCode, "", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));

            // finally compile the .exe file
            var compilation = CSharpCompilation.Create(FileName, new SyntaxTree[] { parsedSyntaxTree }, references: References, DefaultCompilationOptions);
            var result = compilation.Emit(ExePath);
            
            if (result.Success)
            {
                Console.WriteLine("Compiled script.");
            } else
            {
                Console.WriteLine(result.Diagnostics.ToString());
            }


            static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
            {
                var stringText = SourceText.From(text, Encoding.UTF8);
                return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
            }
        }

    }
}