namespace MultiSharp.Options
{
    /// <summary>
    /// Modèle POCO des paramètres MultiSharp — sans dépendance VS SDK.
    /// Utilisé par <c>MultiSharpOptions</c> (DialogPage) pour la persistance VS
    /// et directement par les analyseurs pour lire la configuration.
    /// </summary>
    public sealed class MultiSharpSettings
    {
        // ── Général ──────────────────────────────────────────────────────────
        public bool IsEnabled { get; set; } = true;

        // ── Analyse de code ───────────────────────────────────────────────────
        public bool AnalysisEnabled { get; set; } = true;
        public DiagnosticSeverityOption DefaultSeverity { get; set; } = DiagnosticSeverityOption.Warning;
        public bool SolutionWideAnalysis { get; set; } = false;

        // ── Règles individuelles ─────────────────────────────────────────────
        public bool UnusedVariables { get; set; } = true;
        public bool UnusedPrivateMembers { get; set; } = true;
        public bool NullReferenceAnalysis { get; set; } = true;
        public bool ExpressionSimplifications { get; set; } = true;
        public bool UnusedUsings { get; set; } = true;

        // ── Code Smells ───────────────────────────────────────────────────────
        public int MaxMethodLines { get; set; } = 50;
        public int MaxParameters { get; set; } = 5;
        public int MaxNestingDepth { get; set; } = 4;

        // ── Refactoring ───────────────────────────────────────────────────────
        public bool RefactoringEnabled { get; set; } = true;

        /// <summary>Retourne une instance avec toutes les valeurs par défaut.</summary>
        public static MultiSharpSettings Default => new();
    }

    /// <summary>Sévérité configurable pour les règles MultiSharp.</summary>
    public enum DiagnosticSeverityOption
    {
        Info,
        Suggestion,
        Warning,
        Error
    }
}
