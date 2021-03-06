using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

using SolutionAnalyzer.Views;

namespace SolutionAnalyzer
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// Implements the <see cref="Microsoft.VisualStudio.Shell.ToolWindowPane" />
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.Shell.ToolWindowPane" />
    /// <remarks>In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para></remarks>
    [Guid("93be252a-29c4-45fb-8095-785751b28173")]
    public class MainWindowPane : ToolWindowPane
    {
        /// <summary>
        /// The custom control
        /// </summary>
        private readonly MainWindowControl _customControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowPane" /> class.
        /// </summary>
        public MainWindowPane()
            : base(null)
        {
            Caption = "Solution analyzer";
            //_customControl = new MainWindowControl();

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new QuickLoadingControl();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowPane"/> class.
        /// </summary>
        /// <param name="customControl">The custom control.</param>
        public MainWindowPane(MainWindowControl customControl)
            : this()
        {
            _customControl = customControl;
            Content = _customControl;
        }

        /// <summary>
        /// This method can be overriden by the derived class to execute
        /// any code that needs to run after the IVsWindowFrame is created.
        /// If the toolwindow has a toolbar with a combobox, it should make
        /// sure its command handler are set by the time they return from
        /// this method.
        /// This is called when someone set the Frame property.
        /// </summary>
        /// <include file="doc\ToolWindowPane.uex" path="docs/doc[@for=&quot;ToolWindowPane.OnToolWindowCreated&quot;]/*" />
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            if (_customControl != null)
            {
                SolutionAnalyzerPackage package = (SolutionAnalyzerPackage)Package;
                _customControl.Package = package;
            }

            //Package = {SharpLocalizationHelper.SharpLocalizationHelperPackage}
        }
    }
}
