using System.Threading.Tasks;
using MultiSharp.Refactorings;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Refactorings
{
    public class ExtractInterfaceRefactoringTests
    {
        private static readonly ExtractInterfaceRefactoring Provider = new();

        [Fact]
        public async Task ProposeRefactoring_SurNomDeClasse_AvecMembresPublics()
        {
            var code = @"
class [|MyService|]
{
    public void DoWork() { }
    public int Value { get; set; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurNomDeClasse_SansMembresPublics()
        {
            var code = @"
class [|MyService|]
{
    private void InternalWork() { }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurAutreToken()
        {
            var code = @"
[|class|] MyService
{
    public void DoWork() { }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task ProposeRefactoring_MembresStatiquesIgnores()
        {
            var code = @"
class [|MyService|]
{
    public static void StaticMethod() { }
    public void InstanceMethod() { }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found); // InstanceMethod est inclus
        }
    }
}
