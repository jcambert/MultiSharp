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
    /// US-203 — Extract Interface : génère une interface à partir des membres publics d'une classe.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ExtractInterfaceRefactoring))]
    [Shared]
    public sealed class ExtractInterfaceRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            var token = root.FindToken(span.Start);
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return;

            var classDecl = token.Parent as ClassDeclarationSyntax
                ?? token.Parent?.Parent as ClassDeclarationSyntax;
            if (classDecl == null) return;

            // S'assurer que le curseur est sur le nom de la classe
            if (classDecl.Identifier != token) return;

            var publicMembers = GetPublicMembers(classDecl).ToList();
            if (publicMembers.Count == 0) return;

            context.RegisterRefactoring(CodeAction.Create(
                title: $"Extraire l'interface 'I{classDecl.Identifier.Text}'…",
                createChangedDocument: ct => ExtractInterfaceAsync(document, classDecl, publicMembers, ct),
                equivalenceKey: nameof(ExtractInterfaceRefactoring)));
        }

        private static IEnumerable<MemberDeclarationSyntax> GetPublicMembers(ClassDeclarationSyntax classDecl)
        {
            foreach (var member in classDecl.Members)
            {
                if (member is MethodDeclarationSyntax method
                    && method.Modifiers.Any(SyntaxKind.PublicKeyword)
                    && !method.Modifiers.Any(SyntaxKind.StaticKeyword))
                    yield return member;

                if (member is PropertyDeclarationSyntax prop
                    && prop.Modifiers.Any(SyntaxKind.PublicKeyword)
                    && !prop.Modifiers.Any(SyntaxKind.StaticKeyword))
                    yield return member;
            }
        }

        private static async Task<Document> ExtractInterfaceAsync(
            Document document,
            ClassDeclarationSyntax classDecl,
            List<MemberDeclarationSyntax> publicMembers,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            if (root == null) return document;

            var interfaceName = "I" + classDecl.Identifier.Text;

            // Construire les membres de l'interface (sans corps, sans modificateurs d'accès)
            var interfaceMembers = publicMembers
                .Select(BuildInterfaceMember)
                .Where(m => m != null)
                .Select(m => m!)
                .ToArray();

            // Créer la déclaration d'interface
            var interfaceDecl = SyntaxFactory.InterfaceDeclaration(interfaceName)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithMembers(SyntaxFactory.List(interfaceMembers))
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Ajouter l'implémentation à la classe
            var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(interfaceName));
            ClassDeclarationSyntax newClassDecl;
            if (classDecl.BaseList == null)
            {
                newClassDecl = classDecl.WithBaseList(
                    SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)));
            }
            else
            {
                newClassDecl = classDecl.WithBaseList(
                    classDecl.BaseList.AddTypes(baseType));
            }

            // Insérer l'interface avant la classe dans la compilation unit
            var typeDecl = classDecl.Parent as TypeDeclarationSyntax;
            SyntaxNode newRoot;

            if (typeDecl != null)
            {
                // Classe imbriquée
                var idx = typeDecl.Members.IndexOf(classDecl);
                var newMembers = typeDecl.Members
                    .Replace(classDecl, newClassDecl)
                    .Insert(idx, interfaceDecl);
                newRoot = root.ReplaceNode(typeDecl, typeDecl.WithMembers(newMembers));
            }
            else if (classDecl.Parent is CompilationUnitSyntax compilationUnit)
            {
                var idx = compilationUnit.Members.IndexOf(classDecl);
                var newMembers = compilationUnit.Members
                    .Replace(classDecl, newClassDecl)
                    .Insert(idx, interfaceDecl);
                newRoot = root.ReplaceNode(compilationUnit, compilationUnit.WithMembers(newMembers));
            }
            else if (classDecl.Parent is NamespaceDeclarationSyntax nsDecl)
            {
                var idx = nsDecl.Members.IndexOf(classDecl);
                var newMembers = nsDecl.Members
                    .Replace(classDecl, newClassDecl)
                    .Insert(idx, interfaceDecl);
                newRoot = root.ReplaceNode(nsDecl, nsDecl.WithMembers(newMembers));
            }
            else
            {
                return document;
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private static MemberDeclarationSyntax? BuildInterfaceMember(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                    return SyntaxFactory.MethodDeclaration(method.ReturnType, method.Identifier)
                        .WithTypeParameterList(method.TypeParameterList)
                        .WithParameterList(method.ParameterList)
                        .WithConstraintClauses(method.ConstraintClauses)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .WithAdditionalAnnotations(Formatter.Annotation);

                case PropertyDeclarationSyntax prop:
                    // Construire les accessors interface : get; et/ou set;
                    var accessors = new List<AccessorDeclarationSyntax>();
                    if (prop.AccessorList != null)
                    {
                        foreach (var acc in prop.AccessorList.Accessors)
                        {
                            if (acc.IsKind(SyntaxKind.GetAccessorDeclaration)
                                || acc.IsKind(SyntaxKind.SetAccessorDeclaration))
                            {
                                accessors.Add(SyntaxFactory.AccessorDeclaration(acc.Kind())
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                            }
                        }
                    }

                    if (accessors.Count == 0) return null;

                    return SyntaxFactory.PropertyDeclaration(prop.Type, prop.Identifier)
                        .WithAccessorList(SyntaxFactory.AccessorList(
                            SyntaxFactory.List(accessors)))
                        .WithAdditionalAnnotations(Formatter.Annotation);

                default:
                    return null;
            }
        }
    }
}
