using System.Threading.Tasks;
using MultiSharp.Advanced;
using MultiSharp.Analyzers;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Advanced
{
    public class LinqOptimizationAnalyzerTests
    {
        private static readonly LinqOptimizationAnalyzer Analyzer = new();

        [Fact]
        public async Task Alerte_WhereFirst()
        {
            var code = @"
using System.Linq;
using System.Collections.Generic;
class C
{
    int M(List<int> list)
    {
        return list.Where(x => x > 0).First();
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.LinqWhereFirst, 1);
        }

        [Fact]
        public async Task Alerte_CountSuperiorZero()
        {
            var code = @"
using System.Linq;
using System.Collections.Generic;
class C
{
    bool M(List<int> list)
    {
        return list.Count() > 0;
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.LinqCountNotAny, 1);
        }

        [Fact]
        public async Task Alerte_WhereCount()
        {
            var code = @"
using System.Linq;
using System.Collections.Generic;
class C
{
    int M(List<int> list)
    {
        return list.Where(x => x > 0).Count();
    }
}";
            await AnalyzerTestHelper.VerifyDiagnosticCountAsync(
                Analyzer, code, DiagnosticIds.LinqWhereCount, 1);
        }

        [Fact]
        public async Task PasAlerte_FirstAvecPredicate()
        {
            var code = @"
using System.Linq;
using System.Collections.Generic;
class C
{
    int M(List<int> list)
    {
        return list.First(x => x > 0);
    }
}";
            await AnalyzerTestHelper.VerifyNoDiagnosticsAsync(Analyzer, code, DiagnosticIds.LinqWhereFirst);
        }
    }
}
