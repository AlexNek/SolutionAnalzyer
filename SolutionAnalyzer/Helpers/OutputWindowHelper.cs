using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SolutionAnalyzer.Helpers
{
    /// <summary>
    /// A helper class for writing messages to a Package output window pane.
    /// </summary>
    internal static class OutputWindowHelper
    {
        /// <summary>
        /// The code maid output window pane
        /// </summary>
        private static IVsOutputWindowPane _packageOutputWindowPane;

       
        /// <summary>
        /// Writes the specified exception line to the output pane.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The exception that was handled.</param>
        internal static void ExceptionWriteLine(string message, Exception ex)
        {
            var exceptionMessage = $"{message}: {ex}";

            WriteLine("Handled Exception", exceptionMessage);
        }

        /// <summary>
        /// Writes the specified warning line to the  output pane.
        /// </summary>
        /// <param name="message">The message.</param>
        internal static void WarningWriteLine(string message)
        {
            WriteLine("Warning", message);
        }

        /// <summary>
        /// Attempts to create and retrieve the  output window pane.
        /// </summary>
        /// <returns>The CodeMaid output window pane, otherwise null.</returns>
        private static IVsOutputWindowPane GetPackageOutputWindowPane()
        {
            if (!(Package.GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow))
            {
                return null;
            }

            Guid outputPaneGuid = new Guid(SolutionAnalyzerPackage.GuidPackageOutputPane.ToByteArray());

            outputWindow.CreatePane(ref outputPaneGuid, "Localization Helper", 1, 1);
            outputWindow.GetPane(ref outputPaneGuid, out IVsOutputWindowPane windowPane);
            //int activate = windowPane.Activate();
            return windowPane;
            //return null;
        }

        /// <summary>
        /// Writes the specified line to the output pane.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        private static void WriteLine(string category, string message)
        {
            var outputWindowPane = PackageOutputWindowPane;
            if (outputWindowPane != null)
            {
                string outputMessage = $"[LH {category} {DateTime.Now.ToString("HH:mm:ss")}] {message}{Environment.NewLine}";
                outputWindowPane.OutputStringThreadSafe(outputMessage);
            }
        }

        /// <summary>
        /// Gets the package output window pane.
        /// </summary>
        /// <value>The code maid output window pane.</value>
        private static IVsOutputWindowPane PackageOutputWindowPane
        {
            get
            {
                return _packageOutputWindowPane ?? (_packageOutputWindowPane = GetPackageOutputWindowPane());
            }
        }
    }
}
