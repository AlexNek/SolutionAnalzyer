using System;

namespace SolutionAnalyzer.Common
{
    public interface IErrorHandler
    {
        void HandleError(Exception ex, string text);
    }
}
