using System.Threading.Tasks;
using MultiSharp.CodeGeneration;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.CodeGeneration
{
    public class GenerateEqualsHashCodeTests
    {
        private static readonly GenerateEqualsHashCodeRefactoring Provider = new();

        [Fact]
        public async Task ProposeGeneration_SurClasseAvecMembres()
        {
            var code = @"
class [|Point|]
{
    public int X { get; set; }
    public int Y { get; set; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeGeneration_SiEqualsExisteDeja()
        {
            var code = @"
class [|Point|]
{
    public int X;
    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeGeneration_SurClasseVide()
        {
            var code = @"class [|Empty|] { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
