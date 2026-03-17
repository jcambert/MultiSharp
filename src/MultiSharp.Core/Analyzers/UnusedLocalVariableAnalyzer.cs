using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MultiSharp.Analyzers
{
    /// <summary>
    /// MS0101 — Détecte les variables locales déclarées mais jamais lues.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedLocalVariableAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticIds.UnusedLocalVariable,
            title: "Variable locale inutilisée",
            messageFormat: "La variable '{0}' est déclarée mais jamais utilisée",
            category: "MultiSharp.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Les variables locales non utilisées encombrent le code.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            // Récupérer le corps (BlockSyntax) du membre
            BlockSyntax? body = context.Node switch
            {
                MethodDeclarationSyntax m => m.Body,
                ConstructorDeclarationSyntax c => c.Body,
                LocalFunctionStatementSyntax l => l.Body,
                _ => null
            };

            if (body == null) return;

            var model = context.SemanticModel;
            var dataFlow = model.AnalyzeDataFlow(body);
            if (dataFlow == null || !dataFlow.Succeeded) return;

            // Chercher toutes les déclarations de variables locales dans le corps
            foreach (var decl in body.DescendantNodes<LocalDeclarationStatementSyntax>())
            {
                foreach (var variable in decl.Declaration.Variables)
                {
                    var name = variable.Identifier.Text;

                    // Convention discard : _, _foo
                    if (name == "_" || name.StartsWith("_")) continue;

                    var symbol = model.GetDeclaredSymbol(variable) as ILocalSymbol;
                    if (symbol == null) continue;

                    // La variable est-elle dans la liste des symboles lus ?
                    if (!dataFlow.ReadInside.Contains(symbol) &&
                        !dataFlow.CapturedInside.Contains(symbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule,
                            variable.Identifier.GetLocation(),
                            name));
                    }
                }
            }
        }
    }

    // Extension helper pour éviter les casts répétitifs
    internal static class SyntaxNodeExtensions
    {
        public static System.Collections.Generic.IEnumerable<T> DescendantNodes<T>(
            this SyntaxNode node) where T : SyntaxNode
            => System.Linq.Enumerable.OfType<T>(node.DescendantNodes());
    }
}
