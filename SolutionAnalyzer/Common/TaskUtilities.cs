using System;
using System.Threading.Tasks;

namespace SolutionAnalyzer.Common
{
    /// <summary>
    /// Class TaskUtilities.
    /// </summary>
    public static class TaskUtilities
    {
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        /// <summary>
        /// fire and forget safe as an asynchronous operation.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="handler">The handler.</param>
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler handler = null)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                handler?.HandleError(ex,"Async Command");
            }
        }
    }
}