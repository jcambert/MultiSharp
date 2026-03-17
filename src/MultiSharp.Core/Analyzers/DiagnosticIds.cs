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

        // P5 — Formatage & Style
        public const string NamingInterfacePrefix  = "MS0501";
        public const string NamingPascalCaseType   = "MS0502";
        public const string NamingCamelCaseParam   = "MS0503";
        public const string NamingPrivateField     = "MS0504";
        public const string UseVarKeyword          = "MS0505";
        public const string UseExpressionBody      = "MS0506";
    }
}
