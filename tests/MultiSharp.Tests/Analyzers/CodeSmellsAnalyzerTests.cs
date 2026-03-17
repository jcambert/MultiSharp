using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Analyzers
{
    public class CodeSmellsAnalyzerTests
    {
        private static CodeSmellsAnalyzer MakeAnalyzer(
            int maxLines = 50, int maxParams = 5, int maxDepth = 4)
            => new() { MaxMethodLines = maxLines, MaxParameters = maxParams, MaxNestingDepth = maxDepth };

        // ── Trop de paramètres ────────────────────────────────────────────

        [Fact]
        public async Task PasDeProbleme_ParametresInferieursAuMax()
        {
            var code = @"class C { void M(int a, int b) {} }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(MakeAnalyzer(maxParams: 5), code);
        }

        [Fact]
        public async Task Probleme_TropDeParametres()
        {
            var code = @"class C { void M(int a, int b, int c) {} }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                MakeAnalyzer(maxParams: 2), code, DiagnosticIds.TooManyParameters, 1);
        }

        // ── Nesting trop profond ──────────────────────────────────────────

        [Fact]
        public async Task PasDeProbleme_NestingAcceptable()
        {
            var code = @"
class C {
    void M() {
        if (true) {
            if (true) { }
        }
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(MakeAnalyzer(maxDepth: 4), code);
        }

        [Fact]
        public async Task Probleme_NestingTropProfond()
        {
            var code = @"
class C {
    void M() {
        if (true) {
            if (true) {
                if (true) { }
            }
        }
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                MakeAnalyzer(maxDepth: 2), code, DiagnosticIds.NestingTooDeep, 1);
        }

        // ── Méthode trop longue ───────────────────────────────────────────

        [Fact]
        public async Task PasDeProbleme_MethodeCorte()
        {
            var code = @"
class C {
    void M() {
        int x = 1;
        System.Console.WriteLine(x);
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(MakeAnalyzer(maxLines: 50), code);
        }
    }
}
