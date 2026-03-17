using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiSharp.Analyzers;

namespace MultiSharp.Advanced
{
    /// <summary>
    /// US-605 — Suggestions d'optimisation LINQ :
    /// .Where().First() → .First(pred), .Count() > 0 → .Any(), etc.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LinqOptimizationAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor WhereFirst = new DiagnosticDescriptor(
            id: DiagnosticIds.LinqWhereFirst,
            title: "Simplifier .Where().First()",
            messageFormat: "Remplacer '.Where(pred).First()' par '.First(pred)' (plus efficace)",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CountNotAny = new DiagnosticDescriptor(
            id: DiagnosticIds.LinqCountNotAny,
            title: "Utiliser .Any() au lieu de .Count() > 0",
            messageFormat: "Remplacer '.Count() {0} 0' par '.Any()' (plus efficace)",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor WhereCount = new DiagnosticDescriptor(
            id: DiagnosticIds.LinqWhereCount,
            title: "Simplifier .Where().Count()",
            messageFormat: "Remplacer '.Where(pred).Count()' par '.Count(pred)'",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(WhereFirst, CountNotAny, WhereCount);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax outerAccess) return;
            var outerName = outerAccess.Name.Identifier.Text;

            // Détecter .Count() > 0 et .Count() == 0
            if (outerName == "Count" && invocation.ArgumentList.Arguments.Count == 0)
            {
                var parent = invocation.Parent;
                if (parent is BinaryExpressionSyntax bin)
                {
                    var isCountComparison =
                        (bin.Left == invocation && bin.Right is LiteralExpressionSyntax rightLit
                            && rightLit.Token.Value is int rightVal && rightVal == 0)
                        || (bin.Right == invocation && bin.Left is LiteralExpressionSyntax leftLit
                            && leftLit.Token.Value is int leftVal && leftVal == 0);

                    if (isCountComparison
                        && (bin.IsKind(SyntaxKind.GreaterThanExpression)
                            || bin.IsKind(SyntaxKind.NotEqualsExpression)
                            || bin.IsKind(SyntaxKind.EqualsExpression)))
                    {
                        var op = bin.IsKind(SyntaxKind.EqualsExpression) ? "==" : ">";
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            CountNotAny, invocation.GetLocation(), op));
                    }
                }
            }

            // Détecter .Where(...).First() ou .Where(...).FirstOrDefault()
            if ((outerName == "First" || outerName == "FirstOrDefault" || outerName == "Single" || outerName == "SingleOrDefault")
                && invocation.ArgumentList.Arguments.Count == 0)
            {
                if (IsWhereCall(outerAccess.Expression))
                    ctx.ReportDiagnostic(Diagnostic.Create(WhereFirst, invocation.GetLocation()));
            }

            // Détecter .Where(...).Count()
            if (outerName == "Count" && invocation.ArgumentList.Arguments.Count == 0)
            {
                if (IsWhereCall(outerAccess.Expression))
                    ctx.ReportDiagnostic(Diagnostic.Create(WhereCount, invocation.GetLocation()));
            }
        }

        private static bool IsWhereCall(ExpressionSyntax expr)
        {
            if (expr is not InvocationExpressionSyntax inner) return false;
            if (inner.Expression is not MemberAccessExpressionSyntax innerAccess) return false;
            return innerAccess.Name.Identifier.Text == "Where";
        }
    }
}
