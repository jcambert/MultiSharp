using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Formatting;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Formatting
{
    public class CodeStyleAnalyzerTests
    {
        private static readonly CodeStyleAnalyzer Analyzer = new();

        [Fact]
        public async Task SuggestsVar_SurTypeExplicite()
        {
            var code = @"
class C
{
    void M()
    {
        System.Collections.Generic.List<int> items = new System.Collections.Generic.List<int>();
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UseVarKeyword, 1);
        }

        [Fact]
        public async Task PasSuggestion_SiDejaVar()
        {
            var code = @"
class C
{
    void M()
    {
        var items = new System.Collections.Generic.List<int>();
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.UseVarKeyword);
        }

        [Fact]
        public async Task SuggestsExpressionBody_SurMethodeUnReturn()
        {
            var code = @"
class C
{
    int GetValue() { return 42; }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UseExpressionBody, 1);
        }

        [Fact]
        public async Task PasSuggestion_SiDejaExpressionBody()
        {
            var code = @"
class C
{
    int GetValue() => 42;
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.UseExpressionBody);
        }

        [Fact]
        public async Task PasSuggestion_SurMethodeMultiStatements()
        {
            var code = @"
class C
{
    int Calc(int a, int b)
    {
        int x = a + b;
        return x * 2;
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.UseExpressionBody);
        }

        [Fact]
        public async Task PasSuggestion_Var_SurLitteral()
        {
            var code = @"
class C
{
    void M()
    {
        int x = 42;
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.UseVarKeyword);
        }
    }
}
