using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MultiSharp.Services
{
    /// <summary>
    /// Fournit l'accès au workspace Roslyn de la solution ouverte dans Visual Studio.
    /// Zéro dépendance ajoutée aux projets analysés — tout s'exécute dans le process VS.
    /// </summary>
    public interface IRoslynWorkspaceService
    {
        /// <summary>Workspace Roslyn courant (peut être null si aucune solution ouverte).</summary>
        Workspace? CurrentWorkspace { get; }

        /// <summary>Solution courante (null si aucune solution ouverte).</summary>
        Solution? CurrentSolution { get; }

        /// <summary>Tous les projets C# de la solution courante.</summary>
        IEnumerable<Project> GetCSharpProjects();

        /// <summary>Tous les documents C# de la solution courante.</summary>
        IEnumerable<Document> GetAllDocuments();
    }
}
