using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MultiSharp.Tests.Services
{
    /// <summary>
    /// Tests de la logique Roslyn via CSharpCompilation (pas de dépendance MEF/VS).
    /// Valide que l'infrastructure d'analyse de code fonctionne correctement
    /// avant d'être intégrée dans le service VS.
    /// </summary>
    public class RoslynWorkspaceServiceTests
    {
        private static CSharpCompilation CreateCompilation(string source, string assemblyName = "TestAssembly")
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        [Fact]
        public void SyntaxTree_EstParseable_DepuisSourceCode()
        {
            var source = "class Foo { void Bar() {} }";
            var tree = CSharpSyntaxTree.ParseText(source);

            Assert.NotNull(tree);
            Assert.Equal(source, tree.GetText().ToString());
        }

        [Fact]
        public void Compilation_ContientLesBonsSymboles()
        {
            var compilation = CreateCompilation("public class MyClass { public void MyMethod() {} }");

            var myClass = compilation.GlobalNamespace
                .GetTypeMembers()
                .FirstOrDefault(t => t.Name == "MyClass");

            Assert.NotNull(myClass);
            Assert.Equal("MyClass", myClass!.Name);
        }

        [Fact]
        public async System.Threading.Tasks.Task SemanticModel_EstAccessible_DepuisUneSyntaxTree()
        {
            var source = "public class Foo { public int X { get; set; } }";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.First();

            var semanticModel = compilation.GetSemanticModel(tree);
            Assert.NotNull(semanticModel);

            var root = await tree.GetRootAsync(CancellationToken.None);
            Assert.NotNull(root);
        }

        [Fact]
        public void Diagnostics_SontDetectes_SurCodeInvalide()
        {
            var source = "public class Foo { public int X = undeclaredVariable; }";
            var compilation = CreateCompilation(source);

            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void Compilation_SansErreur_SurCodeValide()
        {
            var source = "public class Foo { public int Add(int a, int b) => a + b; }";
            var compilation = CreateCompilation(source);

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.Empty(errors);
        }
    }
}
