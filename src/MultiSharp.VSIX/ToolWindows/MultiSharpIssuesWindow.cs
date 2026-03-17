using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace MultiSharp.ToolWindows
{
    /// <summary>
    /// Tool Window "MultiSharp Issues" — fenêtre ancrable dans VS
    /// affichant tous les problèmes détectés par les analyseurs.
    /// </summary>
    [Guid(WindowGuidString)]
    public sealed class MultiSharpIssuesWindow : ToolWindowPane
    {
        public const string WindowGuidString = "b2c3d4e5-f6a7-8901-bcde-f12345678901";
        public const string WindowTitle = "MultiSharp Issues";

        private readonly MultiSharpIssuesControl _control;

        public MultiSharpIssuesWindow() : base(null)
        {
            Caption = WindowTitle;
            _control = new MultiSharpIssuesControl();
            Content = _control;
        }

        /// <summary>
        /// Rafraîchit la liste des problèmes affichés.
        /// Appelé par le package quand l'IssueStore change.
        /// </summary>
        public void RefreshIssues(System.Collections.Generic.IReadOnlyList<MultiSharp.Issues.MultiSharpIssue> issues)
        {
            _control.RefreshIssues(issues);
        }
    }
}
