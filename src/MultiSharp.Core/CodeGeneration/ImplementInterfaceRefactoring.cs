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
    /// US-305 — Implémente les membres manquants d'une interface déclarée dans la liste de base.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ImplementInterfaceRefactoring))]
    [Shared]
    public sealed class ImplementInterfaceRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(context.Span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var classDecl = token.Parent as ClassDeclarationSyntax;
            if (classDecl == null || classDecl.Identifier != token) return;

            if (classDecl.BaseList == null || classDecl.BaseList.Types.Count == 0) return;

            var model = await document.GetSemanticModelAsync(context.CancellationToken);
            if (model == null) return;

            var classSymbol = model.GetDeclaredSymbol(classDecl, context.CancellationToken) as INamedTypeSymbol;
            if (classSymbol == null) return;

            // Trouver les membres d'interface non implémentés
            var missing = GetMissingMembers(classSymbol).ToList();
            if (missing.Count == 0) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: $"Implémenter les membres manquants ({missing.Count})…",
                createChangedDocument: ct => ImplementAsync(document, classDecl, missing, ct),
                equivalenceKey: nameof(ImplementInterfaceRefactoring)));
        }

        private static IEnumerable<ISymbol> GetMissingMembers(INamedTypeSymbol classSymbol)
        {
            foreach (var iface in classSymbol.AllInterfaces)
            {
                foreach (var member in iface.GetMembers())
                {
                    if (member.IsStatic) continue;
                    var impl = classSymbol.FindImplementationForInterfaceMember(member);
                    if (impl == null)
                        yield return member;
                }
            }
        }

        private static async Task<Document> ImplementAsync(
            Document document,
            ClassDeclarationSyntax classDecl,
            List<ISymbol> missing,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var generated = new List<MemberDeclarationSyntax>();
            foreach (var member in missing)
            {
                var syntax = GenerateMember(member);
                if (syntax != null)
                    generated.Add(syntax);
            }

            if (generated.Count == 0) return document;

            var newClassDecl = classDecl.WithMembers(classDecl.Members.AddRange(generated));
            var newRoot = root.ReplaceNode(classDecl, newClassDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        private static MemberDeclarationSyntax? GenerateMember(ISymbol member)
        {
            switch (member)
            {
                case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    return GenerateMethod(method);
                case IPropertySymbol property:
                    return GenerateProperty(property);
                default:
                    return null;
            }
        }

        private static MethodDeclarationSyntax GenerateMethod(IMethodSymbol method)
        {
            var returnType = SyntaxFactory.ParseTypeName(
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var parameters = method.Parameters.Select(p =>
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                    .WithType(SyntaxFactory.ParseTypeName(
                        p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))))
                .ToArray();

            // Corps: throw new NotImplementedException();
            var throwStmt = SyntaxFactory.ThrowStatement(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName("System.NotImplementedException"))
                .WithArgumentList(SyntaxFactory.ArgumentList()));

            return SyntaxFactory.MethodDeclaration(returnType, method.Name)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                .WithBody(SyntaxFactory.Block(throwStmt))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static PropertyDeclarationSyntax GenerateProperty(IPropertySymbol property)
        {
            var propType = SyntaxFactory.ParseTypeName(
                property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var throwExpr = SyntaxFactory.ThrowExpression(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName("System.NotImplementedException"))
                .WithArgumentList(SyntaxFactory.ArgumentList()));

            var accessors = new List<AccessorDeclarationSyntax>();

            if (!property.IsWriteOnly)
                accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(throwExpr))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            if (!property.IsReadOnly)
                accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(throwExpr))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            return SyntaxFactory.PropertyDeclaration(propType, property.Name)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
