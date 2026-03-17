using System;
using MultiSharp.Issues;
using Xunit;

namespace MultiSharp.Tests.Issues
{
    public class IssueStoreTests
    {
        private static MultiSharpIssue MakeIssue(string file, string ruleId = "MS001", IssueSeverity sev = IssueSeverity.Warning)
            => new() { FilePath = file, RuleId = ruleId, Message = "test", Severity = sev };

        [Fact]
        public void Store_VideParDefaut()
        {
            var store = new IssueStore();
            Assert.Equal(0, store.Count);
            Assert.Empty(store.Issues);
        }

        [Fact]
        public void SetIssuesForFile_AjouteLesProblemes()
        {
            var store = new IssueStore();
            store.SetIssuesForFile("foo.cs", new[] { MakeIssue("foo.cs"), MakeIssue("foo.cs") });

            Assert.Equal(2, store.Count);
        }

        [Fact]
        public void SetIssuesForFile_RemplaceLesProblemesDuFichier()
        {
            var store = new IssueStore();
            store.SetIssuesForFile("foo.cs", new[] { MakeIssue("foo.cs"), MakeIssue("foo.cs") });
            store.SetIssuesForFile("foo.cs", new[] { MakeIssue("foo.cs") });

            Assert.Equal(1, store.Count);
        }

        [Fact]
        public void ClearIssuesForFile_SupprimeSeulementCeFichier()
        {
            var store = new IssueStore();
            store.SetIssuesForFile("foo.cs", new[] { MakeIssue("foo.cs") });
            store.SetIssuesForFile("bar.cs", new[] { MakeIssue("bar.cs") });
            store.ClearIssuesForFile("foo.cs");

            Assert.Equal(1, store.Count);
            Assert.Equal("bar.cs", store.Issues[0].FilePath);
        }

        [Fact]
        public void Clear_SupprimeTouxLesProblemes()
        {
            var store = new IssueStore();
            store.SetIssuesForFile("foo.cs", new[] { MakeIssue("foo.cs") });
            store.Clear();

            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void IssuesChanged_EstDeclenche_QuandLaListeChange()
        {
            var store = new IssueStore();
            var fired = false;
            store.IssuesChanged += (_, _) => fired = true;

            store.SetIssuesForFile("foo.cs", new[] { MakeIssue("foo.cs") });

            Assert.True(fired);
        }

        [Fact]
        public void SetIssuesForFile_MultiplesFichiers_AggregeCorrectement()
        {
            var store = new IssueStore();
            store.SetIssuesForFile("a.cs", new[] { MakeIssue("a.cs"), MakeIssue("a.cs") });
            store.SetIssuesForFile("b.cs", new[] { MakeIssue("b.cs") });

            Assert.Equal(3, store.Count);
        }
    }
}
