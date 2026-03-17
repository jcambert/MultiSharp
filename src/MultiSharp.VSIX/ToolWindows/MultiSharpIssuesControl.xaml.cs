using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using MultiSharp.Issues;

namespace MultiSharp.ToolWindows
{
    public partial class MultiSharpIssuesControl : UserControl
    {
        private readonly ObservableCollection<IssueViewModel> _allIssues = new();

        public MultiSharpIssuesControl()
        {
            InitializeComponent();
        }

        /// <summary>Rafraîchit la liste depuis le thread UI.</summary>
        public void RefreshIssues(IReadOnlyList<MultiSharpIssue> issues)
        {
            _allIssues.Clear();
            foreach (var issue in issues)
                _allIssues.Add(new IssueViewModel(issue));

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var severityTag = (SeverityFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "All";
            var search = SearchBox.Text?.Trim() ?? "";

            var filtered = _allIssues.AsEnumerable();

            if (severityTag != "All")
                filtered = filtered.Where(i => i.Severity.ToString() == severityTag);

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(i =>
                    i.Message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    i.FilePath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    i.RuleId.Contains(search, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();
            IssuesList.ItemsSource = list;
            StatusText.Text = $"{list.Count} problème(s)";
        }

        private void OnFilterChanged(object sender, EventArgs e) => ApplyFilters();

        private void OnClearClick(object sender, System.Windows.RoutedEventArgs e)
        {
            _allIssues.Clear();
            ApplyFilters();
        }

        private void OnIssueDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IssuesList.SelectedItem is IssueViewModel vm)
                NavigateToIssue(vm);
        }

        private static void NavigateToIssue(IssueViewModel vm)
        {
            // Navigation vers le fichier/ligne — sera complété à US-403
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider
                .GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            if (dte == null || string.IsNullOrEmpty(vm.FilePath)) return;

            try
            {
                dte.ItemOperations.OpenFile(vm.FilePath);
                var selection = dte.ActiveDocument?.Selection as EnvDTE.TextSelection;
                selection?.GotoLine(vm.Line, Select: false);
            }
            catch { /* navigation best-effort */ }
        }
    }

    /// <summary>ViewModel pour l'affichage d'un problème dans la liste.</summary>
    internal sealed class IssueViewModel
    {
        private readonly MultiSharpIssue _issue;

        public IssueViewModel(MultiSharpIssue issue) => _issue = issue;

        public string RuleId => _issue.RuleId;
        public string Message => _issue.Message;
        public string FilePath => _issue.FilePath;
        public int Line => _issue.Line;
        public string ProjectName => _issue.ProjectName;
        public IssueSeverity Severity => _issue.Severity;

        public string ShortFilePath => System.IO.Path.GetFileName(_issue.FilePath);

        public string SeverityIcon => _issue.Severity switch
        {
            IssueSeverity.Error      => "✖",
            IssueSeverity.Warning    => "⚠",
            IssueSeverity.Suggestion => "💡",
            IssueSeverity.Info       => "ℹ",
            _                        => ""
        };
    }
}
