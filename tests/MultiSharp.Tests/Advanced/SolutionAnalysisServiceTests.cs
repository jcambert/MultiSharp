using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiSharp.Advanced;
using MultiSharp.Analyzers;
using MultiSharp.Formatting;
using MultiSharp.Tests.Helpers;
using Xunit;

namespace MultiSharp.Tests.Advanced
{
    public class SolutionAnalysisServiceTests
    {
        [Fact]
        public async Task AnalyzeSolution_TrouveDiagnostics()
        {
            // Un champ privé sans underscore → NamingConventionAnalyzer doit le détecter
            var code = @"class C { private int count; }";
            var solution = SolutionTestHelper.CreateSolution(code);

            var service = new SolutionAnalysisService();
            var analyzers = new List<Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer>
            {
                new NamingConventionAnalyzer()
            };

            var diagnostics = await service.AnalyzeSolutionAsync(solution, analyzers);
            Assert.Contains(diagnostics, d => d.Id == DiagnosticIds.NamingPrivateField);
        }

        [Fact]
        public async Task AnalyzeSolution_ReportesProgressionEvenements()
        {
            var code = @"class C { }";
            var solution = SolutionTestHelper.CreateSolution(code);

            var service = new SolutionAnalysisService();
            int progressEvents = 0;
            bool completedFired = false;

            service.ProgressChanged += (_, _) => progressEvents++;
            service.Completed += (_, _) => completedFired = true;

            await service.AnalyzeSolutionAsync(solution,
                new List<Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer>
                { new NamingConventionAnalyzer() });

            Assert.True(progressEvents > 0);
            Assert.True(completedFired);
        }

        [Fact]
        public void GetChangedProjectIds_DetecteProjetNouveau()
        {
            var code = @"class C { }";
            var oldSolution = SolutionTestHelper.CreateSolution(code);

            // Simuler un ajout de projet dans la nouvelle solution
            var newProjectId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
            var newSolution = oldSolution
                .AddProject(newProjectId, "NewProject", "NewProject",
                    Microsoft.CodeAnalysis.LanguageNames.CSharp);

            var changed = SolutionAnalysisService.GetChangedProjectIds(oldSolution, newSolution);
            Assert.Contains(newProjectId, changed);
        }
    }
}
