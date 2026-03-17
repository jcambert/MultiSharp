using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MultiSharp.Navigation
{
    /// <summary>
    /// US-404 — Structural Search : recherche de patterns de code sémantiques.
    /// Supporte des patterns simples avec variables $x$ et wildcards.
    /// </summary>
    public static class StructuralSearchService
    {
        /// <summary>
        /// Recherche un pattern syntaxique dans tous les documents d'une solution.
        /// Le pattern peut contenir $identifier$ comme wildcard.
        /// Exemples :
        ///   "$x$ == null"  → toute comparaison == null
        ///   "$x$.ToString()"  → tout appel ToString()
        /// </summary>
        public static async Task<IReadOnlyList<StructuralMatch>> SearchAsync(
            Solution solution,
            string pattern,
            CancellationToken ct = default)
        {
            var results = new List<StructuralMatch>();
            var patternTree = CSharpSyntaxTree.ParseText(pattern);
            var patternRoot = patternTree.GetRoot(ct);
            // Préférer la première ExpressionSyntax (plus précise qu'un StatementSyntax englobant)
            var patternNode = (SyntaxNode?)patternRoot.DescendantNodes().OfType<ExpressionSyntax>().FirstOrDefault()
                ?? patternRoot.DescendantNodes().OfType<StatementSyntax>().FirstOrDefault();

            if (patternNode == null) return results;

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var root = await document.GetSyntaxRootAsync(ct);
                    if (root == null) continue;

                    foreach (var node in root.DescendantNodesAndSelf())
                    {
                        if (node.GetType() != patternNode.GetType()) continue;

                        if (MatchesPattern(node, patternNode))
                        {
                            var lineSpan = node.GetLocation().GetLineSpan();
                            results.Add(new StructuralMatch(
                                document: document,
                                node: node,
                                line: lineSpan.StartLinePosition.Line + 1,
                                column: lineSpan.StartLinePosition.Character + 1));
                        }
                    }
                }
            }

            return results;
        }

        private static bool MatchesPattern(SyntaxNode candidate, SyntaxNode pattern)
        {
            // Variable de pattern : $identifier$ → identifiant seul = wildcard
            if (pattern is IdentifierNameSyntax id
                && id.Identifier.Text.StartsWith("$")
                && id.Identifier.Text.EndsWith("$"))
                return true;

            if (candidate.GetType() != pattern.GetType())
                return false;

            var candidateChildren = candidate.ChildNodesAndTokens().ToList();
            var patternChildren = pattern.ChildNodesAndTokens().ToList();

            if (candidateChildren.Count != patternChildren.Count)
                return false;

            for (int i = 0; i < candidateChildren.Count; i++)
            {
                var cc = candidateChildren[i];
                var pc = patternChildren[i];

                if (cc.IsToken && pc.IsToken)
                {
                    var ct2 = cc.AsToken();
                    var pt2 = pc.AsToken();
                    if (pt2.Kind() != SyntaxKind.None && ct2.Kind() != pt2.Kind())
                        return false;
                    // Pour les identifiants wildcard ($x$) : pas de comparaison de valeur
                    if (ct2.IsKind(SyntaxKind.IdentifierToken)
                        && pt2.IsKind(SyntaxKind.IdentifierToken)
                        && pt2.Text.StartsWith("$"))
                        continue;
                    // Pour tous les autres tokens : comparer le texte exactement
                    if (ct2.Text != pt2.Text) return false;
                }
                else if (cc.IsNode && pc.IsNode)
                {
                    if (!MatchesPattern(cc.AsNode()!, pc.AsNode()!))
                        return false;
                }
            }

            return true;
        }
    }

    public sealed class StructuralMatch
    {
        public Document Document { get; }
        public SyntaxNode Node { get; }
        public int Line { get; }
        public int Column { get; }

        public StructuralMatch(Document document, SyntaxNode node, int line, int column)
        {
            Document = document;
            Node = node;
            Line = line;
            Column = column;
        }

        public override string ToString() =>
            $"{Document.Name}({Line},{Column}): {Node.ToFullString().Trim()}";
    }
}
