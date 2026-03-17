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
    /// US-208 — Convert to Static : convertit une méthode d'instance en méthode statique
    /// quand elle n'accède pas aux membres d'instance (this).
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertToStaticRefactoring))]
    [Shared]
    public sealed class ConvertToStaticRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var methodDecl = token.Parent as MethodDeclarationSyntax;
            if (methodDecl == null) return;
            if (methodDecl.Identifier != token) return;

            // Déjà statique ?
            if (methodDecl.Modifiers.Any(SyntaxKind.StaticKeyword)) return;

            // override/virtual/abstract ne peuvent pas être statiques
            if (methodDecl.Modifiers.Any(SyntaxKind.OverrideKeyword)
                || methodDecl.Modifiers.Any(SyntaxKind.VirtualKeyword)
                || methodDecl.Modifiers.Any(SyntaxKind.AbstractKeyword)) return;

            if (methodDecl.Body == null && methodDecl.ExpressionBody == null) return;

            var model = await document.GetSemanticModelAsync(context.CancellationToken);
            if (model == null) return;

            // Vérifier que la méthode n'utilise pas 'this'
            if (UsesThis(methodDecl, model, context.CancellationToken)) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: $"Convertir '{methodDecl.Identifier.Text}' en méthode statique",
                createChangedDocument: ct => MakeStaticAsync(document, methodDecl, ct),
                equivalenceKey: nameof(ConvertToStaticRefactoring)));
        }

        private static bool UsesThis(
            MethodDeclarationSyntax methodDecl,
            SemanticModel model,
            CancellationToken ct)
        {
            // Vérifier les accès 'this.xxx' explicites
            var thisAccesses = methodDecl.DescendantNodes()
                .OfType<ThisExpressionSyntax>();
            if (thisAccesses.Any()) return true;

            // Vérifier les membres d'instance accédés implicitement
            var identifiers = methodDecl.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var id in identifiers)
            {
                if (id.Parent is MemberAccessExpressionSyntax ma && ma.Expression == id)
                    continue; // accès qualifié, pas implicite

                var symbolInfo = model.GetSymbolInfo(id, ct);
                var symbol = symbolInfo.Symbol;
                if (symbol == null) continue;

                if (symbol.IsStatic) continue;

                if (symbol.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method or SymbolKind.Event)
                    return true;
            }

            return false;
        }

        private static async Task<Document> MakeStaticAsync(
            Document document,
            MethodDeclarationSyntax methodDecl,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var staticToken = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            var newModifiers = methodDecl.Modifiers.Add(staticToken);
            var newMethod = methodDecl.WithModifiers(newModifiers)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(methodDecl, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
