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

namespace MultiSharp.Advanced
{
    /// <summary>
    /// US-602 — Postfix Templates : transforme une expression sélectionnée via des templates
    /// .if, .var, .foreach, .null.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PostfixTemplateRefactoring))]
    [Shared]
    public sealed class PostfixTemplateRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            if (span.IsEmpty) return;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var node = root.FindNode(span);
            var expression = node as ExpressionSyntax
                ?? node.DescendantNodesAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();
            if (expression == null) return;

            // L'expression doit être dans un statement
            if (expression.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault() == null) return;

            var model = await document.GetSemanticModelAsync(context.CancellationToken);
            if (model == null) return;

            var typeInfo = model.GetTypeInfo(expression, context.CancellationToken);

            // .if — if (expr) { }
            context.RegisterRefactoring(CodeAction.Create(
                title: $"Postfix .if — if ({expression}) {{ }}",
                createChangedDocument: ct => ApplyIfTemplateAsync(document, expression, ct),
                equivalenceKey: "PostfixIf"));

            // .var — var name = expr;
            context.RegisterRefactoring(CodeAction.Create(
                title: $"Postfix .var — var name = {expression};",
                createChangedDocument: ct => ApplyVarTemplateAsync(document, expression, typeInfo.Type, ct),
                equivalenceKey: "PostfixVar"));

            // .null — if (expr == null) { }
            context.RegisterRefactoring(CodeAction.Create(
                title: $"Postfix .null — if ({expression} == null) {{ }}",
                createChangedDocument: ct => ApplyNullTemplateAsync(document, expression, ct),
                equivalenceKey: "PostfixNull"));

            // .foreach — foreach (var item in expr) { }  (si enumerable)
            if (IsEnumerable(typeInfo.Type))
            {
                context.RegisterRefactoring(CodeAction.Create(
                    title: $"Postfix .foreach — foreach (var item in {expression}) {{ }}",
                    createChangedDocument: ct => ApplyForeachTemplateAsync(document, expression, ct),
                    equivalenceKey: "PostfixForeach"));
            }
        }

        private static bool IsEnumerable(ITypeSymbol? type)
        {
            if (type == null) return false;
            return type.AllInterfaces.Any(i =>
                i.Name == "IEnumerable" && i.ContainingNamespace?.Name == "Collections");
        }

        private static StatementSyntax? GetContainingStatement(ExpressionSyntax expression) =>
            expression.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();

        private static async Task<Document> ApplyIfTemplateAsync(
            Document document, ExpressionSyntax expr, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;
            var stmt = GetContainingStatement(expr);
            if (stmt == null) return document;

            var ifStmt = SyntaxFactory.IfStatement(
                expr,
                SyntaxFactory.Block())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(stmt, ifStmt.WithTriviaFrom(stmt));
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> ApplyVarTemplateAsync(
            Document document, ExpressionSyntax expr, ITypeSymbol? type, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;
            var stmt = GetContainingStatement(expr);
            if (stmt == null) return document;

            var varName = type != null ? SuggestName(type.Name) : "value";
            var varDecl = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(varName),
                            null,
                            SyntaxFactory.EqualsValueClause(expr)))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(stmt, varDecl.WithTriviaFrom(stmt));
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> ApplyNullTemplateAsync(
            Document document, ExpressionSyntax expr, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;
            var stmt = GetContainingStatement(expr);
            if (stmt == null) return document;

            var condition = SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                expr,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

            var ifStmt = SyntaxFactory.IfStatement(condition, SyntaxFactory.Block())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(stmt, ifStmt.WithTriviaFrom(stmt));
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> ApplyForeachTemplateAsync(
            Document document, ExpressionSyntax expr, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;
            var stmt = GetContainingStatement(expr);
            if (stmt == null) return document;

            var foreachStmt = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier("item"),
                expr,
                SyntaxFactory.Block())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(stmt, foreachStmt.WithTriviaFrom(stmt));
            return document.WithSyntaxRoot(newRoot);
        }

        private static string SuggestName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return "value";
            return char.ToLower(typeName[0]) + typeName.Substring(1);
        }
    }
}
