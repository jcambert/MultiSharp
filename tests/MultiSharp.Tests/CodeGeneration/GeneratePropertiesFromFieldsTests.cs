using System.Threading.Tasks;
using MultiSharp.CodeGeneration;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.CodeGeneration
{
    public class GeneratePropertiesFromFieldsTests
    {
        private static readonly GeneratePropertiesFromFieldsRefactoring Provider = new();

        [Fact]
        public async Task ProposeGeneration_SurClasseAvecChampsPrives()
        {
            var code = @"
class [|Customer|]
{
    private int _id;
    private string _name;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeGeneration_SiProprietesExistentDeja()
        {
            var code = @"
class [|Customer|]
{
    private int _id;
    public int Id { get => _id; set => _id = value; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeGeneration_SurChampsPublics()
        {
            var code = @"
class [|Customer|]
{
    public int Id;
    public static int Count;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task ProposeGeneration_AvecUnderscorePrefix()
        {
            var code = @"
class [|Service|]
{
    private string _url;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }
    }
}
