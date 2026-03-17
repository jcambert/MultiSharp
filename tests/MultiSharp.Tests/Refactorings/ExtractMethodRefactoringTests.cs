using System.Threading.Tasks;
using MultiSharp.Refactorings;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Refactorings
{
    public class ExtractMethodRefactoringTests
    {
        private static readonly ExtractMethodRefactoring Provider = new();

        [Fact]
        public async Task ProposeRefactoring_SurStatements()
        {
            var code = @"
class C
{
    void M()
    {
        [|int x = 1;
        int y = 2;|]
        int z = x + y;
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurSelectionVide()
        {
            var code = @"
class C
{
    void M()
    {
        int x = 1;
    }
}";
            // Span vide = pas de sélection
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task ProposeRefactoring_StatementUnique()
        {
            var code = @"
class C
{
    void M(int n)
    {
        [|int result = n * 2;|]
        System.Console.WriteLine(result);
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_HorsMethode()
        {
            // Les statements doivent être dans une méthode
            var code = @"
class C
{
    [|int x = 1;|]
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
