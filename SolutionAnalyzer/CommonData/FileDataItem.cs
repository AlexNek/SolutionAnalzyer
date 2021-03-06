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
    /// Class FileDataItem.
    /// Contains information about code file and first class members
    /// Implements the <see cref="Microsoft.VisualStudio.PlatformUI.ObservableObject" />
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.PlatformUI.ObservableObject" />
    public class FileDataItem : ObservableObject
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
        /// The class count
        /// </summary>
        private int _classCount;

        /// <summary>
        /// The class members list
        /// </summary>
        private ObservableCollection<ClassMemberDataItem> _classMembers = new ObservableCollection<ClassMemberDataItem>();

        /// <summary>
        /// The item image
        /// </summary>
        private ImageSource _image;

        /// <summary>
        /// The item language
        /// </summary>
        private ELanguage _language;

        /// <summary>
        /// The line count
        /// </summary>
        private int _lineCount;

        /// <summary>
        /// The member count
        /// </summary>
        private int? _memberCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDataItem"/> class.
        /// </summary>
        public FileDataItem()
        {
            Uri uriEmpty = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/empty.png", UriKind.RelativeOrAbsolute);
            Uri uriCSharp = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/csharpfile.png", UriKind.RelativeOrAbsolute);

            _emptyImage = new BitmapImage(uriEmpty);
            _cSharpImage = new BitmapImage(uriCSharp);

            Image = _emptyImage;
        }

        /// <summary>
        /// Sorts the class members.
        /// </summary>
        public void SortClassMembers()
        {
            ClassMembers = new ObservableCollection<ClassMemberDataItem>(ClassMembers.OrderByDescending(item => item.LineCount));
        }

        /// <summary>
        /// Gets or sets the class count.
        /// </summary>
        /// <value>The class count.</value>
        public int ClassCount
        {
            get
            {
                return _classCount;
            }
            set
            {
                _classCount = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the document identifier.
        /// </summary>
        /// <value>The document identifier.</value>
        public DocumentId DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the class members.
        /// </summary>
        /// <value>The class members.</value>
        public ObservableCollection<ClassMemberDataItem> ClassMembers
        {
            get
            {
                return _classMembers;
            }
            set
            {
                _classMembers = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        public ImageSource Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath { get; set; }


        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the language.
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
                    Image = _cSharpImage;
                }
                else
                {
                    Image = _emptyImage;
                }
            }
        }

        /// <summary>
        /// Gets or sets the line count.
        /// </summary>
        /// <value>The line count.</value>
        public int LineCount
        {
            get
            {
                return _lineCount;
            }
            set
            {
                _lineCount = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the member count.
        /// </summary>
        /// <value>The member count.</value>
        public int? MemberCount
        {
            get
            {
                return _memberCount;
            }
            set
            {
                _memberCount = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public ProjectDataItem Parent { get; set; }
    }
}
