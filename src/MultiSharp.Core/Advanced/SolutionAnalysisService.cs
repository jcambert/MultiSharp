using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MultiSharp.Advanced
{
    /// <summary>
    /// US-601 — Analyse de l'ensemble de la solution en tâche de fond.
    /// Collecte tous les diagnostics pour tous les documents, avec progression et annulation.
    /// </summary>
    public sealed class SolutionAnalysisService
    {
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<SolutionAnalysisCompletedArgs>? Completed;

        /// <summary>
        /// Lance l'analyse de la solution avec les analyseurs fournis.
        /// La progression est reportée via <see cref="ProgressChanged"/>.
        /// </summary>
        public async Task<IReadOnlyList<Diagnostic>> AnalyzeSolutionAsync(
            Solution solution,
            IReadOnlyList<DiagnosticAnalyzer> analyzers,
            CancellationToken ct = default)
        {
            var allDiagnostics = new List<Diagnostic>();
            var projects = solution.Projects.ToList();
            int total = projects.Count;
            int done = 0;

            foreach (var project in projects)
            {
                ct.ThrowIfCancellationRequested();

                var compilation = await project.GetCompilationAsync(ct);
                if (compilation == null) continue;

                var withAnalyzers = compilation.WithAnalyzers(
                    System.Collections.Immutable.ImmutableArray.CreateRange(analyzers),
                    options: null,
                    cancellationToken: ct);

                var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(ct);
                allDiagnostics.AddRange(diagnostics);

                done++;
                ProgressChanged?.Invoke(this, new ProgressEventArgs(done, total, project.Name));
            }

            Completed?.Invoke(this, new SolutionAnalysisCompletedArgs(allDiagnostics));
            return allDiagnostics;
        }

        /// <summary>
        /// Analyse incrémentale : retourne les IDs des projets nouveaux ou modifiés.
        /// Utilise le VersionStamp Roslyn pour détecter les changements.
        /// </summary>
        public static IReadOnlyList<ProjectId> GetChangedProjectIds(
            Solution oldSolution,
            Solution newSolution)
        {
            return newSolution.Projects
                .Where(p =>
                {
                    var oldProject = oldSolution.GetProject(p.Id);
                    if (oldProject == null) return true;
                    // Si la version du projet a changé, il faut re-analyser
                    return p.Version != oldProject.Version;
                })
                .Select(p => p.Id)
                .ToList();
        }
    }

    public sealed class ProgressEventArgs : EventArgs
    {
        public int Current { get; }
        public int Total { get; }
        public string ProjectName { get; }
        public int PercentComplete => Total == 0 ? 100 : (Current * 100) / Total;

        public ProgressEventArgs(int current, int total, string projectName)
        {
            Current = current;
            Total = total;
            ProjectName = projectName;
        }
    }

    public sealed class SolutionAnalysisCompletedArgs : EventArgs
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public SolutionAnalysisCompletedArgs(IReadOnlyList<Diagnostic> diagnostics)
            => Diagnostics = diagnostics;
    }
}
