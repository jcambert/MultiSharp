using System.Collections.Generic;
using System.Linq;

namespace MultiSharp.Advanced
{
    /// <summary>
    /// US-603 — Service de Live Templates (snippets contextuels).
    /// Gère un catalogue de templates prédéfinis et personnalisés.
    /// </summary>
    public sealed class LiveTemplateService
    {
        private readonly List<LiveTemplate> _templates = new List<LiveTemplate>();

        public LiveTemplateService()
        {
            RegisterBuiltIn();
        }

        private void RegisterBuiltIn()
        {
            Register(new LiveTemplate(
                shortcut: "prop",
                description: "Propriété automatique",
                context: TemplateContext.InClass,
                body: "public $type$ $Name$ { get; set; }",
                variables: new[] { "type", "Name" }));

            Register(new LiveTemplate(
                shortcut: "ctor",
                description: "Constructeur",
                context: TemplateContext.InClass,
                body: "public $ClassName$($params$)\n{\n    $END$\n}",
                variables: new[] { "ClassName", "params" }));

            Register(new LiveTemplate(
                shortcut: "foreach",
                description: "Boucle foreach",
                context: TemplateContext.InMethod,
                body: "foreach (var $item$ in $collection$)\n{\n    $END$\n}",
                variables: new[] { "item", "collection" }));

            Register(new LiveTemplate(
                shortcut: "propfull",
                description: "Propriété avec champ backing",
                context: TemplateContext.InClass,
                body: "private $type$ _$name$;\npublic $type$ $Name$\n{\n    get => _$name$;\n    set => _$name$ = value;\n}",
                variables: new[] { "type", "name", "Name" }));

            Register(new LiveTemplate(
                shortcut: "trycatch",
                description: "Bloc try/catch",
                context: TemplateContext.InMethod,
                body: "try\n{\n    $END$\n}\ncatch ($Exception$ ex)\n{\n    $handler$\n}",
                variables: new[] { "Exception", "handler" }));

            Register(new LiveTemplate(
                shortcut: "singleton",
                description: "Pattern Singleton",
                context: TemplateContext.InClass,
                body: "private static $ClassName$? _instance;\npublic static $ClassName$ Instance => _instance ??= new $ClassName$();",
                variables: new[] { "ClassName" }));
        }

        public void Register(LiveTemplate template) => _templates.Add(template);

        public IReadOnlyList<LiveTemplate> GetAll() => _templates;

        public IReadOnlyList<LiveTemplate> GetByContext(TemplateContext context) =>
            _templates.Where(t => t.Context == context || t.Context == TemplateContext.Any).ToList();

        public LiveTemplate? FindByShortcut(string shortcut) =>
            _templates.FirstOrDefault(t => t.Shortcut == shortcut);

        /// <summary>
        /// Expand un template en substituant les variables par leurs valeurs.
        /// </summary>
        public string Expand(LiveTemplate template, IReadOnlyDictionary<string, string> values)
        {
            var result = template.Body;
            foreach (var kv in values)
                result = result.Replace("$" + kv.Key + "$", kv.Value);
            // Retirer les variables non substituées
            foreach (var v in template.Variables)
                result = result.Replace("$" + v + "$", v);
            return result.Replace("$END$", "");
        }
    }

    public sealed class LiveTemplate
    {
        public string Shortcut { get; }
        public string Description { get; }
        public TemplateContext Context { get; }
        public string Body { get; }
        public IReadOnlyList<string> Variables { get; }

        public LiveTemplate(string shortcut, string description, TemplateContext context,
            string body, string[] variables)
        {
            Shortcut = shortcut;
            Description = description;
            Context = context;
            Body = body;
            Variables = variables;
        }
    }

    public enum TemplateContext
    {
        Any,
        InClass,
        InMethod,
        InNamespace
    }
}
