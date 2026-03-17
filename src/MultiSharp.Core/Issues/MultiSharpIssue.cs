namespace MultiSharp.Issues
{
    /// <summary>Sévérité d'un problème détecté par MultiSharp.</summary>
    public enum IssueSeverity { Info, Suggestion, Warning, Error }

    /// <summary>
    /// Représente un problème de code détecté par un analyseur MultiSharp.
    /// Ce type est dans MultiSharp.Core — aucune dépendance VS SDK.
    /// </summary>
    public sealed class MultiSharpIssue
    {
        public string RuleId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public IssueSeverity Severity { get; set; } = IssueSeverity.Warning;
        public string FilePath { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public string ProjectName { get; set; } = "";

        public override string ToString() =>
            $"[{Severity}] {RuleId}: {Message} ({FilePath}:{Line})";
    }
}
