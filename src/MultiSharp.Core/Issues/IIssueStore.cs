using System;
using System.Collections.Generic;

namespace MultiSharp.Issues
{
    /// <summary>
    /// Stocke et notifie les problèmes détectés par les analyseurs.
    /// Découple les analyseurs du Tool Window VS.
    /// </summary>
    public interface IIssueStore
    {
        /// <summary>Tous les problèmes courants.</summary>
        IReadOnlyList<MultiSharpIssue> Issues { get; }

        /// <summary>Nombre total de problèmes.</summary>
        int Count { get; }

        /// <summary>Déclenché quand la liste change.</summary>
        event EventHandler IssuesChanged;

        /// <summary>Remplace les problèmes d'un fichier.</summary>
        void SetIssuesForFile(string filePath, IEnumerable<MultiSharpIssue> issues);

        /// <summary>Supprime tous les problèmes d'un fichier.</summary>
        void ClearIssuesForFile(string filePath);

        /// <summary>Vide tous les problèmes.</summary>
        void Clear();
    }
}
