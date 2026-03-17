using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MultiSharp.Analyzers
{
    /// <summary>
    /// MS0102 — Détecte les paramètres de méthode jamais utilisés dans le corps.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedParameterAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticIds.UnusedParameter,
            title: "Paramètre inutilisé",
            messageFormat: "Le paramètre '{0}' n'est pas utilisé",
            category: "MultiSharp.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Un paramètre déclaré mais non utilisé peut être supprimé ou indique une erreur de logique.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            // Ignorer les méthodes abstraites, extern, partial sans corps
            if (method.Body == null && method.ExpressionBody == null) return;

            var model = context.SemanticModel;
            var methodSymbol = model.GetDeclaredSymbol(method);
            if (methodSymbol == null) return;

            // Ignorer les overrides et implémentations d'interface (signature imposée)
            if (methodSymbol.IsOverride || methodSymbol.IsAbstract) return;
            if (methodSymbol.ExplicitInterfaceImplementations.Length > 0) return;

            // Corps de la méthode pour l'analyse de flux
            SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;
            if (body == null) return;

            foreach (var param in method.ParameterList.Parameters)
            {
                var paramName = param.Identifier.Text;

                // Convention : _ ou _name = paramètre intentionnellement ignoré
                if (paramName == "_" || paramName.StartsWith("_")) continue;

                var paramSymbol = model.GetDeclaredSymbol(param) as IParameterSymbol;
                if (paramSymbol == null) continue;

                // Vérifier si le paramètre est référencé dans le corps
                var usages = body.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.Text == paramName)
                    .Select(id => model.GetSymbolInfo(id).Symbol)
                    .Any(s => SymbolEqualityComparer.Default.Equals(s, paramSymbol));

                if (!usages)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        param.Identifier.GetLocation(),
                        paramName));
                }
            }
        }
    }
}
