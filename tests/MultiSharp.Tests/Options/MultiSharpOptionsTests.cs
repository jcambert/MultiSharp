using MultiSharp.Options;
using Xunit;

namespace MultiSharp.Tests.Options
{
    /// <summary>
    /// Tests sur MultiSharpSettings (POCO dans MultiSharp.Core, sans dépendance VS SDK).
    /// </summary>
    public class MultiSharpOptionsTests
    {
        [Fact]
        public void ValeurDefaut_IsEnabled_EstTrue()
        {
            var settings = new MultiSharpSettings();
            Assert.True(settings.IsEnabled);
        }

        [Fact]
        public void ValeurDefaut_AnalysisEnabled_EstTrue()
        {
            var settings = new MultiSharpSettings();
            Assert.True(settings.AnalysisEnabled);
        }

        [Fact]
        public void ValeurDefaut_SeveriteParDefaut_EstWarning()
        {
            var settings = new MultiSharpSettings();
            Assert.Equal(DiagnosticSeverityOption.Warning, settings.DefaultSeverity);
        }

        [Fact]
        public void ValeurDefaut_MaxMethodLines_EstCinquante()
        {
            var settings = new MultiSharpSettings();
            Assert.Equal(50, settings.MaxMethodLines);
        }

        [Fact]
        public void ValeurDefaut_MaxParameters_EstCinq()
        {
            var settings = new MultiSharpSettings();
            Assert.Equal(5, settings.MaxParameters);
        }

        [Fact]
        public void Modification_Settings_EstPersistee()
        {
            var settings = new MultiSharpSettings();
            settings.IsEnabled = false;
            settings.MaxMethodLines = 100;

            Assert.False(settings.IsEnabled);
            Assert.Equal(100, settings.MaxMethodLines);
        }

        [Fact]
        public void DefaultFactory_RetourneInstanceAvecValeursParDefaut()
        {
            var settings = MultiSharpSettings.Default;

            Assert.True(settings.IsEnabled);
            Assert.True(settings.AnalysisEnabled);
            Assert.True(settings.RefactoringEnabled);
            Assert.Equal(DiagnosticSeverityOption.Warning, settings.DefaultSeverity);
        }
    }
}
