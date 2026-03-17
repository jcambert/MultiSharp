using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace MultiSharp.Formatting
{
    /// <summary>
    /// US-501 — Service de formatage automatique via Roslyn Formatter.
    /// Utilisé à la sauvegarde ou sur demande.
    /// </summary>
    public static class CodeFormatterService
    {
        /// <summary>
        /// Formate l'intégralité d'un document selon les règles Roslyn par défaut.
        /// </summary>
        public static async Task<Document> FormatDocumentAsync(
            Document document,
            CancellationToken ct = default)
        {
            return await Formatter.FormatAsync(document, cancellationToken: ct);
        }

        /// <summary>
        /// Formate uniquement la plage de texte spécifiée dans le document.
        /// </summary>
        public static async Task<Document> FormatRangeAsync(
            Document document,
            Microsoft.CodeAnalysis.Text.TextSpan span,
            CancellationToken ct = default)
        {
            return await Formatter.FormatAsync(document, span, cancellationToken: ct);
        }

        /// <summary>
        /// Normalise les espaces, indentation et sauts de ligne d'un nœud syntaxique.
        /// Utile pour les tests sans workspace complet.
        /// </summary>
        public static SyntaxNode NormalizeWhitespace(SyntaxNode node) =>
            node.NormalizeWhitespace();
    }
}
