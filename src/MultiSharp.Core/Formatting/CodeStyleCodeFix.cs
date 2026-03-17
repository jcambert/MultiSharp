using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using MultiSharp.Analyzers;

namespace MultiSharp.Formatting
{
    /// <summary>
    /// US-503 — Quick Fix pour les suggestions de style :
    /// - Remplace le type explicite par var
    /// - Convertit une méthode à un return en expression body
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeStyleCodeFix))]
    [Shared]
    public sealed class CodeStyleCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticIds.UseVarKeyword,
            DiagnosticIds.UseExpressionBody);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);

                if (diagnostic.Id == DiagnosticIds.UseVarKeyword)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: "Utiliser 'var'",
                        createChangedDocument: ct => UseVarAsync(context.Document, node, ct),
                        equivalenceKey: "UseVar"),
                        diagnostic);
                }
                else if (diagnostic.Id == DiagnosticIds.UseExpressionBody)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: "Convertir en expression body",
                        createChangedDocument: ct => UseExprBodyAsync(context.Document, node, ct),
                        equivalenceKey: "UseExprBody"),
                        diagnostic);
                }
            }
        }

        private static async Task<Document> UseVarAsync(
            Document document,
            SyntaxNode typeNode,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var varType = SyntaxFactory.IdentifierName("var")
                .WithTriviaFrom(typeNode);

            var newRoot = root.ReplaceNode(typeNode, varType);
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> UseExprBodyAsync(
            Document document,
            SyntaxNode identifierNode,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            // Trouver la méthode parente
            var method = identifierNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method?.Body == null) return document;

            var stmt = method.Body.Statements[0];
            ExpressionSyntax? expr = null;

            if (stmt is ReturnStatementSyntax ret)
                expr = ret.Expression;
            else if (stmt is ExpressionStatementSyntax exprStmt)
                expr = exprStmt.Expression;

            if (expr == null) return document;

            var newMethod = method
                .WithBody(null)
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(expr))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
