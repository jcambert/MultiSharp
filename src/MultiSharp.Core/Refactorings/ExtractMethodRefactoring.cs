using System.Collections.Generic;
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
    /// US-202 — Extract Method : extrait les instructions sélectionnées en une nouvelle méthode.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ExtractMethodRefactoring))]
    [Shared]
    public sealed class ExtractMethodRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            if (span.IsEmpty) return;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            // Trouver les statements couverts par la sélection
            var selectedStatements = root.DescendantNodes(span)
                .OfType<StatementSyntax>()
                .Where(s => span.Contains(s.Span) && s.Parent is BlockSyntax)
                .ToList();

            if (selectedStatements.Count == 0) return;

            // Tous les statements doivent avoir le même parent
            var parent = selectedStatements[0].Parent as BlockSyntax;
            if (parent == null || selectedStatements.Any(s => s.Parent != parent)) return;

            // Trouver la méthode contenante
            var containingMethod = selectedStatements[0]
                .AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();
            if (containingMethod == null) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: "Extraire la méthode…",
                createChangedDocument: ct => ExtractAsync(document, containingMethod,
                    selectedStatements, ct),
                equivalenceKey: nameof(ExtractMethodRefactoring)));
        }

        private static readonly SyntaxAnnotation s_removeAnnotation = new SyntaxAnnotation("MultiSharp_Remove");

        private static async Task<Document> ExtractAsync(
            Document document,
            MethodDeclarationSyntax containingMethod,
            List<StatementSyntax> statements,
            CancellationToken ct)
        {
            var model = await document.GetSemanticModelAsync(ct);
            if (model == null) return document;

            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            // Analyser le flux de données pour déterminer les paramètres nécessaires
            var firstStmt = statements.First();
            var lastStmt = statements.Last();
            var block = (BlockSyntax)firstStmt.Parent!;

            DataFlowAnalysis? dataFlow = null;
            if (statements.Count == 1)
                dataFlow = model.AnalyzeDataFlow(firstStmt);
            else
                dataFlow = model.AnalyzeDataFlow(firstStmt, lastStmt);

            if (dataFlow == null || !dataFlow.Succeeded) return document;

            // Variables lues dans le bloc extrait mais déclarées à l'extérieur → paramètres
            var parameters = dataFlow.DataFlowsIn
                .Where(s => s.Kind is SymbolKind.Local or SymbolKind.Parameter)
                .Select(s => SyntaxFactory.Parameter(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(),
                    GetTypeSyntax(s),
                    SyntaxFactory.Identifier(s.Name),
                    null))
                .ToList();

            // Variables écrites dans le bloc et utilisées après → type de retour
            var writtenAndUsedAfter = dataFlow.WrittenInside
                .Where(s => dataFlow.DataFlowsOut.Contains(s))
                .ToList();

            TypeSyntax returnType = writtenAndUsedAfter.Count == 1
                ? GetTypeSyntax(writtenAndUsedAfter[0])
                : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));

            var newMethodName = "ExtractedMethod";
            var newBody = SyntaxFactory.Block(statements);

            // Ajouter un return si nécessaire
            if (writtenAndUsedAfter.Count == 1)
            {
                var retStmt = SyntaxFactory.ReturnStatement(
                    SyntaxFactory.IdentifierName(writtenAndUsedAfter[0].Name));
                newBody = newBody.AddStatements(retStmt);
            }

            // Construire la nouvelle méthode
            var newMethod = SyntaxFactory.MethodDeclaration(returnType, newMethodName)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(parameters)))
                .WithBody(newBody)
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Construire l'appel à la nouvelle méthode
            var args = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    parameters.Select(p => SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(p.Identifier.Text)))));

            StatementSyntax callStatement;
            if (writtenAndUsedAfter.Count == 1)
            {
                callStatement = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(writtenAndUsedAfter[0].Name),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.IdentifierName(newMethodName), args))))));
            }
            else
            {
                callStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(newMethodName), args));
            }

            // Remplacer les statements sélectionnés par l'appel
            var newBlock = block.ReplaceNodes(statements,
                (original, _) => original == statements[0]
                    ? callStatement.WithTriviaFrom(statements[0])
                    : (SyntaxNode)SyntaxFactory.EmptyStatement()
                        .WithAdditionalAnnotations(s_removeAnnotation));

            // Retirer les EmptyStatements ajoutés
            newBlock = newBlock.RemoveNodes(
                newBlock.Statements.OfType<EmptyStatementSyntax>()
                    .Where(e => e.HasAnnotation(s_removeAnnotation)),
                SyntaxRemoveOptions.KeepNoTrivia) ?? newBlock;

            // Insérer la nouvelle méthode après la méthode contenante
            var typeDecl = containingMethod.Parent as TypeDeclarationSyntax;
            if (typeDecl == null) return document;

            var newContainingMethod = containingMethod.ReplaceNode(block, newBlock);

            var newTypeDecl = typeDecl
                .ReplaceNode(containingMethod, newContainingMethod);

            // Trouver la position après la méthode et insérer
            var updatedMethod = newTypeDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == containingMethod.Identifier.Text);

            var idx = newTypeDecl.Members.IndexOf(updatedMethod);
            var members = newTypeDecl.Members.Insert(idx + 1, newMethod);
            newTypeDecl = newTypeDecl.WithMembers(members);

            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        private static TypeSyntax GetTypeSyntax(ISymbol symbol)
        {
            var typeName = symbol switch
            {
                ILocalSymbol local => local.Type.ToDisplayString(
                    SymbolDisplayFormat.MinimallyQualifiedFormat),
                IParameterSymbol param => param.Type.ToDisplayString(
                    SymbolDisplayFormat.MinimallyQualifiedFormat),
                _ => "object"
            };
            return SyntaxFactory.ParseTypeName(typeName);
        }
    }
}
