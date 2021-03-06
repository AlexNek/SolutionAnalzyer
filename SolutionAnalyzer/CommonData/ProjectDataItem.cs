using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.PlatformUI;

namespace SolutionAnalyzer.CommonData
{
    /// <summary>
    /// Class ProjectDataItem.
    /// Contains information about projects in solution
    /// Implements the <see cref="Microsoft.VisualStudio.PlatformUI.ObservableObject" />
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.PlatformUI.ObservableObject" />
    public class ProjectDataItem : ObservableObject
    {
   
        /// <summary>
        /// The c sharp image
        /// </summary>
        private static ImageSource _cSharpImage;

        /// <summary>
        /// The empty image
        /// </summary>
        private static ImageSource _emptyImage;

        /// <summary>
        /// The code source files
        /// </summary>
        private ObservableCollection<FileDataItem> _codeSourceFiles = new ObservableCollection<FileDataItem>();

        /// <summary>
        /// The files
        /// </summary>
        private int? _files;

        /// <summary>
        /// The image language
        /// </summary>
        private ImageSource? _imageLang;

        /// <summary>
        /// The language
        /// </summary>
        private ELanguage _language;

        /// <summary>
        /// The summary lines
        /// </summary>
        private int? _summaryLines;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectDataItem"/> class.
        /// </summary>
        public ProjectDataItem()
        {
            Uri uriEmpty = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/empty.png", UriKind.RelativeOrAbsolute);
            Uri uriCSharp = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/csharpproj.png", UriKind.RelativeOrAbsolute);

            _emptyImage = new BitmapImage(uriEmpty);
            _cSharpImage = new BitmapImage(uriCSharp);

            ImageLang = _emptyImage;
        }

        /// <summary>
        /// Sorts the code source files.
        /// </summary>
        public void SortCodeSourceFiles()
        {
            CodeSourceFiles = new ObservableCollection<FileDataItem>(CodeSourceFiles.OrderByDescending(item => item.LineCount));
        }

        /// <summary>
        /// Gets the code source files.
        /// </summary>
        /// <value>The code source files.</value>
        public ObservableCollection<FileDataItem> CodeSourceFiles
        {
            get
            {
                return _codeSourceFiles;
            }
            private set
            {
                _codeSourceFiles = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the files count.
        /// </summary>
        /// <value>The files.</value>
        public int? Files
        {
            get
            {
                return _files;
            }
            set
            {
                _files = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the image for language.
        /// </summary>
        /// <value>The image language.</value>
        public ImageSource ImageLang
        {
            get
            {
                return _imageLang;
            }
            set
            {
                _imageLang = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the language of the project.
        /// </summary>
        /// <value>The language.</value>
        public ELanguage Language
        {
            get
            {
                return _language;
            }
            set
            {
                _language = value;
                if (Language == ELanguage.CSharp)
                {
                    ImageLang = _cSharpImage;
                }
                else
                {
                    ImageLang = _emptyImage;
                }
            }
        }

        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        /// <value>The project identifier.</value>
        public ProjectId ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the summary lines count.
        /// </summary>
        /// <value>The summary lines.</value>
        public int? SummaryLines
        {
            get
            {
                return _summaryLines;
            }
            set
            {
                _summaryLines = value;
                NotifyPropertyChanged();
            }
        }
    }
}
