using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.PlatformUI;

namespace SolutionAnalyzer.CommonData
{
    /// <summary>
    /// Class ClassMemberDataItem.
    /// Contains class member description for constructors, functions and properties
    /// Implements the <see cref="Microsoft.VisualStudio.PlatformUI.ObservableObject" />
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.PlatformUI.ObservableObject" />
    public class ClassMemberDataItem : ObservableObject
    {
        /// <summary>
        /// Enum ECodeType
        /// </summary>
        public enum ECodeType
        {
            /// <summary>
            /// The none
            /// </summary>
            None = 0,

            /// <summary>
            /// The method
            /// </summary>
            Method,

            /// <summary>
            /// The constructor
            /// </summary>
            Constructor,

            /// <summary>
            /// The property
            /// </summary>
            Property
        }

        /// <summary>
        /// The empty image
        /// </summary>
        private static ImageSource _emptyImage;

        /// <summary>
        /// The method1 image
        /// </summary>
        private static ImageSource _method1Image;

        /// <summary>
        /// The method2 image
        /// </summary>
        private static ImageSource _method2Image;

        /// <summary>
        /// The property image
        /// </summary>
        private static ImageSource _propertyImage;

        /// <summary>
        /// The code type
        /// </summary>
        private ECodeType _codeType;

        /// <summary>
        /// The image code type
        /// </summary>
        private ImageSource _imageCodeType;

        /// <summary>
        /// The selected
        /// </summary>
        private bool? _selected = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassMemberDataItem"/> class.
        /// </summary>
        public ClassMemberDataItem()
        {
            if (_emptyImage == null)
            {
                Uri uriEmpty = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/empty.png", UriKind.RelativeOrAbsolute);
                _emptyImage = new BitmapImage(uriEmpty);
            }

            if (_method1Image == null)
            {
                Uri uriMethod = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/method1.png", UriKind.RelativeOrAbsolute);
                _method1Image = new BitmapImage(uriMethod);
            }

            if (_method2Image == null)
            {
                Uri uriMethod = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/method2.png", UriKind.RelativeOrAbsolute);
                _method2Image = new BitmapImage(uriMethod);
            }

            if (_propertyImage == null)
            {
                Uri uriProperty = new Uri(@"pack://application:,,,/SolutionAnalyzer;component/Images/property.png", UriKind.RelativeOrAbsolute);
                _propertyImage = new BitmapImage(uriProperty);
            }

            ImageCodeType = _emptyImage;
        }

        /// <summary>
        /// Gets or sets the type of the code.
        /// </summary>
        /// <value>The type of the code.</value>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ECodeType CodeType
        {
            get
            {
                return _codeType;
            }
            set
            {
                if (_codeType != value)
                {
                    _codeType = value;
                    switch (_codeType)
                    {
                        case ECodeType.Method:
                            ImageCodeType = _method2Image;
                            break;
                        case ECodeType.Constructor:
                            ImageCodeType = _method1Image;
                            break;
                        case ECodeType.Property:
                            ImageCodeType = _propertyImage;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the end line position.
        /// </summary>
        /// <value>The end line position.</value>
        public LinePosition EndLinePosition { get; set; }

        /// <summary>
        /// Gets or sets the type of the image code.
        /// </summary>
        /// <value>The type of the image code.</value>
        public ImageSource ImageCodeType
        {
            get
            {
                return _imageCodeType;
            }
            set
            {
                _imageCodeType = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the line count.
        /// </summary>
        /// <value>The line count.</value>
        public int LineCount { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public FileDataItem? Parent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ClassMemberDataItem"/> is selected.
        /// </summary>
        /// <value><c>null</c> if [selected] contains no value, <c>true</c> if [selected]; otherwise, <c>false</c>.</value>
        public bool? Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the start line.
        /// </summary>
        /// <value>The start line.</value>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets the start line position.
        /// </summary>
        /// <value>The start line position.</value>
        public LinePosition StartLinePosition { get; set; }
    }
}
