using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace MultiSharp.Tests.Helpers
{
    /// <summary>
    /// Helper pour tester les CodeRefactoringProvider sans dépendances de test externes.
    /// </summary>
    public static class RefactoringTestHelper
    {
        /// <summary>
        /// Vérifie qu'au moins un refactoring est proposé.
        /// </summary>
        public static async Task<bool> HasRefactoringAsync(
            CodeRefactoringProvider provider,
            string source,
            CancellationToken ct = default)
        {
            var (document, span) = CreateDocumentWithSpan(source);
            bool found = false;

            var context = new CodeRefactoringContext(document, span, _ => { found = true; }, ct);
            await provider.ComputeRefactoringsAsync(context);
            return found;
        }

        private static (Document document, TextSpan span) CreateDocumentWithSpan(string source)
        {
            // Enlever les marqueurs [| et |] pour obtenir le vrai code
            var startMarker = source.IndexOf("[|");
            var endMarker = source.IndexOf("|]");

            TextSpan span;
            string cleanSource;
            if (startMarker >= 0 && endMarker > startMarker)
            {
                cleanSource = source.Replace("[|", "").Replace("|]", "");
                span = TextSpan.FromBounds(startMarker, endMarker - 2); // -2 pour les 2 chars supprimés
            }
            else
            {
                cleanSource = source;
                span = new TextSpan(0, 0);
            }

            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var docId = DocumentId.CreateNewId(projectId);

            var solution = workspace.CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(projectId, GetReferences())
                .AddDocument(docId, "Test.cs", SourceText.From(cleanSource));

            var document = solution.GetDocument(docId)!;
            return (document, span);
        }

        private static ImmutableArray<MetadataReference> GetReferences()
        {
            return ImmutableArray.Create<MetadataReference>(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
            );
        }
    }
}
