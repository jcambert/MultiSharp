using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MultiSharp.Analyzers;

namespace MultiSharp.CodeFixes
{
    /// <summary>
    /// Quick Fix pour MS0101 — Supprime la déclaration de variable inutilisée.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedLocalVariableCodeFix))]
    [Shared]
    public sealed class UnusedLocalVariableCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticIds.UnusedLocalVariable);

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            var declaration = token.Parent?.AncestorsAndSelf()
                .OfType<LocalDeclarationStatementSyntax>()
                .FirstOrDefault();

            if (declaration == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Supprimer la variable inutilisée",
                    createChangedDocument: ct => RemoveDeclarationAsync(context.Document, declaration, ct),
                    equivalenceKey: nameof(UnusedLocalVariableCodeFix)),
                diagnostic);

            // Deuxième option : préfixer par _ (convention discard)
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Préfixer par '_' (discard)",
                    createChangedDocument: ct => PrefixWithUnderscoreAsync(context.Document, token, ct),
                    equivalenceKey: nameof(UnusedLocalVariableCodeFix) + "_prefix"),
                diagnostic);
        }

        private static async Task<Document> RemoveDeclarationAsync(
            Document document,
            LocalDeclarationStatementSyntax declaration,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var newRoot = root.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);
            return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> PrefixWithUnderscoreAsync(
            Document document,
            SyntaxToken identifier,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var newIdentifier = Microsoft.CodeAnalysis.CSharp.SyntaxFactory
                .Identifier("_" + identifier.Text)
                .WithTriviaFrom(identifier);

            var newRoot = root.ReplaceToken(identifier, newIdentifier);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
