using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace MultiSharp.Tests.Helpers
{
    public static class SolutionTestHelper
    {
        public static Solution CreateSolution(string sourceCode, string documentName = "Test.cs")
        {
            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var docId = DocumentId.CreateNewId(projectId);

            var solution = workspace.CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(projectId, GetReferences())
                .AddDocument(docId, documentName, SourceText.From(sourceCode));

            return solution;
        }

        private static ImmutableArray<MetadataReference> GetReferences() =>
            ImmutableArray.Create<MetadataReference>(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
            );
    }
}
