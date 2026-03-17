using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MultiSharp.Analyzers
{
    /// <summary>
    /// MS0105 — Détecte les comparaisons booléennes redondantes : if (x == true), if (x != false).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RedundantBoolComparisonAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticIds.RedundantBoolComparison,
            title: "Comparaison booléenne redondante",
            messageFormat: "La comparaison '{0}' est redondante, utilisez directement '{1}'",
            category: "MultiSharp.Style",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Comparer une expression booléenne à true/false est redondant.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeBinary, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinary, SyntaxKind.NotEqualsExpression);
        }

        private static void AnalyzeBinary(SyntaxNodeAnalysisContext context)
        {
            var binary = (BinaryExpressionSyntax)context.Node;
            var model = context.SemanticModel;

            // Détecter : expr == true / true == expr / expr != false / false != expr
            if (IsBoolLiteral(binary.Right, out var rightValue))
            {
                var leftType = model.GetTypeInfo(binary.Left).Type;
                if (leftType?.SpecialType != SpecialType.System_Boolean) return;

                ReportRedundant(context, binary, binary.Left.ToString(), rightValue,
                    binary.IsKind(SyntaxKind.NotEqualsExpression));
            }
            else if (IsBoolLiteral(binary.Left, out var leftBoolValue))
            {
                var rightType = model.GetTypeInfo(binary.Right).Type;
                if (rightType?.SpecialType != SpecialType.System_Boolean) return;

                ReportRedundant(context, binary, binary.Right.ToString(), leftBoolValue,
                    binary.IsKind(SyntaxKind.NotEqualsExpression));
            }
        }

        private static void ReportRedundant(
            SyntaxNodeAnalysisContext context,
            BinaryExpressionSyntax binary,
            string expr, bool literalValue, bool isNotEquals)
        {
            // == true  → expr         != true  → !expr
            // == false → !expr        != false → expr
            bool negated = (literalValue && isNotEquals) || (!literalValue && !isNotEquals);
            var simplified = negated ? $"!{expr}" : expr;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                binary.GetLocation(),
                binary.ToString(),
                simplified));
        }

        private static bool IsBoolLiteral(ExpressionSyntax expr, out bool value)
        {
            if (expr.IsKind(SyntaxKind.TrueLiteralExpression))  { value = true;  return true; }
            if (expr.IsKind(SyntaxKind.FalseLiteralExpression)) { value = false; return true; }
            value = false;
            return false;
        }
    }
}
