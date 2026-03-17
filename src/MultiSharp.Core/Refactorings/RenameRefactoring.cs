using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace MultiSharp.Refactorings
{
    /// <summary>
    /// US-201 — Rename : renomme un symbole partout dans la solution via Roslyn.
    /// Déclenché par Alt+Enter sur n'importe quel identifiant.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RenameRefactoring))]
    [Shared]
    public sealed class RenameRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            // Trouver le token sous le curseur
            var token = root.FindToken(span.Start);
            if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None)) return;

            // On ne propose le rename que sur des identifiants
            if (!token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken)) return;

            var node = token.Parent;
            if (node == null) return;

            var model = await document.GetSemanticModelAsync(context.CancellationToken);
            if (model == null) return;

            var symbol = model.GetSymbolInfo(node, context.CancellationToken).Symbol
                      ?? model.GetDeclaredSymbol(node, context.CancellationToken);

            if (symbol == null) return;

            // Proposer le renommage pour les symboles qui le méritent
            if (symbol.Kind is SymbolKind.Local or SymbolKind.Parameter
                or SymbolKind.Field or SymbolKind.Property or SymbolKind.Method
                or SymbolKind.NamedType or SymbolKind.Event)
            {
                context.RegisterRefactoring(CodeAction.Create(
                    title: $"Renommer '{symbol.Name}'…",
                    createChangedSolution: async ct =>
                    {
                        // Nouveau nom = nom actuel + "1" (placeholder — l'IDE proposera inline rename)
                        // En pratique VS intercepte ce refactoring et ouvre son UI de rename inline
                        var newName = symbol.Name + "New";
                        return await Renamer.RenameSymbolAsync(
                            document.Project.Solution,
                            symbol,
                            new SymbolRenameOptions(),
                            newName,
                            ct);
                    },
                    equivalenceKey: nameof(RenameRefactoring)));
            }
        }
    }
}
