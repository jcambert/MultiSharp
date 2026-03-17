using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Analyzers
{
    public class RedundantBoolComparisonTests
    {
        private static readonly RedundantBoolComparisonAnalyzer Analyzer = new();

        [Fact]
        public async Task PasDeProbleme_SiBoolSeul()
        {
            var code = @"class C { void M(bool x) { if (x) {} } }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task Probleme_SiEgalTrue()
        {
            var code = @"class C { void M(bool x) { if (x == true) {} } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.RedundantBoolComparison, 1);
        }

        [Fact]
        public async Task Probleme_SiEgalFalse()
        {
            var code = @"class C { void M(bool x) { if (x == false) {} } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.RedundantBoolComparison, 1);
        }

        [Fact]
        public async Task Probleme_SiDifferentTrue()
        {
            var code = @"class C { void M(bool x) { if (x != true) {} } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.RedundantBoolComparison, 1);
        }

        [Fact]
        public async Task Probleme_SiTrueEgalExpr()
        {
            var code = @"class C { void M(bool x) { if (true == x) {} } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.RedundantBoolComparison, 1);
        }

        [Fact]
        public async Task PasDeProbleme_SiComparaisonNonBool()
        {
            var code = @"class C { void M(int x) { if (x == 1) {} } }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }
    }
}
