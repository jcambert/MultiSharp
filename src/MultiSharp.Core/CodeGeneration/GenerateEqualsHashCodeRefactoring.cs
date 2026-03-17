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
    /// US-302 — Génère Equals(object) et GetHashCode() basés sur les champs/propriétés publics.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GenerateEqualsHashCodeRefactoring))]
    [Shared]
    public sealed class GenerateEqualsHashCodeRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(context.Span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var classDecl = token.Parent as ClassDeclarationSyntax;
            if (classDecl == null || classDecl.Identifier != token) return;

            var members = GetEqualityMembers(classDecl);
            if (members.Count == 0) return;

            var hasEquals = classDecl.Members.OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.Text == "Equals"
                    && m.ParameterList.Parameters.Count == 1);
            var hasHashCode = classDecl.Members.OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.Text == "GetHashCode"
                    && m.ParameterList.Parameters.Count == 0);

            if (hasEquals && hasHashCode) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: "Générer Equals et GetHashCode…",
                createChangedDocument: ct => GenerateAsync(context.Document, classDecl, members, ct),
                equivalenceKey: nameof(GenerateEqualsHashCodeRefactoring)));
        }

        private static List<string> GetEqualityMembers(ClassDeclarationSyntax classDecl)
        {
            var names = new List<string>();
            foreach (var member in classDecl.Members)
            {
                if (member is FieldDeclarationSyntax field
                    && !field.Modifiers.Any(SyntaxKind.StaticKeyword))
                    foreach (var v in field.Declaration.Variables)
                        names.Add(v.Identifier.Text);

                if (member is PropertyDeclarationSyntax prop
                    && !prop.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && prop.Modifiers.Any(SyntaxKind.PublicKeyword))
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
            var newMembers = new List<MemberDeclarationSyntax>();

            // Equals
            // public override bool Equals(object obj) {
            //   if (obj is TypeName other) return field1 == other.field1 && ...;
            //   return false; }
            var comparisons = memberNames
                .Select(n => (ExpressionSyntax)SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    SyntaxFactory.IdentifierName(n),
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("other"),
                        SyntaxFactory.IdentifierName(n))))
                .ToList();

            ExpressionSyntax equalsExpr = comparisons.Count == 1
                ? comparisons[0]
                : comparisons.Aggregate((a, b) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, a, b));

            var equalsBody = SyntaxFactory.Block(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.IsPatternExpression(
                        SyntaxFactory.IdentifierName("obj"),
                        SyntaxFactory.DeclarationPattern(
                            SyntaxFactory.IdentifierName(typeName),
                            SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier("other")))),
                    SyntaxFactory.ReturnStatement(equalsExpr)),
                SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)));

            newMembers.Add(SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), "Equals")
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("obj"))
                        .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))))
                .WithBody(equalsBody)
                .WithAdditionalAnnotations(Formatter.Annotation));

            // GetHashCode
            // public override int GetHashCode() => HashCode.Combine(f1, f2, ...);
            // Fallback (net472) : utiliser XOR
            ExpressionSyntax hashExpr;
            if (memberNames.Count == 1)
            {
                hashExpr = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(memberNames[0]),
                        SyntaxFactory.IdentifierName("GetHashCode")));
            }
            else
            {
                // XOR chain : f1.GetHashCode() ^ f2.GetHashCode() ^ ...
                hashExpr = memberNames
                    .Select(n => (ExpressionSyntax)SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(n),
                            SyntaxFactory.IdentifierName("GetHashCode"))))
                    .Aggregate((a, b) => SyntaxFactory.BinaryExpression(SyntaxKind.ExclusiveOrExpression, a, b));
            }

            newMembers.Add(SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), "GetHashCode")
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList())
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(hashExpr))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithAdditionalAnnotations(Formatter.Annotation));

            var newClassDecl = classDecl.WithMembers(classDecl.Members.AddRange(newMembers));
            var newRoot = root.ReplaceNode(classDecl, newClassDecl);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
