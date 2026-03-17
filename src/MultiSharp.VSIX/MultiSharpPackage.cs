using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using MultiSharp.Options;
using Task = System.Threading.Tasks.Task;

namespace MultiSharp
{
    /// <summary>
    /// Point d'entrée de l'extension MultiSharp.
    /// AsyncPackage permet un chargement asynchrone pour ne pas bloquer le démarrage de VS.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(
        typeof(MultiSharpOptions),
        categoryName: "MultiSharp",
        pageName: "Général",
        categoryResourceID: 0,
        pageNameResourceID: 0,
        supportsAutomation: true)]
    public sealed class MultiSharpPackage : AsyncPackage
    {
        public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        /// <summary>
        /// Accès aux options de l'extension depuis n'importe où dans le package.
        /// </summary>
        public MultiSharpOptions Options =>
            (MultiSharpOptions)GetDialogPage(typeof(MultiSharpOptions));

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Retour sur le thread UI pour l'initialisation des services VS
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}
