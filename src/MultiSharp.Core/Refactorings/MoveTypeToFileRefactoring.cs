using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MultiSharp.Refactorings
{
    /// <summary>
    /// US-207 — Move Type to File : déplace un type dans un nouveau fichier portant son nom.
    /// Proposé uniquement quand le fichier contient plusieurs types au niveau namespace/top-level.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MoveTypeToFileRefactoring))]
    [Shared]
    public sealed class MoveTypeToFileRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            // Trouver la déclaration de type (class/struct/enum/interface/record)
            var typeDecl = token.Parent as BaseTypeDeclarationSyntax;
            if (typeDecl == null) return;
            if (typeDecl.Identifier != token) return;

            // Vérifier qu'il y a d'autres types dans le même parent
            var siblings = typeDecl.Parent?.ChildNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(t => t != typeDecl)
                .ToList();
            if (siblings == null || siblings.Count == 0) return;

            var typeName = typeDecl.Identifier.Text;

            context.RegisterRefactoring(CodeAction.Create(
                title: $"Déplacer '{typeName}' dans '{typeName}.cs'…",
                createChangedSolution: ct => MoveTypeAsync(document, typeDecl, ct),
                equivalenceKey: nameof(MoveTypeToFileRefactoring)));
        }

        private static async Task<Solution> MoveTypeAsync(
            Document document,
            BaseTypeDeclarationSyntax typeDecl,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document.Project.Solution;

            var typeName = typeDecl.Identifier.Text;

            // Récupérer les usings du fichier source
            var compilationUnit = root as CompilationUnitSyntax;
            var usings = compilationUnit?.Usings ?? default;

            // Construire le contenu du nouveau fichier
            SyntaxNode newFileRoot;
            if (typeDecl.Parent is NamespaceDeclarationSyntax nsDecl)
            {
                var newNs = SyntaxFactory.NamespaceDeclaration(nsDecl.Name)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl));
                newFileRoot = SyntaxFactory.CompilationUnit()
                    .WithUsings(usings)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(newNs));
            }
            else
            {
                newFileRoot = SyntaxFactory.CompilationUnit()
                    .WithUsings(usings)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl));
            }

            // Supprimer le type du fichier source
            var newRoot = root.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia);
            if (newRoot == null) return document.Project.Solution;

            var solution = document.Project.Solution;

            // Mettre à jour le document source
            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);

            // Ajouter le nouveau document
            var newDocId = DocumentId.CreateNewId(document.Project.Id);
            solution = solution.AddDocument(newDocId, $"{typeName}.cs",
                SourceText.From(newFileRoot.ToFullString()),
                folders: document.Folders);

            return solution;
        }
    }
}
