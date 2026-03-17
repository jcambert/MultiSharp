using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MultiSharp.Navigation
{
    /// <summary>
    /// US-402 — Service Find Usages via Roslyn SymbolFinder.
    /// </summary>
    public static class FindUsagesService
    {
        /// <summary>
        /// Trouve toutes les références à un symbole dans la solution.
        /// </summary>
        public static async Task<IReadOnlyList<UsageLocation>> FindUsagesAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken ct = default)
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, ct);
            var results = new List<UsageLocation>();

            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    var lineSpan = location.Location.GetLineSpan();
                    results.Add(new UsageLocation(
                        projectName: location.Document.Project.Name,
                        filePath: location.Document.FilePath ?? location.Document.Name,
                        line: lineSpan.StartLinePosition.Line + 1,
                        column: lineSpan.StartLinePosition.Character + 1,
                        isWrite: false));
                }
            }

            return results.OrderBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
        }

        /// <summary>
        /// Trouve toutes les implémentations d'un symbole d'interface ou méthode virtuelle.
        /// </summary>
        public static async Task<IReadOnlyList<ISymbol>> FindImplementationsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken ct = default)
        {
            var implementations = await SymbolFinder.FindImplementationsAsync(symbol, solution, cancellationToken: ct);
            return implementations.ToList();
        }

        /// <summary>
        /// Trouve tous les types dérivés d'un type de base.
        /// </summary>
        public static async Task<IReadOnlyList<INamedTypeSymbol>> FindDerivedTypesAsync(
            INamedTypeSymbol baseType,
            Solution solution,
            CancellationToken ct = default)
        {
            var derived = await SymbolFinder.FindDerivedClassesAsync(baseType, solution, cancellationToken: ct);
            return derived.ToList();
        }
    }

    public sealed class UsageLocation
    {
        public string ProjectName { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public bool IsWrite { get; }

        public UsageLocation(string projectName, string filePath, int line, int column, bool isWrite)
        {
            ProjectName = projectName;
            FilePath = filePath;
            Line = line;
            Column = column;
            IsWrite = isWrite;
        }

        public override string ToString() =>
            $"{System.IO.Path.GetFileName(FilePath)}({Line},{Column}) [{(IsWrite ? "write" : "read")}]";
    }
}
