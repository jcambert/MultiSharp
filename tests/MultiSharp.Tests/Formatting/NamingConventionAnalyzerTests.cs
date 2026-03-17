using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Formatting;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Formatting
{
    public class NamingConventionAnalyzerTests
    {
        private static readonly NamingConventionAnalyzer Analyzer = new();

        [Fact]
        public async Task Alerte_InterfaceSansPrefixeI()
        {
            var code = @"interface Service { void Run(); }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.NamingInterfacePrefix, 1);
        }

        [Fact]
        public async Task PasAlerte_InterfaceAvecPrefixeI()
        {
            var code = @"interface IService { void Run(); }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code,
                DiagnosticIds.NamingInterfacePrefix);
        }

        [Fact]
        public async Task Alerte_ClasseNonPascalCase()
        {
            var code = @"class myService { }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.NamingPascalCaseType, 1);
        }

        [Fact]
        public async Task PasAlerte_ClassePascalCase()
        {
            var code = @"class MyService { }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code,
                DiagnosticIds.NamingPascalCaseType);
        }

        [Fact]
        public async Task Alerte_ParametreNonCamelCase()
        {
            var code = @"class C { void M(int MyParam) { } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.NamingCamelCaseParam, 1);
        }

        [Fact]
        public async Task PasAlerte_ParametreCamelCase()
        {
            var code = @"class C { void M(int myParam) { } }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code,
                DiagnosticIds.NamingCamelCaseParam);
        }

        [Fact]
        public async Task Alerte_ChampPriveSansUnderscore()
        {
            var code = @"class C { private int count; }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.NamingPrivateField, 1);
        }

        [Fact]
        public async Task PasAlerte_ChampPriveAvecUnderscore()
        {
            var code = @"class C { private int _count; }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code,
                DiagnosticIds.NamingPrivateField);
        }

        [Fact]
        public async Task PasAlerte_ChampStatique()
        {
            var code = @"class C { private static int counter; }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code,
                DiagnosticIds.NamingPrivateField);
        }
    }
}
