using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MultiSharp.Navigation
{
    /// <summary>
    /// US-401 — Service de recherche de symboles avec matching fuzzy dans une solution.
    /// </summary>
    public static class SymbolSearchService
    {
        /// <summary>
        /// Recherche des symboles dans tous les projets de la solution.
        /// </summary>
        public static async Task<IReadOnlyList<SymbolMatch>> SearchAsync(
            Solution solution,
            string query,
            SymbolFilter filter = SymbolFilter.All,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return Array.Empty<SymbolMatch>();

            var results = new List<SymbolMatch>();

            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync(ct);
                if (compilation == null) continue;

                CollectSymbols(compilation.GlobalNamespace, query, filter, project.Name, results);
            }

            // Trier par score décroissant
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            return results;
        }

        private static void CollectSymbols(
            INamespaceSymbol ns,
            string query,
            SymbolFilter filter,
            string projectName,
            List<SymbolMatch> results)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                if (filter.HasFlag(SymbolFilter.Types))
                {
                    var score = FuzzyScore(type.Name, query);
                    if (score > 0)
                        results.Add(new SymbolMatch(type, score, projectName));
                }

                if (filter.HasFlag(SymbolFilter.Members))
                {
                    foreach (var member in type.GetMembers())
                    {
                        if (member.IsImplicitlyDeclared) continue;
                        if (member is IMethodSymbol m && m.MethodKind != MethodKind.Ordinary) continue;

                        var score = FuzzyScore(member.Name, query);
                        if (score > 0)
                            results.Add(new SymbolMatch(member, score, projectName));
                    }
                }
            }

            foreach (var nested in ns.GetNamespaceMembers())
                CollectSymbols(nested, query, filter, projectName, results);
        }

        /// <summary>
        /// Score de matching fuzzy : caractères consécutifs et début de mot valorisés.
        /// </summary>
        public static int FuzzyScore(string name, string query)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(query)) return 0;

            // Exact match
            if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase)) return 100;

            // Starts with
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 80;

            // Contains
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 60;

            // Acronym match (e.g. "GC" matches "GetCustomer")
            var acronym = GetAcronym(name);
            if (string.Equals(acronym, query, StringComparison.OrdinalIgnoreCase)) return 50;

            // Subsequence match
            return IsSubsequence(name, query) ? 30 : 0;
        }

        private static string GetAcronym(string name)
        {
            var result = new System.Text.StringBuilder();
            foreach (var c in name)
                if (char.IsUpper(c))
                    result.Append(c);
            return result.ToString();
        }

        private static bool IsSubsequence(string name, string query)
        {
            int j = 0;
            foreach (var c in name)
            {
                if (j < query.Length && char.ToLower(c) == char.ToLower(query[j]))
                    j++;
            }
            return j == query.Length;
        }
    }

    public sealed class SymbolMatch
    {
        public ISymbol Symbol { get; }
        public int Score { get; }
        public string ProjectName { get; }

        public SymbolMatch(ISymbol symbol, int score, string projectName)
        {
            Symbol = symbol;
            Score = score;
            ProjectName = projectName;
        }
    }

    [Flags]
    public enum SymbolFilter
    {
        Types = 1,
        Members = 2,
        All = Types | Members
    }
}
