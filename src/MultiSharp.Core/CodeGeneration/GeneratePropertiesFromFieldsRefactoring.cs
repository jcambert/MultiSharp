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

namespace MultiSharp.CodeGeneration
{
    /// <summary>
    /// US-304 — Génère des propriétés publiques pour les champs privés non encapsulés.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GeneratePropertiesFromFieldsRefactoring))]
    [Shared]
    public sealed class GeneratePropertiesFromFieldsRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(context.Span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var classDecl = token.Parent as ClassDeclarationSyntax;
            if (classDecl == null || classDecl.Identifier != token) return;

            var unencapsulated = GetUnencapsulatedFields(classDecl);
            if (unencapsulated.Count == 0) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: $"Générer les propriétés pour les champs privés ({unencapsulated.Count})…",
                createChangedDocument: ct => GenerateAsync(context.Document, classDecl, unencapsulated, ct),
                equivalenceKey: nameof(GeneratePropertiesFromFieldsRefactoring)));
        }

        private static List<(TypeSyntax Type, string FieldName, string PropName)> GetUnencapsulatedFields(
            ClassDeclarationSyntax classDecl)
        {
            // Noms de propriétés existantes
            var existingProps = classDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => p.Identifier.Text.ToLower())
                .ToHashSet();

            var result = new List<(TypeSyntax, string, string)>();
            foreach (var field in classDecl.Members.OfType<FieldDeclarationSyntax>())
            {
                if (field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    || field.Modifiers.Any(SyntaxKind.ConstKeyword)
                    || field.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;

                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldName = variable.Identifier.Text;
                    var propName = ToPascalCase(fieldName.TrimStart('_'));

                    // Vérifier qu'il n'y a pas déjà une propriété avec ce nom
                    if (!existingProps.Contains(propName.ToLower()))
                        result.Add((field.Declaration.Type, fieldName, propName));
                }
            }
            return result;
        }

        private static string ToPascalCase(string name) =>
            name.Length == 0 ? name : char.ToUpper(name[0]) + name.Substring(1);

        private static async Task<Document> GenerateAsync(
            Document document,
            ClassDeclarationSyntax classDecl,
            List<(TypeSyntax Type, string FieldName, string PropName)> fields,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var properties = fields.Select(f =>
                (MemberDeclarationSyntax)SyntaxFactory.PropertyDeclaration(f.Type, f.PropName)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                                SyntaxFactory.IdentifierName(f.FieldName)))
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(f.FieldName),
                                    SyntaxFactory.IdentifierName("value"))))
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })))
                    .WithAdditionalAnnotations(Formatter.Annotation))
                .ToArray();

            var newClassDecl = classDecl.WithMembers(classDecl.Members.AddRange(properties));
            var newRoot = root.ReplaceNode(classDecl, newClassDecl);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
