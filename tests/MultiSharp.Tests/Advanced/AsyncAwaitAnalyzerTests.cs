using System.Threading.Tasks;
using MultiSharp.Advanced;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Advanced
{
    public class AsyncAwaitAnalyzerTests
    {
        private static readonly AsyncAwaitAnalyzer Analyzer = new();

        [Fact]
        public async Task Alerte_SurResultBloquant()
        {
            var code = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var t = Task.FromResult(1);
        int x = t.Result;
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.AsyncBlockingCall, 1);
        }

        [Fact]
        public async Task Alerte_SurAsyncVoid()
        {
            var code = @"
using System.Threading.Tasks;
class C
{
    async void BadMethod() { await Task.Delay(1); }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.AsyncVoidMethod, 1);
        }

        [Fact]
        public async Task PasAlerte_EventHandlerAsyncVoid()
        {
            var code = @"
using System;
using System.Threading.Tasks;
class C
{
    async void OnClick(object sender, EventArgs e) { await Task.Delay(1); }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.AsyncVoidMethod);
        }

        [Fact]
        public async Task Alerte_AsyncSansAwait()
        {
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task<int> Compute() { return 42; }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.AsyncWithoutAwait, 1);
        }

        [Fact]
        public async Task PasAlerte_AsyncAvecAwait()
        {
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task<int> Compute()
    {
        await Task.Delay(1);
        return 42;
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.AsyncWithoutAwait);
        }
    }
}
