using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace MultiSharp.Services
{
    /// <summary>
    /// Implémentation de <see cref="IRoslynWorkspaceService"/> via MEF + VisualStudioWorkspace.
    /// Le workspace est fourni par le composant Roslyn embarqué dans VS — les projets analysés
    /// n'ont aucune dépendance ajoutée.
    /// </summary>
    [Export(typeof(IRoslynWorkspaceService))]
    internal sealed class RoslynWorkspaceService : IRoslynWorkspaceService
    {
        private readonly Lazy<VisualStudioWorkspace?> _workspace;

        [ImportingConstructor]
        public RoslynWorkspaceService()
        {
            _workspace = new Lazy<VisualStudioWorkspace?>(ResolveWorkspace);
        }

        public Workspace? CurrentWorkspace => _workspace.Value;

        public Solution? CurrentSolution => _workspace.Value?.CurrentSolution;

        public IEnumerable<Project> GetCSharpProjects()
        {
            var solution = CurrentSolution;
            if (solution is null)
                return Enumerable.Empty<Project>();

            return solution.Projects
                .Where(p => p.Language == LanguageNames.CSharp);
        }

        public IEnumerable<Document> GetAllDocuments()
        {
            return GetCSharpProjects()
                .SelectMany(p => p.Documents);
        }

        private static VisualStudioWorkspace? ResolveWorkspace()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var componentModel = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                return componentModel?.GetService<VisualStudioWorkspace>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
