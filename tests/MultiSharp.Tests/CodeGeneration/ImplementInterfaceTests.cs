using System.Threading.Tasks;
using MultiSharp.CodeGeneration;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.CodeGeneration
{
    public class ImplementInterfaceTests
    {
        private static readonly ImplementInterfaceRefactoring Provider = new();

        [Fact]
        public async Task ProposeImplementation_SurClasseAvecInterfaceNonImplementee()
        {
            var code = @"
interface IGreeter { void Greet(); }
class [|MyGreeter|] : IGreeter { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }

        [Fact]
        public async Task PasDeProposition_SurClasseSansInterface()
        {
            var code = @"class [|Plain|] { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task PasDeProposition_SiInterfaceDejaImplementee()
        {
            var code = @"
interface IGreeter { void Greet(); }
class [|MyGreeter|] : IGreeter
{
    public void Greet() { }
}";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.False(found);
        }

        [Fact]
        public async Task ProposeImplementation_InterfaceAvecPropriete()
        {
            var code = @"
interface IValue { int Value { get; } }
class [|Holder|] : IValue { }";
            var found = await RefactoringTestHelper.HasRefactoringAsync(Provider, code);
            Assert.True(found);
        }
    }
}
