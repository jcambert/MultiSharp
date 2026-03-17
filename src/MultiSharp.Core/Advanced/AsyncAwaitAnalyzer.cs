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
    /// US-604 — Détection des mauvaises pratiques async/await :
    /// .Result/.Wait() bloquants, async void, async sans await.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncAwaitAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor BlockingAsyncCall = new DiagnosticDescriptor(
            id: DiagnosticIds.AsyncBlockingCall,
            title: "Appel bloquant sur Task",
            messageFormat: "'{0}' bloque le thread courant et peut causer un deadlock. Utilisez 'await'.",
            category: "Async",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AsyncVoid = new DiagnosticDescriptor(
            id: DiagnosticIds.AsyncVoidMethod,
            title: "Méthode async void",
            messageFormat: "La méthode '{0}' est 'async void'. Utilisez 'async Task' pour pouvoir gérer les exceptions.",
            category: "Async",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AsyncWithoutAwait = new DiagnosticDescriptor(
            id: DiagnosticIds.AsyncWithoutAwait,
            title: "Méthode async sans await",
            messageFormat: "La méthode '{0}' est déclarée async mais ne contient aucun await.",
            category: "Async",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(BlockingAsyncCall, AsyncVoid, AsyncWithoutAwait);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext ctx)
        {
            var memberAccess = (MemberAccessExpressionSyntax)ctx.Node;
            var memberName = memberAccess.Name.Identifier.Text;

            if (memberName != "Result" && memberName != "Wait" && memberName != "GetAwaiter") return;

            // Vérifier que l'expression cible est de type Task
            var model = ctx.SemanticModel;
            var typeInfo = model.GetTypeInfo(memberAccess.Expression, ctx.CancellationToken);
            var type = typeInfo.Type;
            if (type == null) return;

            var typeName = type.ToDisplayString();
            if (!typeName.Contains("Task") && !typeName.Contains("ValueTask")) return;

            // .GetAwaiter().GetResult() est aussi bloquant mais plus difficile à détecter
            var accessName = memberName == "Wait" ? ".Wait()" : "." + memberName;
            ctx.ReportDiagnostic(Diagnostic.Create(
                BlockingAsyncCall, memberAccess.Name.GetLocation(), accessName));
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext ctx)
        {
            var method = (MethodDeclarationSyntax)ctx.Node;

            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword)) return;

            // async void (hors event handlers)
            if (method.ReturnType is PredefinedTypeSyntax pre
                && pre.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                // Les event handlers (signature void M(object, EventArgs)) sont légitimes
                var @params = method.ParameterList.Parameters;
                bool isEventHandler = @params.Count == 2
                    && @params[0].Type?.ToString() == "object"
                    && (@params[1].Type?.ToString().Contains("EventArgs") ?? false);

                if (!isEventHandler)
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        AsyncVoid, method.Identifier.GetLocation(), method.Identifier.Text));
            }

            // async sans await
            var hasAwait = method.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
            if (!hasAwait)
                ctx.ReportDiagnostic(Diagnostic.Create(
                    AsyncWithoutAwait, method.Identifier.GetLocation(), method.Identifier.Text));
        }
    }
}
