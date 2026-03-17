using System.Threading.Tasks;
using MultiSharp.CodeGeneration;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.CodeGeneration
{
    public class GenerateToStringTests
    {
        private static readonly GenerateToStringRefactoring Provider = new();

        [Fact]
        public async Task ProposeGeneration_SurClasseAvecMembresPublics()
        {
            var code = @"
class [|Person|]
{
    public string Name { get; set; }
    public int Age { get; set; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeGeneration_SiToStringExisteDeja()
        {
            var code = @"
class [|Person|]
{
    public string Name { get; set; }
    public override string ToString() => Name;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeGeneration_SansMembresPublics()
        {
            var code = @"
class [|Internal|]
{
    private int _secret;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
