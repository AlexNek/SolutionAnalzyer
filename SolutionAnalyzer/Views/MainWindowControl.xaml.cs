using System.Windows.Controls;

using SolutionAnalyzer.ViewModels;

namespace SolutionAnalyzer.Views
{
    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// Implements the <see cref="System.Windows.Controls.UserControl" />
    /// Implements the <see cref="System.Windows.Markup.IComponentConnector" />
    /// </summary>
    /// <seealso cref="System.Windows.Controls.UserControl" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class MainWindowControl : UserControl
    {
        /// <summary>
        /// The view model
        /// </summary>
        private readonly MainWindowControlVm _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowControl" /> class.
        /// </summary>
        public MainWindowControl()
        {
            _viewModel = new MainWindowControlVm();
            DataContext = _viewModel;
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        /// <value>The package.</value>
        public SolutionAnalyzerPackage Package
        {
            get
            {
                return _viewModel.Package;
            }
            set
            {
                _viewModel.Package = value;
            }
        }
    }
}
