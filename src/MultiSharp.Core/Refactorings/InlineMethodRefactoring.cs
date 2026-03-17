using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace MultiSharp.Refactorings
{
    /// <summary>
    /// US-204 — Inline Method : remplace un appel de méthode par le corps de la méthode appelée.
    /// Fonctionne uniquement sur les méthodes privées à corps simple (un seul return).
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(InlineMethodRefactoring))]
    [Shared]
    public sealed class InlineMethodRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            // Trouver l'invocation
            var invocation = token.Parent?.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();
            if (invocation == null) return;

            var model = await document.GetSemanticModelAsync(context.CancellationToken);
            if (model == null) return;

            var symbol = model.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (symbol == null) return;

            // Uniquement les méthodes privées de la même classe
            if (symbol.DeclaredAccessibility != Accessibility.Private) return;
            if (symbol.IsAbstract || symbol.IsVirtual || symbol.IsOverride) return;

            // Trouver la déclaration syntaxique
            var methodDecl = symbol.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax(context.CancellationToken))
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();
            if (methodDecl?.Body == null) return;

            // Uniquement si le corps contient un seul return ou une seule expression
            var statements = methodDecl.Body.Statements;
            if (statements.Count != 1) return;

            var stmt = statements[0];
            if (stmt is not ReturnStatementSyntax && stmt is not ExpressionStatementSyntax) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: $"Inliner '{symbol.Name}'",
                createChangedDocument: ct => InlineAsync(document, invocation, methodDecl, symbol, ct),
                equivalenceKey: nameof(InlineMethodRefactoring)));
        }

        private static async Task<Document> InlineAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            MethodDeclarationSyntax methodDecl,
            IMethodSymbol symbol,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var stmt = methodDecl.Body!.Statements[0];

            // Construire un mapping paramètre → argument
            var paramMap = symbol.Parameters
                .Zip(invocation.ArgumentList.Arguments,
                    (p, a) => (p.Name, Arg: a.Expression))
                .ToDictionary(x => x.Name, x => x.Arg);

            // Obtenir l'expression à inliner
            ExpressionSyntax? inlinedExpr = null;
            if (stmt is ReturnStatementSyntax ret)
                inlinedExpr = ret.Expression;
            else if (stmt is ExpressionStatementSyntax exprStmt)
                inlinedExpr = exprStmt.Expression;

            if (inlinedExpr == null) return document;

            // Substituer les paramètres par les arguments
            inlinedExpr = (ExpressionSyntax)new ParameterReplacer(paramMap).Visit(inlinedExpr);

            // Remplacer l'invocation par l'expression inlinée
            var newRoot = root.ReplaceNode(invocation,
                inlinedExpr.WithAdditionalAnnotations(Formatter.Annotation));

            return document.WithSyntaxRoot(newRoot);
        }

        private sealed class ParameterReplacer : CSharpSyntaxRewriter
        {
            private readonly System.Collections.Generic.Dictionary<string, ExpressionSyntax> _map;

            public ParameterReplacer(System.Collections.Generic.Dictionary<string, ExpressionSyntax> map)
            {
                _map = map;
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (_map.TryGetValue(node.Identifier.Text, out var replacement))
                    return replacement.WithTriviaFrom(node);
                return base.VisitIdentifierName(node);
            }
        }
    }
}
