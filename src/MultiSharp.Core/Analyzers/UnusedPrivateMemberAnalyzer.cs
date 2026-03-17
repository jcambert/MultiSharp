using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MultiSharp.Analyzers
{
    /// <summary>
    /// MS0103 — Détecte les membres privés (champs, méthodes, propriétés) jamais référencés.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedPrivateMemberAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticIds.UnusedPrivateMember,
            title: "Membre privé inutilisé",
            messageFormat: "Le membre privé '{0}' n'est jamais utilisé",
            category: "MultiSharp.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Les membres privés non utilisés constituent du code mort.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            // Collecter tous les membres privés candidates
            var privateMembers = type.GetMembers()
                .Where(IsPrivateCandidate)
                .ToList();

            if (privateMembers.Count == 0) return;

            // Collecter tous les identifiants référencés dans les syntaxes du type
            var syntaxRefs = type.DeclaringSyntaxReferences;
            var referencedNames = syntaxRefs
                .SelectMany(r => r.GetSyntax().DescendantNodes())
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToHashSet();

            foreach (var member in privateMembers)
            {
                // Ignorer les membres utilisés comme handlers d'événements ou via réflexion
                if (member.Name.StartsWith("_")) continue;

                // Si aucune référence au nom dans tout le type (approximation rapide)
                var usageCount = syntaxRefs
                    .SelectMany(r => r.GetSyntax().DescendantNodes())
                    .OfType<IdentifierNameSyntax>()
                    .Count(id => id.Identifier.Text == member.Name);

                // Le membre se référence lui-même dans sa déclaration (1 occurrence = pas utilisé)
                if (usageCount <= 1)
                {
                    var location = member.Locations.FirstOrDefault();
                    if (location != null)
                        context.ReportDiagnostic(Diagnostic.Create(Rule, location, member.Name));
                }
            }
        }

        private static bool IsPrivateCandidate(ISymbol member) =>
            member.DeclaredAccessibility == Accessibility.Private &&
            !member.IsImplicitlyDeclared &&
            member.Kind is SymbolKind.Field or SymbolKind.Method or SymbolKind.Property &&
            !member.Name.StartsWith(".") && // .ctor etc.
            !(member is IMethodSymbol m && m.MethodKind != MethodKind.Ordinary);
    }
}
