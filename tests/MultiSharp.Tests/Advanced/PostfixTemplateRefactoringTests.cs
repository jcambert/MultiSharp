using System.Threading.Tasks;
using MultiSharp.Advanced;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Advanced
{
    public class PostfixTemplateRefactoringTests
    {
        private static readonly PostfixTemplateRefactoring Provider = new();

        [Fact]
        public async Task ProposeTemplates_SurExpressionDansMethode()
        {
            var code = @"
class C
{
    void M()
    {
        [|GetValue()|];
    }
    int GetValue() => 42;
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeTemplate_SurSelectionVide()
        {
            // Span vide → pas de refactoring
            var code = @"class C { void M() { } }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }
    }
}
