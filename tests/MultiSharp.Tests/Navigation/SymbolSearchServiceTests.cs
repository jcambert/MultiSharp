using System.Threading.Tasks;
using MultiSharp.Navigation;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Navigation
{
    public class SymbolSearchServiceTests
    {
        [Theory]
        [InlineData("MyClass", "MyClass", 100)]
        [InlineData("MyClass", "My", 80)]
        [InlineData("MyClass", "Class", 60)]
        [InlineData("GetCustomer", "GC", 50)]
        [InlineData("MyClass", "MC", 50)]
        [InlineData("MyClass", "MyCls", 30)]
        [InlineData("MyClass", "xyz", 0)]
        public void FuzzyScore_RetourneScoreAttendu(string name, string query, int expectedMinScore)
        {
            var score = SymbolSearchService.FuzzyScore(name, query);
            Assert.True(score >= expectedMinScore,
                $"FuzzyScore('{name}', '{query}') = {score}, attendu >= {expectedMinScore}");
        }

        [Fact]
        public async Task Search_TrouveTypeDansProjet()
        {
            var code = @"
namespace MyApp
{
    public class CustomerService { }
    public class OrderService { }
}";
            var solution = SolutionTestHelper.CreateSolution(code);
            var results = await SymbolSearchService.SearchAsync(solution, "Customer");

            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Symbol.Name == "CustomerService");
        }

        [Fact]
        public async Task Search_RetourneVideSiQueryVide()
        {
            var code = @"class C { }";
            var solution = SolutionTestHelper.CreateSolution(code);
            var results = await SymbolSearchService.SearchAsync(solution, "");

            Assert.Empty(results);
        }

        [Fact]
        public async Task Search_TrouveMembresAvecFiltreMembers()
        {
            var code = @"
class MyService
{
    public void ProcessOrder() { }
    public void ProcessPayment() { }
}";
            var solution = SolutionTestHelper.CreateSolution(code);
            var results = await SymbolSearchService.SearchAsync(solution, "Process",
                filter: SymbolFilter.Members);

            Assert.True(results.Count >= 2);
        }

        [Fact]
        public async Task Search_TrieParScoreDecroissant()
        {
            var code = @"
class SearchHelper { }
class MySearch { }
class SearchEngine { }";
            var solution = SolutionTestHelper.CreateSolution(code);
            var results = await SymbolSearchService.SearchAsync(solution, "Search");

            for (int i = 1; i < results.Count; i++)
                Assert.True(results[i - 1].Score >= results[i].Score);
        }
    }
}
