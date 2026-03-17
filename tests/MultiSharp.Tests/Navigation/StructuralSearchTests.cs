using System.Linq;
using System.Threading.Tasks;
using MultiSharp.Navigation;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Navigation
{
    public class StructuralSearchTests
    {
        [Fact]
        public async Task Search_TrouvePatternNullComparison()
        {
            var code = @"
class C
{
    void M(string s)
    {
        if (s == null) { }
        if (s != null) { }
    }
}";
            var solution = SolutionTestHelper.CreateSolution(code);
            // Pattern exact: s == null
            var results = await StructuralSearchService.SearchAsync(solution, "s == null");

            Assert.NotEmpty(results);
            Assert.Single(results);
        }

        [Fact]
        public async Task Search_RetourneVideSiPatternIntrouvable()
        {
            var code = @"class C { void M() { int x = 1; } }";
            var solution = SolutionTestHelper.CreateSolution(code);
            var results = await StructuralSearchService.SearchAsync(solution, "XXXX.YYYY()");

            Assert.Empty(results);
        }

        [Fact]
        public async Task Search_TrouvePatternExact()
        {
            var code = @"
class C
{
    void M()
    {
        int a = 1 + 2;
        int b = 1 + 2;
        int c = 3 + 4;
    }
}";
            var solution = SolutionTestHelper.CreateSolution(code);
            var results = await StructuralSearchService.SearchAsync(solution, "1 + 2");

            Assert.Equal(2, results.Count);
        }
    }
}
