using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Formatting;

namespace RoslynCSharpExtensions
{
    class Program
    {
        static void Main(string[] args)
        {
            var syntaxTree = SyntaxTree.ParseFile("code.txt");
            var root = syntaxTree.GetRoot();

            var newRoot = (CompilationUnitSyntax)(new ListsInitializerRewriter(GetSemanticsModel(syntaxTree)).Visit(root));
            newRoot.Format(new FormattingOptions(false, 4, 4));

            BuildExe(SyntaxTree.Create(newRoot));
        }

        static void BuildExe(SyntaxTree tree)
        {
            var result = GetCompilation(tree).Emit("test.exe");
            Console.WriteLine("built into test.exe with success: {0}", result.Success);
        }

        private static SemanticModel GetSemanticsModel(SyntaxTree tree)
        {
            return GetCompilation(tree).GetSemanticModel(tree);
        }

        private static Compilation GetCompilation(SyntaxTree tree)
        {
            var mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");

            var compilation = Compilation.Create(
                outputName: "HelloWorld",
                syntaxTrees: new[] { tree },
                references: new[] { mscorlib });
            return compilation;
        }
    }
}
