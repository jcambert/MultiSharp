using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MultiSharp.Tests.Helpers
{
    /// <summary>
    /// Helper pour tester les DiagnosticAnalyzer sans dépendance
    /// sur les packages Microsoft.CodeAnalysis.Testing.
    /// </summary>
    public static class AnalyzerTestHelper
    {
        private static readonly MetadataReference[] DefaultReferences =
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
        };

        /// <summary>
        /// Exécute l'analyseur sur le code source donné et retourne les diagnostics produits.
        /// </summary>
        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
            DiagnosticAnalyzer analyzer,
            string source,
            CancellationToken ct = default)
        {
            var compilation = CreateCompilation(source);
            var compilationWithAnalyzer = compilation.WithAnalyzers(
                ImmutableArray.Create(analyzer),
                options: null,
                cancellationToken: ct);

            return await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync(ct);
        }

        /// <summary>
        /// Vérifie qu'aucun diagnostic n'est produit sur le code source.
        /// </summary>
        public static async Task VerifyNoDiagnosticsAsync(DiagnosticAnalyzer analyzer, string source)
        {
            var diagnostics = await GetDiagnosticsAsync(analyzer, source);
            var filtered = diagnostics.Where(d => analyzer.SupportedDiagnostics.Any(sd => sd.Id == d.Id)).ToList();
            Xunit.Assert.Empty(filtered);
        }

        /// <summary>
        /// Vérifie qu'aucun diagnostic du type spécifié n'est produit sur le code source.
        /// </summary>
        public static async Task VerifyNoDiagnosticsAsync(
            DiagnosticAnalyzer analyzer, string source, string diagnosticId)
        {
            var diagnostics = await GetDiagnosticsAsync(analyzer, source);
            var count = diagnostics.Count(d => d.Id == diagnosticId);
            Xunit.Assert.Equal(0, count);
        }

        /// <summary>
        /// Vérifie qu'exactement les diagnostics attendus sont produits (par ID).
        /// </summary>
        public static async Task VerifyDiagnosticsAsync(
            DiagnosticAnalyzer analyzer,
            string source,
            params string[] expectedDiagnosticIds)
        {
            var diagnostics = await GetDiagnosticsAsync(analyzer, source);
            var filtered = diagnostics
                .Where(d => analyzer.SupportedDiagnostics.Any(sd => sd.Id == d.Id))
                .Select(d => d.Id)
                .OrderBy(x => x)
                .ToList();

            var expected = expectedDiagnosticIds.OrderBy(x => x).ToList();
            Xunit.Assert.Equal(expected, filtered);
        }

        /// <summary>
        /// Vérifie le nombre de diagnostics d'un ID donné.
        /// </summary>
        public static async Task VerifyDiagnosticCountAsync(
            DiagnosticAnalyzer analyzer,
            string source,
            string diagnosticId,
            int expectedCount)
        {
            var diagnostics = await GetDiagnosticsAsync(analyzer, source);
            var count = diagnostics.Count(d => d.Id == diagnosticId);
            Xunit.Assert.Equal(expectedCount, count);
        }

        private static CSharpCompilation CreateCompilation(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(SourceText.From(source));
            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { tree },
                DefaultReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
        }
    }
}
