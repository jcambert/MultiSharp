using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Analyzers
{
    public class UnusedUsingAnalyzerTests
    {
        private static readonly UnusedUsingAnalyzer Analyzer = new();

        [Fact]
        public async Task PasDeProbleme_SiUsingUtilise()
        {
            var code = @"
using System;
class C { void M() { Console.WriteLine(); } }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task Probleme_SiUsingInutilise()
        {
            var code = @"
using System.Collections.Generic;
class C { void M() { } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UnusedUsing, 1);
        }

        [Fact]
        public async Task Probleme_SiPlusieursUsingsInutilises()
        {
            var code = @"
using System.Collections.Generic;
using System.Linq;
class C { void M() { } }";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UnusedUsing, 2);
        }

        [Fact]
        public async Task PasDeProbleme_SiAucunUsing()
        {
            var code = @"class C { void M() { } }";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }
    }
}
