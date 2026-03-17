using System.Threading.Tasks;
using MultiSharp.Refactorings;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Refactorings
{
    public class RenameRefactoringTests
    {
        private static readonly RenameRefactoring Provider = new();

        [Fact]
        public async Task ProposeRefactoring_SurMethode()
        {
            var code = @"
class C
{
    void [|MyMethod|]() { }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task ProposeRefactoring_SurVariable()
        {
            var code = @"
class C
{
    void M()
    {
        int [|myVar|] = 0;
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurMotCle()
        {
            var code = @"
[|class|] C { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task ProposeRefactoring_SurPropriete()
        {
            var code = @"
class C
{
    public int [|MyProp|] { get; set; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task ProposeRefactoring_SurClasse()
        {
            var code = @"
class [|MyClass|] { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }
    }
}
