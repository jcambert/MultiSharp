using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Rename;
using MultiSharp.Analyzers;

namespace MultiSharp.Formatting
{
    /// <summary>
    /// US-502 — Quick Fix pour les violations de convention de nommage.
    /// Renomme le symbole selon la convention (PascalCase, camelCase, _prefix, I-prefix).
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamingConventionCodeFix))]
    [Shared]
    public sealed class NamingConventionCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticIds.NamingInterfacePrefix,
            DiagnosticIds.NamingPascalCaseType,
            DiagnosticIds.NamingCamelCaseParam,
            DiagnosticIds.NamingPrivateField);

        public override FixAllProvider? GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var currentName = token.Text;
                string newName;

                switch (diagnostic.Id)
                {
                    case DiagnosticIds.NamingInterfacePrefix:
                        newName = "I" + ToPascalCase(currentName);
                        break;
                    case DiagnosticIds.NamingPascalCaseType:
                        newName = ToPascalCase(currentName);
                        break;
                    case DiagnosticIds.NamingCamelCaseParam:
                        newName = ToCamelCase(currentName);
                        break;
                    case DiagnosticIds.NamingPrivateField:
                        newName = "_" + ToCamelCase(currentName.TrimStart('_'));
                        break;
                    default:
                        continue;
                }

                if (newName == currentName) continue;

                context.RegisterCodeFix(CodeAction.Create(
                    title: $"Renommer en '{newName}'",
                    createChangedSolution: ct => RenameAsync(context.Document, token, newName, ct),
                    equivalenceKey: diagnostic.Id + "_fix"),
                    diagnostic);
            }
        }

        private static async Task<Solution> RenameAsync(
            Document document,
            SyntaxToken token,
            string newName,
            CancellationToken ct)
        {
            var model = await document.GetSemanticModelAsync(ct);
            if (model == null) return document.Project.Solution;

            var node = token.Parent;
            if (node == null) return document.Project.Solution;

            var symbol = model.GetDeclaredSymbol(node, ct)
                      ?? model.GetSymbolInfo(node, ct).Symbol;
            if (symbol == null) return document.Project.Solution;

            return await Renamer.RenameSymbolAsync(
                document.Project.Solution, symbol,
                new SymbolRenameOptions(), newName, ct);
        }

        private static string ToPascalCase(string name)
        {
            name = name.TrimStart('_');
            return name.Length == 0 ? name : char.ToUpper(name[0]) + name.Substring(1);
        }

        private static string ToCamelCase(string name)
        {
            name = name.TrimStart('_');
            return name.Length == 0 ? name : char.ToLower(name[0]) + name.Substring(1);
        }
    }
}
