using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using SolutionAnalyzer.Helpers;
using SolutionAnalyzer.Views;
using Task = System.Threading.Tasks.Task;

namespace SolutionAnalyzer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// Implements the <see cref="Microsoft.VisualStudio.Shell.AsyncPackage" />
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.Shell.AsyncPackage" />
    /// <remarks><para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para></remarks>
    [ProvideBindingPath] //important to load referenced assembly
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 100)] // Info on this package for Help/About
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(MainWindowPane))]
    public sealed class SolutionAnalyzerPackage : AsyncPackage
    {
        /// <summary>
        /// SolutionAnalyzerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f11df89d-ef7d-42b8-8130-551e10819c91";

        /// <summary>
        /// The unique identifier package output pane string
        /// </summary>
        public const string GuidPackageOutputPaneString = "35C35A14-6E62-43D2-A4E0-482D3233221F";

        /// <summary>
        /// The unique identifier package output pane
        /// </summary>
        public static Guid GuidPackageOutputPane = new Guid(GuidPackageOutputPaneString);

        /// <summary>
        /// The cookie
        /// </summary>
        private uint _cookie;

        /// <summary>
        /// The solution container
        /// </summary>
        private VsSolutionContainer _solutionContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionAnalyzerPackage"/> class.
        /// </summary>
        public SolutionAnalyzerPackage()
        {
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            }
        }

        /// <summary>
        /// Returns the asynchronous tool window factory interface for the tool window identified by
        /// <paramref name="toolWindowType" />, if asynchronous creation is supported for the tool window.
        /// If asynchronous creation is not supported, null is returned.
        /// </summary>
        /// <param name="toolWindowType">Type of the window to be created</param>
        /// <returns>The asynchronous factory interface, or null if not supported</returns>
        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            if (toolWindowType == typeof(MainWindowPane).GUID)
            {
                return this;
            }

            return base.GetAsyncToolWindowFactory(toolWindowType);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            if (!ThreadHelper.CheckAccess())
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }
            //prevent zombie state, when VS not initialized. Possible use dte from OnShellPropertyChange?
            //IVsShell shellService = GetService(typeof(SVsShell)) as IVsShell;
            //if (shellService != null)
            //{
            //    ErrorHandler.ThrowOnFailure(shellService.AdviseShellPropertyChanges(this, out _cookie));
            //}

            //await CustomCommands.InitializeAsync(this);
            //await Commands.CommandMain.InitializeAsync(this);
            await MainWindowCommand.InitializeAsync(this);

            _solutionContainer = new VsSolutionContainer(this);
        }

        /// <summary>
        /// Initialize tool window as an asynchronous operation.
        /// work together with GetAsyncToolWindowFactory
        /// </summary>
        /// <param name="toolWindowType">Type of the window to be created</param>
        /// <param name="id">The instance identifier for the tool window</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation</param>
        /// <returns>A task representing the initialization work.  The result of the task is a context
        /// object that will be passed to the passed to the matching <see cref="T:Microsoft.VisualStudio.Shell.ToolWindowPane" />
        /// constructor. If no object needs to be passed to the pane constructor,
        /// <see cref="F:Microsoft.VisualStudio.Shell.Package.ToolWindowCreationContext.Unspecified" /> can be returned.  In this case,
        /// the pane's default constructor will be invoked.</returns>
        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            IVsUIShell shellService = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
            if (shellService != null)
            {
                shellService.SetWaitCursor();
            }

            MainWindowControl customControl = new MainWindowControl();

            return customControl; // this is passed to the tool window constructor
        }

        /// <summary>
        /// Handles the <see cref="E:DispatcherUnhandledException" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DispatcherUnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            OutputWindowHelper.ExceptionWriteLine("DispatcherUnhandledException raised in Visual Studio", e.Exception);
            //e.Handled = true;
        }

        /// <summary>
        /// Gets the solution container.
        /// </summary>
        /// <value>The solution container.</value>
        internal VsSolutionContainer SolutionContainer
        {
            get
            {
                return _solutionContainer;
            }
        }
    }
}
