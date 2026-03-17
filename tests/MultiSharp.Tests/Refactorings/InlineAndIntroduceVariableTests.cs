using System.Threading.Tasks;
using MultiSharp.Refactorings;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Refactorings
{
    public class InlineMethodRefactoringTests
    {
        private static readonly InlineMethodRefactoring Provider = new();

        [Fact]
        public async Task ProposeRefactoring_SurAppelMethodePriveeSimple()
        {
            var code = @"
class C
{
    void M()
    {
        int x = [|Add|](1, 2);
    }
    private int Add(int a, int b) { return a + b; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurMethodePublique()
        {
            var code = @"
class C
{
    void M()
    {
        int x = [|Add|](1, 2);
    }
    public int Add(int a, int b) { return a + b; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurMethodeMultiStatements()
        {
            var code = @"
class C
{
    void M()
    {
        [|DoStuff|]();
    }
    private void DoStuff()
    {
        int x = 1;
        int y = 2;
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }

    public class IntroduceVariableRefactoringTests
    {
        private static readonly IntroduceVariableRefactoring Provider = new();

        [Fact]
        public async Task ProposeRefactoring_SurExpression()
        {
            var code = @"
class C
{
    void M()
    {
        System.Console.WriteLine([|1 + 2|]);
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurLitteral()
        {
            var code = @"
class C
{
    void M()
    {
        System.Console.WriteLine([|42|]);
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurIdentifiantSimple()
        {
            var code = @"
class C
{
    void M(int x)
    {
        System.Console.WriteLine([|x|]);
    }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
