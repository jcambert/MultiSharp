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
    /// US-303 — Génère ToString() affichant le nom du type et ses membres publics.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GenerateToStringRefactoring))]
    [Shared]
    public sealed class GenerateToStringRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(context.Span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var classDecl = token.Parent as ClassDeclarationSyntax;
            if (classDecl == null || classDecl.Identifier != token) return;

            // Ne pas proposer si ToString existe déjà
            var hasToString = classDecl.Members.OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.Text == "ToString"
                    && m.ParameterList.Parameters.Count == 0);
            if (hasToString) return;

            var members = GetPublicMembers(classDecl);
            if (members.Count == 0) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: "Générer ToString()…",
                createChangedDocument: ct => GenerateAsync(context.Document, classDecl, members, ct),
                equivalenceKey: nameof(GenerateToStringRefactoring)));
        }

        private static List<string> GetPublicMembers(ClassDeclarationSyntax classDecl)
        {
            var names = new List<string>();
            foreach (var member in classDecl.Members)
            {
                if (member is FieldDeclarationSyntax field
                    && field.Modifiers.Any(SyntaxKind.PublicKeyword)
                    && !field.Modifiers.Any(SyntaxKind.StaticKeyword))
                    foreach (var v in field.Declaration.Variables)
                        names.Add(v.Identifier.Text);

                if (member is PropertyDeclarationSyntax prop
                    && prop.Modifiers.Any(SyntaxKind.PublicKeyword)
                    && !prop.Modifiers.Any(SyntaxKind.StaticKeyword))
                    names.Add(prop.Identifier.Text);
            }
            return names;
        }

        private static async Task<Document> GenerateAsync(
            Document document,
            ClassDeclarationSyntax classDecl,
            List<string> memberNames,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var typeName = classDecl.Identifier.Text;

            // Construire: $"TypeName {{ Prop1 = {Prop1}, Prop2 = {Prop2} }}"
            // On utilise une interpolation string simple
            var parts = memberNames.Select(n => $"{n} = {{{n}}}");
            var interpolatedContent = $"{typeName} {{ {string.Join(", ", parts)} }}";

            // Construire l'interpolated string via SyntaxFactory
            var interpolatedString = BuildInterpolatedString(typeName, memberNames);

            var toStringMethod = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)), "ToString")
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList())
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(interpolatedString))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newClassDecl = classDecl.WithMembers(classDecl.Members.Add(toStringMethod));
            var newRoot = root.ReplaceNode(classDecl, newClassDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        private static ExpressionSyntax BuildInterpolatedString(string typeName, List<string> memberNames)
        {
            // Construire manuellement: $"TypeName { Prop1 = {Prop1}, ... }"
            var contents = new List<InterpolatedStringContentSyntax>();

            // Texte de début: "TypeName { "
            contents.Add(SyntaxFactory.InterpolatedStringText(
                SyntaxFactory.Token(
                    SyntaxTriviaList.Empty,
                    SyntaxKind.InterpolatedStringTextToken,
                    typeName + " { ",
                    typeName + " { ",
                    SyntaxTriviaList.Empty)));

            for (int i = 0; i < memberNames.Count; i++)
            {
                if (i > 0)
                {
                    contents.Add(SyntaxFactory.InterpolatedStringText(
                        SyntaxFactory.Token(
                            SyntaxTriviaList.Empty,
                            SyntaxKind.InterpolatedStringTextToken,
                            ", ",
                            ", ",
                            SyntaxTriviaList.Empty)));
                }

                // "Name = "
                contents.Add(SyntaxFactory.InterpolatedStringText(
                    SyntaxFactory.Token(
                        SyntaxTriviaList.Empty,
                        SyntaxKind.InterpolatedStringTextToken,
                        memberNames[i] + " = ",
                        memberNames[i] + " = ",
                        SyntaxTriviaList.Empty)));

                // {memberName}
                contents.Add(SyntaxFactory.Interpolation(
                    SyntaxFactory.IdentifierName(memberNames[i])));
            }

            // Texte de fin: " }"
            contents.Add(SyntaxFactory.InterpolatedStringText(
                SyntaxFactory.Token(
                    SyntaxTriviaList.Empty,
                    SyntaxKind.InterpolatedStringTextToken,
                    " }",
                    " }",
                    SyntaxTriviaList.Empty)));

            return SyntaxFactory.InterpolatedStringExpression(
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                SyntaxFactory.List(contents),
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
        }
    }
}
