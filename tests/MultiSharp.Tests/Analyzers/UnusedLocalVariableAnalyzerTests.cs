using System.Threading.Tasks;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Analyzers
{
    public class UnusedLocalVariableAnalyzerTests
    {
        private static readonly UnusedLocalVariableAnalyzer Analyzer = new();

        [Fact]
        public async Task PasDeProbleme_SiVariableUtilisee()
        {
            var code = @"
class C {
    void M() {
        int x = 1;
        System.Console.WriteLine(x);
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task PasDeProbleme_SiVariableCommenceParUnderscore()
        {
            var code = @"
class C {
    void M() {
        int _unused = 1;
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task Probleme_SiVariableDeclareeNonUtilisee()
        {
            var code = @"
class C {
    void M() {
        int x = 1;
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UnusedLocalVariable, 1);
        }

        [Fact]
        public async Task Probleme_SiPlusieursVariablesNonUtilisees()
        {
            var code = @"
class C {
    void M() {
        int a = 1;
        string b = ""hello"";
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.UnusedLocalVariable, 2);
        }

        [Fact]
        public async Task PasDeProbleme_SiVariableUtiliseeDansCondition()
        {
            var code = @"
class C {
    void M() {
        bool flag = true;
        if (flag) System.Console.WriteLine();
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }

        [Fact]
        public async Task PasDeProbleme_SiVariableUtiliseeDansReturn()
        {
            var code = @"
class C {
    int M() {
        int result = 42;
        return result;
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code);
        }
    }
}
