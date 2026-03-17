using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MultiSharp.Analyzers
{
    /// <summary>
    /// MS0107 — Détecte les directives using non utilisées dans un fichier.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedUsingAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticIds.UnusedUsing,
            title: "Directive using inutilisée",
            messageFormat: "La directive 'using {0}' n'est pas utilisée",
            category: "MultiSharp.Style",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Les directives using non utilisées encombrent le fichier.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var root = context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken);
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            if (usings.Count == 0) return;

            // Utiliser les diagnostics du compilateur CS8019 (using inutilisé)
            // comme source de vérité — on évite de recalculer ce qu'il sait déjà
            var unusedUsings = context.SemanticModel
                .GetDiagnostics(cancellationToken: context.CancellationToken)
                .Where(d => d.Id == "CS8019")
                .ToList();

            foreach (var compilerDiag in unusedUsings)
            {
                // Trouver le using correspondant
                var node = root.FindNode(compilerDiag.Location.SourceSpan) as UsingDirectiveSyntax;
                if (node == null) continue;

                var namespaceName = node.Name?.ToString() ?? node.ToString();
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    node.GetLocation(),
                    namespaceName));
            }
        }
    }
}
