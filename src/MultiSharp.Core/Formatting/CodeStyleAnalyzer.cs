using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiSharp.Analyzers;

namespace MultiSharp.Formatting
{
    /// <summary>
    /// US-503 — Suggestions de modernisation du style C#.
    /// Détecte : type explicite quand var suffit, méthodes une ligne → expression body.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CodeStyleAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor UseVar = new DiagnosticDescriptor(
            id: DiagnosticIds.UseVarKeyword,
            title: "Utiliser 'var'",
            messageFormat: "Le type peut être remplacé par 'var' (type apparent : '{0}')",
            category: "Style",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UseExpressionBody = new DiagnosticDescriptor(
            id: DiagnosticIds.UseExpressionBody,
            title: "Utiliser un corps d'expression",
            messageFormat: "La méthode '{0}' peut être convertie en expression body (=>)",
            category: "Style",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(UseVar, UseExpressionBody);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext ctx)
        {
            var decl = (LocalDeclarationStatementSyntax)ctx.Node;
            var varDecl = decl.Declaration;

            // Déjà var
            if (varDecl.Type.IsVar) return;

            // Plusieurs déclarations → pas de suggestion
            if (varDecl.Variables.Count != 1) return;

            var variable = varDecl.Variables[0];
            if (variable.Initializer == null) return;

            // Vérifier sémantiquement que le type est apparent
            var model = ctx.SemanticModel;
            var typeInfo = model.GetTypeInfo(variable.Initializer.Value, ctx.CancellationToken);
            if (typeInfo.Type == null || typeInfo.Type.Kind == SymbolKind.ErrorType) return;
            if (typeInfo.Type.SpecialType == SpecialType.System_Void) return;

            // Ne pas suggérer var pour les littéraux numériques (ambiguïté int/long/etc.)
            if (variable.Initializer.Value is LiteralExpressionSyntax lit
                && (lit.IsKind(SyntaxKind.NumericLiteralExpression)
                    || lit.IsKind(SyntaxKind.NullLiteralExpression)))
                return;

            var typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            ctx.ReportDiagnostic(Diagnostic.Create(UseVar, varDecl.Type.GetLocation(), typeName));
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext ctx)
        {
            var method = (MethodDeclarationSyntax)ctx.Node;

            // Déjà expression body
            if (method.ExpressionBody != null) return;
            if (method.Body == null) return;

            var statements = method.Body.Statements;

            // Un seul statement de type return ou expression
            if (statements.Count != 1) return;

            var stmt = statements[0];
            if (stmt is ReturnStatementSyntax ret && ret.Expression != null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    UseExpressionBody, method.Identifier.GetLocation(), method.Identifier.Text));
            }
            else if (stmt is ExpressionStatementSyntax expr
                && method.ReturnType is PredefinedTypeSyntax pre
                && pre.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    UseExpressionBody, method.Identifier.GetLocation(), method.Identifier.Text));
            }
        }
    }
}
