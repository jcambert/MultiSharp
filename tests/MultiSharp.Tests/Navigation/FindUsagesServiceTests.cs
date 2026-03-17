using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MultiSharp.Navigation;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Navigation
{
    public class FindUsagesServiceTests
    {
        [Fact]
        public async Task FindUsages_TrouveReferencesMethode()
        {
            var code = @"
class C
{
    void Helper() { }
    void M() { Helper(); Helper(); }
}";
            var solution = SolutionTestHelper.CreateSolution(code);
            var document = solution.Projects.First().Documents.First();

            var root = await document.GetSyntaxRootAsync();
            var model = await document.GetSemanticModelAsync();

            var method = root!.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == "Helper");
            var symbol = model!.GetDeclaredSymbol(method)!;

            var usages = await FindUsagesService.FindUsagesAsync(symbol, solution);
            Assert.Equal(2, usages.Count);
        }

        [Fact]
        public async Task FindUsages_RetourneVideSiAucuneReference()
        {
            var code = @"
class C
{
    void Unused() { }
    void M() { }
}";
            var solution = SolutionTestHelper.CreateSolution(code);
            var document = solution.Projects.First().Documents.First();

            var root = await document.GetSyntaxRootAsync();
            var model = await document.GetSemanticModelAsync();

            var method = root!.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == "Unused");
            var symbol = model!.GetDeclaredSymbol(method)!;

            var usages = await FindUsagesService.FindUsagesAsync(symbol, solution);
            Assert.Empty(usages);
        }

        [Fact]
        public async Task FindImplementations_TrouveImplementations()
        {
            var code = @"
interface IGreeter { void Greet(); }
class FrenchGreeter : IGreeter { public void Greet() { } }
class EnglishGreeter : IGreeter { public void Greet() { } }";

            var solution = SolutionTestHelper.CreateSolution(code);
            var document = solution.Projects.First().Documents.First();

            var root = await document.GetSyntaxRootAsync();
            var model = await document.GetSemanticModelAsync();

            var iface = root!.DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .First();
            var symbol = model!.GetDeclaredSymbol(iface)!;

            var impls = await FindUsagesService.FindImplementationsAsync(symbol, solution);
            Assert.Equal(2, impls.Count);
        }
    }
}
