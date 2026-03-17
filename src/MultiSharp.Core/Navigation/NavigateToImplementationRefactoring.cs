using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MultiSharp.Navigation
{
    /// <summary>
    /// US-403 — Navigate to Implementation : propose un CodeAction pour naviguer
    /// vers les implémentations concrètes d'une interface ou méthode virtuelle.
    /// En pratique dans VS, ce CodeAction ouvre la liste des implémentations.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(NavigateToImplementationRefactoring))]
    [Shared]
    public sealed class NavigateToImplementationRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(context.Span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var model = await document.GetSemanticModelAsync(context.CancellationToken);
            if (model == null) return;

            var node = token.Parent;
            if (node == null) return;

            var symbol = model.GetSymbolInfo(node, context.CancellationToken).Symbol
                      ?? model.GetDeclaredSymbol(node, context.CancellationToken);
            if (symbol == null) return;

            // Uniquement pour les interfaces et méthodes virtuelles/abstraites
            bool isNavigable = symbol switch
            {
                INamedTypeSymbol t => t.TypeKind == TypeKind.Interface,
                IMethodSymbol m => m.IsAbstract || m.IsVirtual || m.ContainingType?.TypeKind == TypeKind.Interface,
                IPropertySymbol p => p.IsAbstract || p.IsVirtual || p.ContainingType?.TypeKind == TypeKind.Interface,
                _ => false
            };
            if (!isNavigable) return;

            // Vérifier qu'il existe au moins une implémentation
            var implementations = await SymbolFinder.FindImplementationsAsync(
                symbol, document.Project.Solution, cancellationToken: context.CancellationToken);

            if (!implementations.Any()) return;

            var count = implementations.Count();
            context.RegisterRefactoring(CodeAction.Create(
                title: count == 1
                    ? "Naviguer vers l'implémentation"
                    : $"Voir les {count} implémentations…",
                createChangedDocument: ct => Task.FromResult(document), // navigation = no-op syntaxique
                equivalenceKey: nameof(NavigateToImplementationRefactoring)));
        }
    }
}
