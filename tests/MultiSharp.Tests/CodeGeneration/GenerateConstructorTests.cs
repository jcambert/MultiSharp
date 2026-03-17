using System.Threading.Tasks;
using MultiSharp.CodeGeneration;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.CodeGeneration
{
    public class GenerateConstructorTests
    {
        private static readonly GenerateConstructorRefactoring Provider = new();

        [Fact]
        public async Task ProposeGeneration_SurClasseAvecChamps()
        {
            var code = @"
class [|MyClass|]
{
    private int _id;
    private string _name;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeGeneration_SurClasseVide()
        {
            var code = @"class [|MyClass|] { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeGeneration_SurChampsStatiquesUniquement()
        {
            var code = @"
class [|MyClass|]
{
    private static int s_counter;
    private const int MaxSize = 100;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeGeneration_SiConstructeurExisteDeja()
        {
            var code = @"
class [|MyClass|]
{
    private int _id;
    public MyClass(int id) { _id = id; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeGeneration_SurAutreToken()
        {
            var code = @"
[|class|] MyClass
{
    private int _id;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
