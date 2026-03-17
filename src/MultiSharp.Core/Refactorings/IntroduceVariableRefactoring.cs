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
    /// US-205 — Introduce Variable : extrait l'expression sélectionnée dans une variable locale.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(IntroduceVariableRefactoring))]
    [Shared]
    public sealed class IntroduceVariableRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            if (span.IsEmpty) return;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            // Trouver l'expression couverte par la sélection
            var node = root.FindNode(span);
            var expression = node as ExpressionSyntax
                ?? node.DescendantNodesAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();

            if (expression == null) return;

            // Ignorer les expressions trop simples (littéraux, identifiants simples)
            if (expression is LiteralExpressionSyntax) return;
            if (expression is IdentifierNameSyntax) return;

            // L'expression doit être dans un statement
            var containingStatement = expression.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
            if (containingStatement == null) return;

            if (containingStatement.Parent is not BlockSyntax) return;

            // Doit être dans une méthode
            var containingMethod = expression.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod == null) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: "Introduire une variable…",
                createChangedDocument: ct => IntroduceAsync(document, expression, containingStatement, ct),
                equivalenceKey: nameof(IntroduceVariableRefactoring)));
        }

        private static async Task<Document> IntroduceAsync(
            Document document,
            ExpressionSyntax expression,
            StatementSyntax containingStatement,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var model = await document.GetSemanticModelAsync(ct);

            // Déterminer le type de la variable
            TypeSyntax varType = SyntaxFactory.IdentifierName("var");
            if (model != null)
            {
                var typeInfo = model.GetTypeInfo(expression, ct);
                if (typeInfo.Type != null && typeInfo.Type.SpecialType != SpecialType.System_Void)
                {
                    varType = SyntaxFactory.ParseTypeName(
                        typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }
            }

            var varName = "extractedValue";

            // Créer : var extractedValue = <expression>;
            var newDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(varType,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(varName),
                            null,
                            SyntaxFactory.EqualsValueClause(expression)))))
                .WithTriviaFrom(containingStatement)
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Remplacer l'expression dans le statement original par le nom de la variable
            var newStatement = containingStatement.ReplaceNode(
                expression,
                SyntaxFactory.IdentifierName(varName));

            // Remplacer dans le bloc parent
            var block = (BlockSyntax)containingStatement.Parent!;
            var stmtIndex = block.Statements.IndexOf(containingStatement);
            var newBlock = block.WithStatements(
                block.Statements
                    .Replace(containingStatement, newStatement)
                    .Insert(stmtIndex, newDeclaration));

            var newRoot = root.ReplaceNode(block, newBlock);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
