using System.Threading.Tasks;
using MultiSharp.Refactorings;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Refactorings
{
    public class ConvertToStaticRefactoringTests
    {
        private static readonly ConvertToStaticRefactoring Provider = new();

        [Fact]
        public async Task ProposeRefactoring_SurMethodeSansThis()
        {
            var code = @"
class C
{
    int [|Add|](int a, int b) { return a + b; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurMethodeDejaStatique()
        {
            var code = @"
class C
{
    static int [|Add|](int a, int b) { return a + b; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurMethodeAvecThis()
        {
            var code = @"
class C
{
    int _value = 0;
    int [|GetValue|]() { return this._value; }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeRefactoring_SurMethodeOverride()
        {
            var code = @"
class Base { public virtual void [|M|]() { } }
class C : Base { public override void [|M|]() { } }";
            // On sélectionne dans la classe dérivée — override ne peut pas devenir static
            var codeOverride = @"
class Base { public virtual void M() { } }
class C : Base { public override void [|M|]() { } }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, codeOverride);
            Assert.False(found);
        }
    }
}
