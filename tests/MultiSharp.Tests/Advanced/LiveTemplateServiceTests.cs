using System.Collections.Generic;
using MultiSharp.Advanced;
using Xunit;

namespace MultiSharp.Tests.Advanced
{
    public class LiveTemplateServiceTests
    {
        private readonly LiveTemplateService _service = new();

        [Fact]
        public void TemplatesBuiltInCharges()
        {
            var all = _service.GetAll();
            Assert.NotEmpty(all);
            Assert.Contains(all, t => t.Shortcut == "prop");
            Assert.Contains(all, t => t.Shortcut == "ctor");
            Assert.Contains(all, t => t.Shortcut == "foreach");
        }

        [Fact]
        public void FindByShortcut_TrouveTemplate()
        {
            var template = _service.FindByShortcut("prop");
            Assert.NotNull(template);
            Assert.Equal("Propriété automatique", template!.Description);
        }

        [Fact]
        public void FindByShortcut_RetourneNullSiInexistant()
        {
            var template = _service.FindByShortcut("nonexistent");
            Assert.Null(template);
        }

        [Fact]
        public void Expand_SubstitueVariables()
        {
            var template = _service.FindByShortcut("prop")!;
            var expanded = _service.Expand(template, new Dictionary<string, string>
            {
                { "type", "int" },
                { "Name", "Value" }
            });
            Assert.Contains("int", expanded);
            Assert.Contains("Value", expanded);
        }

        [Fact]
        public void GetByContext_FiltreParContexte()
        {
            var classTemplates = _service.GetByContext(TemplateContext.InClass);
            Assert.NotEmpty(classTemplates);
            Assert.All(classTemplates, t =>
                Assert.True(t.Context == TemplateContext.InClass || t.Context == TemplateContext.Any));
        }

        [Fact]
        public void Register_AjouteTemplate()
        {
            var custom = new LiveTemplate("mytemplate", "Mon template", TemplateContext.Any,
                "custom body", new[] { "param" });
            _service.Register(custom);
            Assert.NotNull(_service.FindByShortcut("mytemplate"));
        }
    }
}
