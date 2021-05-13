#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

using SolutionAnalyzer.CodeVisitors;
using SolutionAnalyzer.Common;
using SolutionAnalyzer.CommonData;
using SolutionAnalyzer.Helpers;

using Task = System.Threading.Tasks.Task;

namespace SolutionAnalyzer.ViewModels
{
    /// <summary>
    /// Class MainWindowControlVm.
    /// View-Model for the main window panel
    /// Implements the <see cref="GalaSoft.MvvmLight.ViewModelBase" />
    /// </summary>
    /// <seealso cref="GalaSoft.MvvmLight.ViewModelBase" />
    internal class MainWindowControlVm : ViewModelBase
    {
        /// <summary>
        /// The error handler
        /// </summary>
        private readonly IErrorHandler _errorHandler;

        /// <summary>
        /// The class member selection text
        /// </summary>
        private string _classMemberSelectionText;

        /// <summary>
        /// The current selection text
        /// </summary>
        private string _currentSelectionText;

        /// <summary>
        /// The debug text
        /// </summary>
        private string _debugText;

        /// <summary>
        /// The ignore update selection
        /// </summary>
        private bool _ignoreUpdateSelection;

        /// <summary>
        /// The package
        /// </summary>
        private SolutionAnalyzerPackage _package;

        /// <summary>
        /// The selected class member
        /// </summary>
        private ClassMemberDataItem _selectedClassMember;

        /// <summary>
        /// The selected file
        /// </summary>
        private FileDataItem _selectedFile;

        /// <summary>
        /// The selected project
        /// </summary>
        private ProjectDataItem _selectedProject;

        /// <summary>
        /// The solution container
        /// </summary>
        private VsSolutionContainer? _solutionContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowControlVm"/> class.
        /// </summary>
        public MainWindowControlVm()
        {
            _errorHandler = new ErrorHandlerTrace();
            CommandNewClassPart = new AsyncCommand(ExecNewClassPartAsync, CanRefresh, _errorHandler);
            CommandRefresh = new AsyncCommand(ExecRefresh, CanRefresh, _errorHandler);
            CommandRefreshSelected = new AsyncCommand(ExecRefreshSelectedAsync, CanRefresh, _errorHandler);
            CommandMoveToNewClassPart = new AsyncCommand(ExecMoveToNewClassPartAsync, CanRefresh, _errorHandler);
            CommandReloadProjects = new AsyncCommand(ExecCommandReloadProjectsAsync, CanRefresh, _errorHandler);
            CommandScanProjects = new AsyncCommand(ExecCommandScanProjectsAsync, CanRefresh, _errorHandler);

            CommandSetFlag = new RelayCommand(ExecSetFlag);
            CommandResetFlag = new RelayCommand(ExecResetFlag);

            CommandExport = new RelayCommand(ExecCommandExport, CanRefresh);

            ClassMemberSelectionChangedCommand = new RelayCommand<IList<object>>(ExecClassMemberSelectionChanged);

            FilesRowDblClickCommand = new AsyncCommand<FileDataItem>(ExecFilesRowDblClickAsync, CanRunFilesDblClick, _errorHandler);
            ClassMemberRowDblClickCommand = new AsyncCommand<ClassMemberDataItem>(
                ExecClassMemberRowDblClickAsync,
                CanRunClassMemberDblClick,
                _errorHandler);
        }

        /// <summary>
        /// Determines whether the button refresh could be pressed
        /// </summary>
        /// <returns><c>true</c> if the button can refresh; otherwise, <c>false</c>.</returns>
        private bool CanRefresh()
        {
            //return true;
            return _solutionContainer != null && _solutionContainer.IsSolutionOpened;
        }

        /// <summary>
        /// Determines whether allowed command execution for the class members grid row double click.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns><c>true</c> if this command can run ; otherwise, <c>false</c>.</returns>
        private bool CanRunClassMemberDblClick(ClassMemberDataItem arg)
        {
            return true;
        }

        /// <summary>
        /// Determines whether allowed command execution for the files grid row double click.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns><c>true</c> if this command can run; otherwise, <c>false</c>.</returns>
        private bool CanRunFilesDblClick(FileDataItem arg)
        {
            return true;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the ClassMemberDataItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ClassMemberDataItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClassMemberDataItem.Selected))
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(
                        () => { UpdateClassMemberSelectionText(sender as ClassMemberDataItem); }));
            }
        }

        private void DoActionSolutionOpened()
        {
            RefreshCommandState();
            LoadProjects();
            DebugText = null;
        }

        private void DoActionSolutionClosed()
        {
            RefreshCommandState();
            SelectedProject = null;
            CodeSourceProjects.Clear();
            RaisePropertyChanged(nameof(CodeSourceFiles));
            RaisePropertyChanged(nameof(ClassMembers));

            //references from SelectedProject - no sense to clear
            //CodeSourceFiles?.Clear();
            //ClassMembers?.Clear();
            DebugText = null;
        }

        /// <summary>
        /// Execute command class member row double click as an asynchronous operation.
        /// Open document and select area with class member
        /// </summary>
        /// <param name="arg">The argument.</param>
        private async Task ExecClassMemberRowDblClickAsync(ClassMemberDataItem arg)
        {
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            VisualStudioWorkspace workspace = await VisualStudioHelper.GetVisualStudioWorkspaceAsync(_package);

            if (arg.Parent != null)
            {
                workspace.OpenDocument(arg.Parent.DocumentId);
                VisualStudioHelper.OpenDocumentSelectTextBlock(arg.Parent.FullPath, arg.StartLinePosition, arg.EndLinePosition);
            }
        }

        /// <summary>
        /// Executes command - grid selection changed.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private void ExecClassMemberSelectionChanged(IList<object> args)
        {
            SelectedItems.Clear();
            foreach (ClassMemberDataItem item in args)
            {
                SelectedItems.Add(item);
                //Trace.WriteLine("Selection:" + item.Name);
            }
        }

        private void ExecCommandExport()
        {
            try
            {
                string solutionFullName = String.Empty;
                if (_solutionContainer != null)
                {
                    solutionFullName = _solutionContainer.FullName;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "XML file (*.xml)|*.xml";
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(solutionFullName);
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(solutionFullName);
                saveFileDialog.AddExtension = true;
                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportHelper.ExportXml(saveFileDialog.FileName, solutionFullName, CodeSourceProjects);
                }
            }
            catch (Exception ex)
            {
                OutputWindowHelper.ExceptionWriteLine("Export", ex);
            }
        }

        private async Task ExecCommandReloadProjectsAsync()
        {
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            DoActionSolutionOpened();
        }

        private async Task ExecCommandScanProjectsAsync()
        {
            IVsStatusbar progressBar = VisualStudioHelper.InitProgress(out uint userId);
            try
            {
                for (int i = 0; i < CodeSourceProjects.Count; i++)
                {
                    ProjectDataItem projectDataItem = CodeSourceProjects[i];
                    progressBar.Progress(
                        ref userId,
                        1,
                        $"Update project {projectDataItem.Name} lines count",
                        (uint)i + 1,
                        (uint)CodeSourceProjects.Count);
                    await UpdateProjectAsync(projectDataItem);
                }

                if (CodeSourceProjects.Count > 0)
                {
                    List<ProjectDataItem> copyOfProjects = new List<ProjectDataItem>(CodeSourceProjects);
                    //sort descended order
                    copyOfProjects.Sort(
                        (x, y) =>
                            {
                                int xSummaryLines = x.SummaryLines.HasValue ? x.SummaryLines.Value : 0;
                                int ySummaryLines = y.SummaryLines.HasValue ? y.SummaryLines.Value : 0;
                                return -xSummaryLines.CompareTo(ySummaryLines);
                            });
                    CodeSourceProjects.Clear();
                    foreach (ProjectDataItem projectDataItem in copyOfProjects)
                    {
                        CodeSourceProjects.Add(projectDataItem);
                    }
                }
            }
            finally
            {
                VisualStudioHelper.CloseProgress(progressBar, userId);
            }
        }

        /// <summary>
        /// Execute command files row double click as an asynchronous operation.
        /// Open document related to the clicked row
        /// </summary>
        /// <param name="arg">The argument.</param>
        private async Task ExecFilesRowDblClickAsync(FileDataItem arg)
        {
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            VisualStudioWorkspace workspace = await VisualStudioHelper.GetVisualStudioWorkspaceAsync(_package);
            workspace.OpenDocument(arg.DocumentId);
            //await Task.FromResult(0);
        }

        /// <summary>
        /// Execute command move to the new class part as an asynchronous operation.
        /// </summary>
        private async Task ExecMoveToNewClassPartAsync()
        {
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            List<ClassMemberDataItem> selectedMembers = new List<ClassMemberDataItem>();
            if (SelectedFile != null)
            {
                foreach (ClassMemberDataItem item in SelectedFile.ClassMembers)
                {
                    if (item.Selected.HasValue && item.Selected.Value)
                    {
                        selectedMembers.Add(item);
                    }
                }
            }

            if (selectedMembers.Count > 0)
            {
                selectedMembers.Sort((x, y) => x.StartLine.CompareTo(y.StartLine));
            }

            CodeGeneratorHelper codeGenerator = new CodeGeneratorHelper(_package);

            await codeGenerator.GenerateNewClassPartAsync(SelectedFile, selectedMembers);
            await ExecRefreshSelectedAsync();
        }

        /// <summary>
        /// Execute command create new class part as an asynchronous operation.
        /// </summary>
        private async Task ExecNewClassPartAsync()
        {
            //await Task.Delay(10);
            CodeGeneratorHelper codeGenerator = new CodeGeneratorHelper(_package);

            await codeGenerator.GenerateNewClassPartAsync(SelectedFile);
        }

        /// <summary>
        /// Executes command - refresh files
        /// </summary>
        private async Task ExecRefresh()
        {
            if (_selectedProject != null)
            {
                await UpdateProjectAsync(_selectedProject);
            }
            else
            {
                LoadProjects();
            }
        }

        /// <summary>
        /// Execute command - refresh selected file as an asynchronous operation.
        /// </summary>
        private async Task ExecRefreshSelectedAsync()
        {
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            if (SelectedFile != null)
            {
                await UpdateFileItemAsync(SelectedFile);
                await UpdateFileAsync(SelectedFile);
            }
        }

        /// <summary>
        /// Executes command - reset selection flag.
        /// </summary>
        private void ExecResetFlag()
        {
            SetMoveIncludeFlagBySelection(false);
        }

        /// <summary>
        /// Executes command - set selections flag.
        /// </summary>
        private void ExecSetFlag()
        {
            SetMoveIncludeFlagBySelection(true);
        }

        /// <summary>
        /// Loads the projects.
        /// </summary>
        private void LoadProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            CodeSourceProjects.Clear();
            
            IList<ProjectDataItem> items = new List<ProjectDataItem>();
            _solutionContainer?.GetProjects(items, null);
            foreach (ProjectDataItem projectDataItem in items)
            {
                CodeSourceProjects.Add(projectDataItem);
            }
        }

        /// <summary>
        /// Called when package initialized.
        /// </summary>
        private void OnPackageInitialized()
        {
            _solutionContainer = _package?.SolutionContainer;
            if (_solutionContainer != null)
            {
                _solutionContainer.Notify += SolutionContainer_Notify;
                //_solutionContainer.SendNotificationIfSolutionOpened();
            }

            //load project in any case
            ThreadHelper.Generic.BeginInvoke(
                () =>
                    {
                        RefreshCommandState();
                        CommandManager.InvalidateRequerySuggested(); //???
                        LoadProjects();
                    });
        }

        /// <summary>
        /// Refreshes the state of the commands
        /// </summary>
        private void RefreshCommandState()
        {
            ((AsyncCommand)CommandNewClassPart).RaiseCanExecuteChanged();
            ((AsyncCommand)CommandRefresh).RaiseCanExecuteChanged();
            ((AsyncCommand)CommandRefreshSelected).RaiseCanExecuteChanged();
            ((AsyncCommand)CommandMoveToNewClassPart).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Sets the move flag by grid rows selection.
        /// </summary>
        /// <param name="flagState">if set to <c>true</c> [flag state].</param>
        private void SetMoveIncludeFlagBySelection(bool flagState)
        {
            foreach (ClassMemberDataItem selectedItem in SelectedItems)
            {
                selectedItem.Selected = flagState;
            }
        }

        /// <summary>
        /// Notification handler for Solution events.
        /// </summary>
        /// <param name="notifyData">The notify data.</param>
        /// <exception cref="ArgumentNullException">notifyData</exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SolutionContainer_Notify(NotifyData notifyData)
        {
            //return;
            if (notifyData == null)
            {
                throw new ArgumentNullException(nameof(notifyData));
            }

            switch (notifyData.NotifyType)
            {
                case NotifyData.ENotifyType.None:
                    break;
                case NotifyData.ENotifyType.SolutionOpened:
                    DoActionSolutionOpened();
                    break;
                case NotifyData.ENotifyType.SolutionClosed:
                    DoActionSolutionClosed();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Updates the class member selection text.
        /// </summary>
        /// <param name="classMemberDataItem">The class member data item.</param>
        private void UpdateClassMemberSelectionText(ClassMemberDataItem? classMemberDataItem)
        {
            if (_selectedClassMember != null)
            {
                FileDataItem? fileDataItem = _selectedClassMember.Parent;
                if (fileDataItem != null)
                {
                    int lineCount = 0;
                    foreach (ClassMemberDataItem item in fileDataItem.ClassMembers)
                    {
                        if (item.Selected.HasValue && item.Selected.Value)
                        {
                            lineCount += item.LineCount;
                        }
                    }

                    ClassMemberSelectionText = "Selected for move:" + lineCount + " lines";
                }
                else
                {
                    ClassMemberSelectionText = string.Empty;
                }
            }
            else
            {
                ClassMemberSelectionText = string.Empty;
            }
        }

        /// <summary>
        /// Updates the current file selection text.
        /// </summary>
        private void UpdateCurrentSelectionText()
        {
            StringBuilder sb = new StringBuilder();
            if (_selectedProject != null)
            {
                sb.Append(_selectedProject.Name + " | ");
            }

            if (_selectedFile != null)
            {
                sb.Append(_selectedFile.Name);
            }

            CurrentSelectionText = sb.ToString();
        }

        /// <summary>
        /// Update selected file as an asynchronous operation.
        /// </summary>
        private async Task UpdateFileAsync()
        {
            if (_selectedFile != null)
            {
                await UpdateFileAsync(_selectedFile);
            }
        }

        /// <summary>
        /// Update file as an asynchronous operation.
        /// </summary>
        /// <param name="selectedFile">The selected file.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">selectedFile</exception>
        private async Task<bool> UpdateFileAsync(FileDataItem selectedFile)
        {
            if (selectedFile == null)
            {
                throw new ArgumentNullException(nameof(selectedFile));
            }

            //ThreadHelper.ThrowIfNotOnUIThread();
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            IList<ClassMemberDataItem> items = await _solutionContainer?.GetClassMembersAsync(selectedFile);
            //CodeSourceProjects.Clear();
            IVsStatusbar progressBar = VisualStudioHelper.InitProgress(out uint userId);
            try
            {
                selectedFile.ClassMembers.Clear();
                for (int i = 0; i < items.Count; i++)
                {
                    ClassMemberDataItem classMemberDataItem = items[i];
                    progressBar.Progress(ref userId, 1, "Calculate lines of code for data members", (uint)i, (uint)items.Count - 1);
                    //selectedProject?.AllFiles.Add(fileDataItem);
                    classMemberDataItem.Parent = selectedFile;
                    classMemberDataItem.StartLine = classMemberDataItem.StartLinePosition.Line;
                    classMemberDataItem.LineCount = classMemberDataItem.EndLinePosition.Line - classMemberDataItem.StartLinePosition.Line;
                    classMemberDataItem.PropertyChanged += ClassMemberDataItem_PropertyChanged;

                    selectedFile?.ClassMembers.Add(classMemberDataItem);
                }
            }
            finally
            {
                VisualStudioHelper.CloseProgress(progressBar, userId);
            }

            if (selectedFile != null)
            {
                selectedFile.MemberCount = selectedFile.ClassMembers.Count;
                selectedFile.SortClassMembers();
            }

            RaisePropertyChanged(nameof(ClassMembers));

            //fake return with value
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Update file item as an asynchronous operation.
        /// </summary>
        /// <param name="fileDataItem">The file data item.</param>
        private async Task UpdateFileItemAsync(FileDataItem fileDataItem)
        {
            VisualStudioWorkspace workspace = await VisualStudioHelper.GetVisualStudioWorkspaceAsync(_package);
            Document? roslynDocument = workspace.CurrentSolution.GetDocument(fileDataItem.DocumentId);
            if (roslynDocument != null)
            {
                SyntaxTree? tree = await roslynDocument.GetSyntaxTreeAsync();
                SyntaxNode? syntaxNode = await tree?.GetRootAsync();
                if (syntaxNode != null)
                {
                    CsCodeClassFinderVisitor classFinderVisitor = new CsCodeClassFinderVisitor();

                    SyntaxNodeWalker walker = new SyntaxNodeWalker(_package, fileDataItem);
                    walker.Visit(syntaxNode, classFinderVisitor);

                    FileLinePositionSpan lineSpan = syntaxNode.GetLocation().GetMappedLineSpan();
                    //var sp = lineSpan.StartLinePosition;
                    //var ep = lineSpan.EndLinePosition;
                    fileDataItem.LineCount = lineSpan.Span.End.Line - lineSpan.Span.Start.Line + 1;
                    fileDataItem.ClassCount = classFinderVisitor.ClassCount;
                }
            }

            SelectedClassMember = null;
            UpdateClassMemberSelectionText(null);
        }

        /// <summary>
        /// Update project as an asynchronous operation.
        /// </summary>
        /// <param name="project">The selected project.</param>
        /// <exception cref="ArgumentNullException">selectedProject</exception>
        private async Task UpdateProjectAsync(ProjectDataItem project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            //ThreadHelper.ThrowIfNotOnUIThread();
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            IList<FileDataItem> items = new List<FileDataItem>();
            _solutionContainer?.GetProjectItems(project, items);

            IVsStatusbar progressBar = VisualStudioHelper.InitProgress(out uint userId);
            try
            {
                project.CodeSourceFiles.Clear();
                for (int i = 0; i < items.Count; i++)
                {
                    FileDataItem fileDataItem = items[i];
                    progressBar.Progress(ref userId, 1, $"Calculate lines of code - {i:D2}.{fileDataItem.Name}", (uint)i, (uint)items.Count - 1);
                    project?.CodeSourceFiles.Add(fileDataItem);

                    await UpdateFileItemAsync(fileDataItem);
                }
            }
            finally
            {
                VisualStudioHelper.CloseProgress(progressBar, userId);
            }

            if (project != null)
            {
                ObservableCollection<FileDataItem> codeSourceFiles = project.CodeSourceFiles;
                project.Files = codeSourceFiles.Count;

                int lines = 0;
                foreach (FileDataItem codeSourceFile in codeSourceFiles)
                {
                    lines += codeSourceFile.LineCount;
                }

                project.SummaryLines = lines;
                project?.SortCodeSourceFiles();
            }

            RaisePropertyChanged(nameof(CodeSourceFiles));
            //return await Task.FromResult(false);
        }

        /// <summary>
        /// Update selected project as an asynchronous operation.
        /// </summary>
        private async Task UpdateProjectMainAsync()
        {
            if (_selectedProject != null)
            {
                await UpdateProjectAsync(_selectedProject);
            }
        }

        /// <summary>
        /// Gets the class member row double click command.
        /// </summary>
        /// <value>The class member row double click command.</value>
        public ICommand ClassMemberRowDblClickCommand { get; }

        /// <summary>
        /// Gets the class members.
        /// </summary>
        /// <value>The class members.</value>
        public ObservableCollection<ClassMemberDataItem>? ClassMembers
        {
            get
            {
                return SelectedFile?.ClassMembers;
            }
        }

        /// <summary>
        /// Gets the selection changed command.
        /// </summary>
        /// <value>The selection changed command.</value>
        public ICommand ClassMemberSelectionChangedCommand { get; }

        /// <summary>
        /// Gets or sets the class member selection text.
        /// </summary>
        /// <value>The class member selection text.</value>
        public string ClassMemberSelectionText
        {
            get
            {
                return _classMemberSelectionText;
            }
            set
            {
                _classMemberSelectionText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the source files.
        /// </summary>
        /// <value>The code source files.</value>
        public ObservableCollection<FileDataItem>? CodeSourceFiles
        {
            get
            {
                return SelectedProject?.CodeSourceFiles;
            }
        }

        /// <summary>
        /// Gets the code source projects.
        /// </summary>
        /// <value>The code source projects.</value>
        public ObservableCollection<ProjectDataItem> CodeSourceProjects { get; } = new ObservableCollection<ProjectDataItem>();

        public ICommand CommandExport { get; }

        /// <summary>
        /// Gets the command - move to the new class part.
        /// </summary>
        /// <value>The command move to new class part.</value>
        public ICommand CommandMoveToNewClassPart { get; }

        /// <summary>
        /// Gets the command - create new class part.
        /// </summary>
        /// <value>The command new class part.</value>
        public ICommand CommandNewClassPart { get; }

        /// <summary>
        /// Gets the command  - refresh projects
        /// </summary>
        /// <value>The command refresh.</value>
        public ICommand CommandRefresh { get; }

        /// <summary>
        /// Gets the command - refresh selected file.
        /// </summary>
        /// <value>The command refresh selected.</value>
        public ICommand CommandRefreshSelected { get; }

        public ICommand CommandReloadProjects { get; }

        /// <summary>
        /// Gets the command  - reset selection flag.
        /// </summary>
        /// <value>The command reset flag.</value>
        public ICommand CommandResetFlag { get; }

        public ICommand CommandScanProjects { get; }

        /// <summary>
        /// Gets the command - set selection flag.
        /// </summary>
        /// <value>The command set flag.</value>
        public ICommand CommandSetFlag { get; }

        /// <summary>
        /// Gets or sets the current selection text.
        /// </summary>
        /// <value>The current selection text.</value>
        public string CurrentSelectionText
        {
            get
            {
                return _currentSelectionText;
            }
            set
            {
                _currentSelectionText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the debug text.
        /// </summary>
        /// <value>The debug text.</value>
        public string DebugText
        {
            get
            {
                return _debugText;
            }
            set
            {
                _debugText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the files row double click command.
        /// </summary>
        /// <value>The files row double click command.</value>
        public ICommand FilesRowDblClickCommand { get; }

        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        /// <value>The package.</value>
        public SolutionAnalyzerPackage Package
        {
            get
            {
                return _package;
            }
            set
            {
                _package = value;
                OnPackageInitialized();
            }
        }

        /// <summary>
        /// Gets or sets the selected class member.
        /// </summary>
        /// <value>The selected class member.</value>
        public ClassMemberDataItem SelectedClassMember
        {
            get
            {
                return _selectedClassMember;
            }
            set
            {
                if (_selectedClassMember != value)
                {
                    _selectedClassMember = value;

                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected class members.
        /// </summary>
        /// <value>The selected class members.</value>
        public List<ClassMemberDataItem>? SelectedClassMembers { get; set; }

        /// <summary>
        /// Gets or sets the selected file.
        /// </summary>
        /// <value>The selected file.</value>
        public FileDataItem? SelectedFile
        {
            get
            {
                return _selectedFile;
            }
            set
            {
                if (_selectedFile != value)
                {
                    _selectedFile = value;
                    //UpdateCurrentSelectionText();
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(
                            () =>
                                {
                                    UpdateCurrentSelectionText();
                                    UpdateFileAsync();
                                }));
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected project.
        /// </summary>
        /// <value>The selected project.</value>
        public ProjectDataItem SelectedProject
        {
            get
            {
                return _selectedProject;
            }
            set
            {
                if (_selectedProject != value)
                {
                    _selectedProject = value;
                    SelectedFile = null;
                    //if (_selectedProject != AllProjectsDataItem)
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(
                            () =>
                                {
                                    UpdateCurrentSelectionText();
                                    UpdateProjectMainAsync();
                                }));

                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the solution container.
        /// </summary>
        /// <value>The solution container.</value>
        public VsSolutionContainer SolutionContainer
        {
            get
            {
                return _solutionContainer;
            }
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        /// <value>The selected items.</value>
        private IList<ClassMemberDataItem> SelectedItems { get; } = new List<ClassMemberDataItem>();
    }
}
