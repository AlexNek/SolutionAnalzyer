// ***********************************************************************
// Author           : John Thiriet
//                    https://github.com/johnthiriet/AsyncVoid
// ***********************************************************************

using System.Threading.Tasks;
using System.Windows.Input;

namespace SolutionAnalyzer.Common
{
    public interface IAsyncCommand<T> : ICommand
    {
        bool CanExecute(T parameter);

        Task ExecuteAsync(T parameter);
    }
}
