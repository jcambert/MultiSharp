using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Analyzers
{
    public class UnusedParameterAnalyzerTests
    {
        private static readonly UnusedParameterAnalyzer Analyzer = new();

        [Fact]
        public async Task PasDeProbleme_SiParametreUtilise()
        {
            var code = @"
class C {
    void M(int x) {
        System.Console.WriteLine(x);
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task PasDeProbleme_SiParametreCommenceParUnderscore()
        {
            var code = @"
class C {
    void M(int _unused) { }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task PasDeProbleme_SiOverride()
        {
            var code = @"
abstract class Base {
    public abstract void M(int x);
}
class C : Base {
    public override void M(int x) { }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task Probleme_SiParametreNonUtilise()
        {
            var code = @"
class C {
    void M(int x) { }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UnusedParameter, 1);
        }

        [Fact]
        public async Task Probleme_SiPlusieursParametresNonUtilises()
        {
            var code = @"
class C {
    void M(int x, string y) { }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UnusedParameter, 2);
        }

        [Fact]
        public async Task PasDeProbleme_SiParametreUtiliseDansExpressionBody()
        {
            var code = @"
class C {
    int M(int x) => x * 2;
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }
    }
}
