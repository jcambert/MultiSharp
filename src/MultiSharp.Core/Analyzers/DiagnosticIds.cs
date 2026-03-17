namespace MultiSharp.Analyzers
{
    /// <summary>Identifiants des règles MultiSharp.</summary>
    public static class DiagnosticIds
    {
        // P1 — Analyse de code
        public const string UnusedLocalVariable  = "MS0101";
        public const string UnusedParameter      = "MS0102";
        public const string UnusedPrivateMember  = "MS0103";
        public const string PossibleNullRef       = "MS0104";
        public const string RedundantBoolComparison = "MS0105";
        public const string RedundantCast         = "MS0106";
        public const string UnusedUsing           = "MS0107";
        public const string MethodTooLong         = "MS0108";
        public const string TooManyParameters     = "MS0109";
        public const string NestingTooDeep        = "MS0110";
    }
}
