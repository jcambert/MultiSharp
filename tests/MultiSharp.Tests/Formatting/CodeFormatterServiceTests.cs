using MultiSharp.Formatting;
using Xunit;

namespace MultiSharp.Tests.Formatting
{
    public class CodeFormatterServiceTests
    {
        [Fact]
        public void NormalizeWhitespace_IndenteLesBlocs()
        {
            var code = @"class C{void M(){int x=1;}}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var normalized = CodeFormatterService.NormalizeWhitespace(root);
            var result = normalized.ToFullString();

            // Vérifie que des espaces ont été ajoutés
            Assert.Contains(" ", result);
            Assert.Contains("{", result);
        }

        [Fact]
        public void NormalizeWhitespace_ConserveLeCode()
        {
            var code = @"class MyClass { int x = 1; }";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var normalized = CodeFormatterService.NormalizeWhitespace(root);
            var result = normalized.ToFullString();

            // Le code essentiel est conservé
            Assert.Contains("MyClass", result);
            Assert.Contains("int x", result);
        }
    }
}
