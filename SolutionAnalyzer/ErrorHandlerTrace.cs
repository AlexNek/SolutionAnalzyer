using System;
using System.Diagnostics;

using SolutionAnalyzer.Common;

namespace SolutionAnalyzer
{
    /// <summary>
    /// Class ErrorHandlerTrace.
    /// Implements error output as simple trace
    /// Implements the <see cref="SolutionAnalyzer.Common.IErrorHandler" />
    /// </summary>
    /// <seealso cref="SolutionAnalyzer.Common.IErrorHandler" />
    internal class ErrorHandlerTrace : IErrorHandler
    {
        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="text">The text.</param>
        public void HandleError(Exception ex, string text = "")
        {
            Trace.WriteLine($"*** {text} *** {ex.Message}");
            if (ex.InnerException != null)
            {
                Trace.WriteLine("Inner: " + ex.InnerException.Message);
            }
        }
    }
}
