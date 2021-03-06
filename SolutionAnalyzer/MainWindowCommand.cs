using System;
using System.ComponentModel.Design;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace SolutionAnalyzer
{
    /// <summary>
    /// Default Command handler
    /// </summary>
    internal sealed class MainWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7b55ef16-4bcc-40d0-a6c2-5d34389d752f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowCommand" /> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        /// <exception cref="ArgumentNullException">
        /// package
        /// or
        /// commandService
        /// </exception>
        private MainWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in MainWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MainWindowCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            _package.JoinableTaskFactory.RunAsync(
                async delegate
                    {
                        try
                        {
                            ToolWindowPane window = await _package.ShowToolWindowAsync(typeof(MainWindowPane), 0, true, _package.DisposalToken);
                            if (null == window || null == window.Frame)
                            {
                                //throw new NotSupportedException("Cannot create tool window");
                                VsShellUtilities.ShowMessageBox(
                                    _package,
                                    "Cannot create tool window",
                                    "Error",
                                    OLEMSGICON.OLEMSGICON_WARNING,
                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            VsShellUtilities.ShowMessageBox(
                                _package,
                                "Cannot create tool window - " + ex.Message,
                                "Error",
                                OLEMSGICON.OLEMSGICON_WARNING,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                    });
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        /// <value>The instance.</value>
        public static MainWindowCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        /// <value>The service provider.</value>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return _package;
            }
        }
    }
}
