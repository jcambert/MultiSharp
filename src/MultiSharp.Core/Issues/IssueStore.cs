using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiSharp.Issues
{
    /// <summary>
    /// Implémentation thread-safe de <see cref="IIssueStore"/>.
    /// </summary>
    public sealed class IssueStore : IIssueStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, List<MultiSharpIssue>> _byFile = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? IssuesChanged;

        public IReadOnlyList<MultiSharpIssue> Issues
        {
            get
            {
                lock (_lock)
                    return _byFile.Values.SelectMany(x => x).ToList();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                    return _byFile.Values.Sum(x => x.Count);
            }
        }

        public void SetIssuesForFile(string filePath, IEnumerable<MultiSharpIssue> issues)
        {
            lock (_lock)
                _byFile[filePath] = new List<MultiSharpIssue>(issues);

            IssuesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearIssuesForFile(string filePath)
        {
            lock (_lock)
                _byFile.Remove(filePath);

            IssuesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            lock (_lock)
                _byFile.Clear();

            IssuesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
