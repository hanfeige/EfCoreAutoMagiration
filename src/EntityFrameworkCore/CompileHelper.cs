using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Kings.EntityFrameworkCore.AutoMigration
{
    public static class CompileHelper
    {
        public static Assembly Compile(string text, params Assembly[] referencedAssemblies)
        {
            var references = referencedAssemblies.Select(it => MetadataReference.CreateFromFile(it.Location));
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var assemblyName = @"_" + Guid.NewGuid().ToString("D");
            var syntaxTrees = new SyntaxTree[] { CSharpSyntaxTree.ParseText(text) };
            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, options);
            using var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream);
            if (compilationResult.Success)
            {
                stream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(stream.ToArray());
            }
            throw new InvalidOperationException(compilationResult.Diagnostics.Length > 0 ? compilationResult.Diagnostics[0].ToString() : "Compilation error");
        }
        public static Assembly[] GetCompileAssemblies(params Type[] baseTypes)
        {
            var references = new List<Assembly>()
                .AddForNameIfNotExtist(Assembly.Load(new AssemblyName("netstandard")))
                .AddForNameIfNotExtist(Assembly.Load(new AssemblyName("System.Runtime")))
                .AddForNameIfNotExtist(typeof(object).Assembly)

                .AddForNameIfNotExtist(baseTypes.Select(type => type.Assembly).ToArray());
            return references.ToArray();
        }
        public static List<Assembly> AddForNameIfNotExtist(this List<Assembly> referencedAssemblies, params Assembly[] assemblys)
        {
            foreach (var assembly in assemblys)
            {
                if (referencedAssemblies.All(ass => assembly.FullName != ass.FullName))
                {
                    referencedAssemblies.Add(assembly);
                }
            }
            return referencedAssemblies;
        }
    }
}
