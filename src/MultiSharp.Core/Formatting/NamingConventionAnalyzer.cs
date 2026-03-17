using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiSharp.Analyzers;

namespace MultiSharp.Formatting
{
    /// <summary>
    /// US-502 — Analyseur de conventions de nommage.
    /// Règles : interfaces en I-prefix, classes/méthodes/propriétés en PascalCase,
    /// paramètres/variables locales en camelCase, champs privés en _camelCase.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NamingConventionAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor InterfacePrefix = new DiagnosticDescriptor(
            id: DiagnosticIds.NamingInterfacePrefix,
            title: "Interface sans préfixe 'I'",
            messageFormat: "L'interface '{0}' devrait commencer par 'I'",
            category: "Style",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PascalCaseType = new DiagnosticDescriptor(
            id: DiagnosticIds.NamingPascalCaseType,
            title: "Type non PascalCase",
            messageFormat: "Le type '{0}' devrait être en PascalCase",
            category: "Style",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CamelCaseParameter = new DiagnosticDescriptor(
            id: DiagnosticIds.NamingCamelCaseParam,
            title: "Paramètre non camelCase",
            messageFormat: "Le paramètre '{0}' devrait être en camelCase",
            category: "Style",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PrivateFieldUnderscore = new DiagnosticDescriptor(
            id: DiagnosticIds.NamingPrivateField,
            title: "Champ privé sans préfixe '_'",
            messageFormat: "Le champ privé '{0}' devrait commencer par '_'",
            category: "Style",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(InterfacePrefix, PascalCaseType, CamelCaseParameter, PrivateFieldUnderscore);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.RecordDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
            context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
        }

        private static void AnalyzeInterface(SyntaxNodeAnalysisContext ctx)
        {
            var decl = (InterfaceDeclarationSyntax)ctx.Node;
            var name = decl.Identifier.Text;
            if (!name.StartsWith("I") || (name.Length > 1 && !char.IsUpper(name[1])))
                ctx.ReportDiagnostic(Diagnostic.Create(InterfacePrefix, decl.Identifier.GetLocation(), name));
        }

        private static void AnalyzeType(SyntaxNodeAnalysisContext ctx)
        {
            var name = ctx.Node switch
            {
                ClassDeclarationSyntax c => c.Identifier.Text,
                StructDeclarationSyntax s => s.Identifier.Text,
                EnumDeclarationSyntax e => e.Identifier.Text,
                RecordDeclarationSyntax r => r.Identifier.Text,
                _ => null
            };
            if (name != null && !IsPascalCase(name))
            {
                var loc = ctx.Node switch
                {
                    ClassDeclarationSyntax c => c.Identifier.GetLocation(),
                    StructDeclarationSyntax s => s.Identifier.GetLocation(),
                    EnumDeclarationSyntax e => e.Identifier.GetLocation(),
                    RecordDeclarationSyntax r => r.Identifier.GetLocation(),
                    _ => ctx.Node.GetLocation()
                };
                ctx.ReportDiagnostic(Diagnostic.Create(PascalCaseType, loc, name));
            }
        }

        private static void AnalyzeParameter(SyntaxNodeAnalysisContext ctx)
        {
            var param = (ParameterSyntax)ctx.Node;
            var name = param.Identifier.Text;
            if (string.IsNullOrEmpty(name) || name == "_") return;
            if (!IsCamelCase(name))
                ctx.ReportDiagnostic(Diagnostic.Create(CamelCaseParameter, param.Identifier.GetLocation(), name));
        }

        private static void AnalyzeField(SyntaxNodeAnalysisContext ctx)
        {
            var field = (FieldDeclarationSyntax)ctx.Node;
            if (!field.Modifiers.Any(SyntaxKind.PrivateKeyword)
                && !(!field.Modifiers.Any(SyntaxKind.PublicKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ProtectedKeyword)
                    && !field.Modifiers.Any(SyntaxKind.InternalKeyword)))
                return;
            if (field.Modifiers.Any(SyntaxKind.StaticKeyword)
                || field.Modifiers.Any(SyntaxKind.ConstKeyword)) return;

            foreach (var variable in field.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                if (!name.StartsWith("_"))
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        PrivateFieldUnderscore, variable.Identifier.GetLocation(), name));
            }
        }

        public static bool IsPascalCase(string name) =>
            name.Length > 0 && char.IsUpper(name[0]);

        public static bool IsCamelCase(string name) =>
            name.Length > 0 && char.IsLower(name[0]) && !name.StartsWith("_");
    }
}
