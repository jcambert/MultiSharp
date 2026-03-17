using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using MultiSharp.Options;
using MultiSharp.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace MultiSharp
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideToolWindow(typeof(MultiSharpIssuesWindow))]
    [ProvideMenuResource("Menus.ctmenu", 1)]
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

        public MultiSharpOptions Options =>
            (MultiSharpOptions)GetDialogPage(typeof(MultiSharpOptions));

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await ShowIssuesWindowCommand.InitializeAsync(this);
        }

        /// <summary>Ouvre ou active le Tool Window MultiSharp Issues.</summary>
        public async Task ShowIssuesWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var window = await FindToolWindowAsync(
                typeof(MultiSharpIssuesWindow), id: 0, create: true, cancellationToken: default);
            (window?.Frame as Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame)
                ?.Show();
        }
    }
}
