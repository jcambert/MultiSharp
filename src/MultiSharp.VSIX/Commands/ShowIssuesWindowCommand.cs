using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MultiSharp
{
    /// <summary>
    /// Commande VS "View > MultiSharp Issues" pour ouvrir le Tool Window.
    /// </summary>
    internal sealed class ShowIssuesWindowCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new("c3d4e5f6-a7b8-9012-cdef-123456789012");

        private readonly AsyncPackage _package;

        private ShowIssuesWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;
            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
                _ = new ShowIssuesWindowCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await ((MultiSharpPackage)_package).ShowIssuesWindowAsync();
            });
        }
    }
}
