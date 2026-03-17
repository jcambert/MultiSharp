using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiSharp.Options;

namespace MultiSharp.Analyzers
{
    /// <summary>
    /// MS0108/MS0109/MS0110 — Détecte les code smells SOLID :
    /// méthodes trop longues, trop de paramètres, nesting trop profond.
    /// Les seuils sont lus depuis <see cref="MultiSharpSettings"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CodeSmellsAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor MethodTooLongRule = new(
            id: DiagnosticIds.MethodTooLong,
            title: "Méthode trop longue",
            messageFormat: "La méthode '{0}' fait {1} lignes (maximum : {2})",
            category: "MultiSharp.Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TooManyParametersRule = new(
            id: DiagnosticIds.TooManyParameters,
            title: "Trop de paramètres",
            messageFormat: "La méthode '{0}' a {1} paramètres (maximum : {2})",
            category: "MultiSharp.Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NestingTooDeepRule = new(
            id: DiagnosticIds.NestingTooDeep,
            title: "Imbrication trop profonde",
            messageFormat: "Ce bloc est imbriqué à {0} niveaux (maximum : {1})",
            category: "MultiSharp.Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // Seuils par défaut (peuvent être surchargés via MultiSharpSettings)
        public int MaxMethodLines   { get; set; } = MultiSharpSettings.Default.MaxMethodLines;
        public int MaxParameters    { get; set; } = MultiSharpSettings.Default.MaxParameters;
        public int MaxNestingDepth  { get; set; } = MultiSharpSettings.Default.MaxNestingDepth;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MethodTooLongRule, TooManyParametersRule, NestingTooDeepRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var methodName = method.Identifier.Text;

            // ── Méthode trop longue ───────────────────────────────────────
            if (method.Body != null)
            {
                var lineSpan = method.Body.GetLocation().GetLineSpan();
                var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;

                if (lineCount > MaxMethodLines)
                    context.ReportDiagnostic(Diagnostic.Create(
                        MethodTooLongRule, method.Identifier.GetLocation(),
                        methodName, lineCount, MaxMethodLines));
            }

            // ── Trop de paramètres ────────────────────────────────────────
            var paramCount = method.ParameterList.Parameters.Count;
            if (paramCount > MaxParameters)
                context.ReportDiagnostic(Diagnostic.Create(
                    TooManyParametersRule, method.Identifier.GetLocation(),
                    methodName, paramCount, MaxParameters));

            // ── Nesting trop profond ──────────────────────────────────────
            if (method.Body != null)
                CheckNesting(context, method.Body, 0);
        }

        private void CheckNesting(SyntaxNodeAnalysisContext context, SyntaxNode node, int depth)
        {
            foreach (var child in node.ChildNodes())
            {
                var isNestingNode = child is IfStatementSyntax
                    or ForStatementSyntax or ForEachStatementSyntax
                    or WhileStatementSyntax or DoStatementSyntax
                    or SwitchStatementSyntax or TryStatementSyntax;

                var nextDepth = isNestingNode ? depth + 1 : depth;

                if (nextDepth > MaxNestingDepth)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        NestingTooDeepRule, child.GetLocation(),
                        nextDepth, MaxNestingDepth));
                    return; // Signaler seulement le premier niveau de dépassement
                }

                CheckNesting(context, child, nextDepth);
            }
        }
    }
}
