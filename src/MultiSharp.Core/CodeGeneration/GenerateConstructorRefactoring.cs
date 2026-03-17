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
    /// US-301 — Génère un constructeur initialisant tous les champs/propriétés privés non-statiques.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GenerateConstructorRefactoring))]
    [Shared]
    public sealed class GenerateConstructorRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(context.Span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var classDecl = token.Parent as ClassDeclarationSyntax;
            if (classDecl == null || classDecl.Identifier != token) return;

            var fields = GetInjectableFields(classDecl);
            if (fields.Count == 0) return;

            // Ne proposer que si aucun constructeur n'existe déjà avec ce nombre de paramètres
            var existingCtors = classDecl.Members.OfType<ConstructorDeclarationSyntax>()
                .Where(c => c.ParameterList.Parameters.Count == fields.Count)
                .ToList();
            if (existingCtors.Count > 0) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: "Générer le constructeur…",
                createChangedDocument: ct => GenerateAsync(context.Document, classDecl, fields, ct),
                equivalenceKey: nameof(GenerateConstructorRefactoring)));
        }

        private static List<(TypeSyntax Type, string Name)> GetInjectableFields(ClassDeclarationSyntax classDecl)
        {
            var result = new List<(TypeSyntax, string)>();
            foreach (var member in classDecl.Members)
            {
                if (member is FieldDeclarationSyntax field
                    && !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        var rawName = variable.Identifier.Text.TrimStart('_');
                        result.Add((field.Declaration.Type, rawName));
                    }
                }
                else if (member is PropertyDeclarationSyntax prop
                    && !prop.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && prop.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    result.Add((prop.Type, ToCamelCase(prop.Identifier.Text)));
                }
            }
            return result;
        }

        private static string ToCamelCase(string name) =>
            name.Length == 0 ? name : char.ToLower(name[0]) + name.Substring(1);

        private static async Task<Document> GenerateAsync(
            Document document,
            ClassDeclarationSyntax classDecl,
            List<(TypeSyntax Type, string Name)> fields,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            // Paramètres du constructeur
            var parameters = fields.Select(f =>
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(f.Name))
                    .WithType(f.Type)).ToArray();

            // Assignments this._field = field; ou this.field = field;
            var assignments = classDecl.Members
                .SelectMany(m => m switch
                {
                    FieldDeclarationSyntax fd when !fd.Modifiers.Any(SyntaxKind.StaticKeyword)
                        && !fd.Modifiers.Any(SyntaxKind.ConstKeyword)
                        => fd.Declaration.Variables.Select(v =>
                        {
                            var rawName = v.Identifier.Text.TrimStart('_');
                            return (FieldId: v.Identifier.Text, ParamName: rawName);
                        }),
                    _ => System.Array.Empty<(string, string)>()
                })
                .Select(x => (StatementSyntax)SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ThisExpression(),
                            SyntaxFactory.IdentifierName(x.FieldId)),
                        SyntaxFactory.IdentifierName(x.ParamName))))
                .ToList();

            var ctor = SyntaxFactory.ConstructorDeclaration(classDecl.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                .WithBody(SyntaxFactory.Block(assignments))
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Insérer en première position dans la classe
            var newClassDecl = classDecl.WithMembers(classDecl.Members.Insert(0, ctor));
            var newRoot = root.ReplaceNode(classDecl, newClassDecl);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
