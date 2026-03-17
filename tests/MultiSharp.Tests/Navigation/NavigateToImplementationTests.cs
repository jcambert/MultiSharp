using System.Threading.Tasks;
using MultiSharp.Navigation;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Navigation
{
    public class NavigateToImplementationTests
    {
        private static readonly NavigateToImplementationRefactoring Provider = new();

        [Fact]
        public async Task ProposeNavigation_SurInterfaceImplementee()
        {
            var code = @"
interface [|IService|] { void Run(); }
class MyService : IService { public void Run() { } }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeNavigation_SurInterfaceSansImplementation()
        {
            var code = @"interface [|IOrphan|] { void Run(); }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeNavigation_SurClasseConcrete()
        {
            var code = @"class [|Plain|] { void M() { } }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
