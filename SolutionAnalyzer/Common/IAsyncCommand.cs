// ***********************************************************************
// Assembly         : SharpLocalizationHelper
// Author           : John Thiriet
//                    https://github.com/johnthiriet/AsyncVoid
// ***********************************************************************

using System.Threading.Tasks;
using System.Windows.Input;

namespace SolutionAnalyzer.Common
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }
}