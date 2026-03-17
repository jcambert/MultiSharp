using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using MultiSharp.Options;

namespace MultiSharp.Options
{
    /// <summary>
    /// Page d'options VS (Tools > Options > MultiSharp).
    /// Wraps <see cref="MultiSharpSettings"/> pour la persistance dans le VS settings store.
    /// </summary>
    public class MultiSharpOptions : DialogPage
    {
        // ── Général ──────────────────────────────────────────────────────────

        [Category("Général")]
        [DisplayName("Activer MultiSharp")]
        [Description("Active ou désactive l'ensemble de l'extension MultiSharp.")]
        [DefaultValue(true)]
        public bool IsEnabled { get; set; } = true;

        // ── Analyse de code ───────────────────────────────────────────────────

        [Category("Analyse de code")]
        [DisplayName("Activer l'analyse")]
        [Description("Active les analyseurs de code (variables inutilisées, null ref, code smells…).")]
        [DefaultValue(true)]
        public bool AnalysisEnabled { get; set; } = true;

        [Category("Analyse de code")]
        [DisplayName("Sévérité par défaut")]
        [Description("Sévérité appliquée aux règles dont la sévérité n'est pas configurée individuellement.")]
        [DefaultValue(DiagnosticSeverityOption.Warning)]
        public DiagnosticSeverityOption DefaultSeverity { get; set; } = DiagnosticSeverityOption.Warning;

        [Category("Analyse de code")]
        [DisplayName("Analyse solution-wide")]
        [Description("Analyse tous les fichiers de la solution, pas seulement ceux ouverts.")]
        [DefaultValue(false)]
        public bool SolutionWideAnalysis { get; set; } = false;

        // ── Règles individuelles ─────────────────────────────────────────────

        [Category("Règles — Variables")]
        [DisplayName("Variables inutilisées")]
        [Description("Signale les variables locales et paramètres non utilisés.")]
        [DefaultValue(true)]
        public bool UnusedVariables { get; set; } = true;

        [Category("Règles — Variables")]
        [DisplayName("Membres privés inutilisés")]
        [Description("Signale les champs, méthodes et propriétés privés jamais référencés.")]
        [DefaultValue(true)]
        public bool UnusedPrivateMembers { get; set; } = true;

        [Category("Règles — Null Safety")]
        [DisplayName("Déréférencement potentiellement nul")]
        [Description("Signale les accès membres sans vérification null préalable.")]
        [DefaultValue(true)]
        public bool NullReferenceAnalysis { get; set; } = true;

        [Category("Règles — Style")]
        [DisplayName("Simplifications d'expressions")]
        [Description("Suggère des simplifications (?.  ?? etc.).")]
        [DefaultValue(true)]
        public bool ExpressionSimplifications { get; set; } = true;

        [Category("Règles — Style")]
        [DisplayName("Using inutilisés")]
        [Description("Signale les directives using non utilisées.")]
        [DefaultValue(true)]
        public bool UnusedUsings { get; set; } = true;

        // ── Code smells ───────────────────────────────────────────────────────

        [Category("Code Smells")]
        [DisplayName("Longueur maximale d'une méthode (lignes)")]
        [Description("Signale les méthodes dépassant ce nombre de lignes.")]
        [DefaultValue(50)]
        public int MaxMethodLines { get; set; } = 50;

        [Category("Code Smells")]
        [DisplayName("Nombre maximal de paramètres")]
        [Description("Signale les méthodes avec trop de paramètres.")]
        [DefaultValue(5)]
        public int MaxParameters { get; set; } = 5;

        [Category("Code Smells")]
        [DisplayName("Profondeur d'imbrication maximale")]
        [Description("Signale les blocs imbriqués au-delà de ce niveau.")]
        [DefaultValue(4)]
        public int MaxNestingDepth { get; set; } = 4;

        // ── Refactoring ───────────────────────────────────────────────────────

        [Category("Refactoring")]
        [DisplayName("Activer les refactorings")]
        [Description("Active les suggestions de refactoring (Extract Method, Rename, etc.).")]
        [DefaultValue(true)]
        public bool RefactoringEnabled { get; set; } = true;

        /// <summary>
        /// Exporte les paramètres courants vers un <see cref="MultiSharpSettings"/>
        /// utilisable par les analyseurs (sans dépendance VS SDK).
        /// </summary>
        public MultiSharpSettings ToSettings() => new()
        {
            IsEnabled = IsEnabled,
            AnalysisEnabled = AnalysisEnabled,
            DefaultSeverity = DefaultSeverity,
            SolutionWideAnalysis = SolutionWideAnalysis,
            UnusedVariables = UnusedVariables,
            UnusedPrivateMembers = UnusedPrivateMembers,
            NullReferenceAnalysis = NullReferenceAnalysis,
            ExpressionSimplifications = ExpressionSimplifications,
            UnusedUsings = UnusedUsings,
            MaxMethodLines = MaxMethodLines,
            MaxParameters = MaxParameters,
            MaxNestingDepth = MaxNestingDepth,
            RefactoringEnabled = RefactoringEnabled,
        };
    }
}
