#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;

using SolutionAnalyzer.CodeVisitors;
using SolutionAnalyzer.Common;
using SolutionAnalyzer.CommonData;
using SolutionAnalyzer.Helpers;

using Document = Microsoft.CodeAnalysis.Document;
using Project = Microsoft.CodeAnalysis.Project;
using Task = System.Threading.Tasks.Task;

//using Document = EnvDTE.Document;
//using Project = EnvDTE.Project;

namespace SolutionAnalyzer
{
    /// <summary>
    /// Class VsSolutionContainer.
    /// </summary>
    internal class VsSolutionContainer
    {
        /// <summary>
        /// Delegate NotifyInfo
        /// </summary>
        /// <param name="notifyData">The notify data.</param>
        public delegate void NotifyInfo(NotifyData notifyData);

        /// <summary>
        /// Enum EItemKind
        /// </summary>
        public enum EItemKind
        {
            /// <summary>
            /// The none
            /// </summary>
            None,

            /// <summary>
            /// The physical file
            /// </summary>
            PhysicalFile,

            /// <summary>
            /// The physical folder
            /// </summary>
            PhysicalFolder,

            /// <summary>
            /// The sub project
            /// </summary>
            SubProject,

            /// <summary>
            /// The virtual folder
            /// </summary>
            VirtualFolder
        }

        /// <summary>
        /// Occurs when [notify].
        /// </summary>
        public event NotifyInfo Notify;

        /// <summary>
        /// The cs unique identifier
        /// </summary>
        private static readonly Guid CsGuid = new Guid("B5E9BD34-6D3E-4B5D-925E-8A43B79820B4");

        /// <summary>
        /// The physical file unique identifier
        /// </summary>
        private static readonly Guid PhysicalFileGuid = new Guid(Constants.vsProjectItemKindPhysicalFile);

        /// <summary>
        /// The physical folder unique identifier
        /// </summary>
        private static readonly Guid PhysicalFolderGuid = new Guid(Constants.vsProjectItemKindPhysicalFolder);

        /// <summary>
        /// The sub project unique identifier
        /// </summary>
        private static readonly Guid SubProjectGuid = new Guid(Constants.vsProjectItemKindSubProject);

        //project kind FAE04EC0-301F-11D3-BF4B-00C04F79EFBC
        /// <summary>
        /// The vb unique identifier
        /// </summary>
        private static readonly Guid VbGuid = new Guid("B5E9BD33-6D3E-4B5D-925E-8A43B79820B4");

        /// <summary>
        /// The virtual folder unique identifier
        /// </summary>
        private static readonly Guid VirtualFolderGuid = new Guid(Constants.vsProjectItemKindVirtualFolder);

        /// <summary>
        /// The DTE
        /// </summary>
        private readonly DTE2 _dte;

        /// <summary>
        /// The package
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// The workspace
        /// </summary>
        private readonly VisualStudioWorkspace _workspace;

        /// <summary>
        /// The build events
        /// </summary>
        private BuildEvents _buildEvents;

        /// <summary>
        /// The document events
        /// </summary>
        private DocumentEvents _documentEvents;

        /// <summary>
        /// The misc files events
        /// </summary>
        private ProjectItemsEvents _miscFilesEvents;

        /// <summary>
        /// The project items events
        /// </summary>
        private ProjectItemsEvents _projectItemsEvents;

        /// <summary>
        /// The solution events
        /// </summary>
        private SolutionEvents _solutionEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsSolutionContainer"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        public VsSolutionContainer(AsyncPackage package)
        {
            _package = package;

            _workspace = VisualStudioHelper.GetVisualStudioWorkspace(_package);
            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
            _dte = _package.GetService<DTE, DTE2>();
            if (_dte.Solution != null)
            {
                IsSolutionOpened = _dte.Solution.IsOpen;
            }

            InitEvents();
        }

        /// <summary>
        /// get class members as an asynchronous operation.
        /// </summary>
        /// <param name="fileDataItem">The file data item.</param>
        /// <returns>Task&lt;IList&lt;ClassMemberDataItem&gt;&gt;.</returns>
        public async Task<IList<ClassMemberDataItem>> GetClassMembersAsync(FileDataItem fileDataItem)
        {
            VisualStudioWorkspace workspace = await VisualStudioHelper.GetVisualStudioWorkspaceAsync(_package);

            Document? roslynDocument = workspace.CurrentSolution.GetDocument(fileDataItem.DocumentId);
            if (roslynDocument != null)
            {
                SyntaxTree? tree = await roslynDocument.GetSyntaxTreeAsync();
                SyntaxNode? syntaxNode = await tree?.GetRootAsync();
                if (syntaxNode != null)
                {
                    CsCodeClassMemberVisitor classMembersVisitor = new CsCodeClassMemberVisitor();

                    SyntaxNodeWalker walker = new SyntaxNodeWalker(_package, fileDataItem);
                    walker.Visit(syntaxNode, classMembersVisitor);
                    return classMembersVisitor.ClassMemberDataItems;
                }
            }

            return await Task.FromResult<IList<ClassMemberDataItem>>(null);
        }

        /// <summary>
        /// Gets the project items.
        /// </summary>
        /// <param name="projectItem">The project item.</param>
        /// <param name="items">The items.</param>
        /// <exception cref="ArgumentNullException">projectItem</exception>
        public async Task GetProjectItems(ProjectDataItem projectItem, IList<FileDataItem> items)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VisualStudioWorkspace workspace = VisualStudioHelper.GetVisualStudioWorkspace(_package);
            Project projectRoslyn = workspace.CurrentSolution.GetProject(projectItem.ProjectId);

            if (projectRoslyn != null)
            {
                GetProjectItems(projectItem, projectRoslyn.Documents, items);

                Dictionary<string, Document> roslynFileMap = new Dictionary<string, Document>();
                FillRoslynProjectFiles(projectItem, roslynFileMap);
                foreach (FileDataItem fileDataItem in items)
                {
                    if (roslynFileMap.ContainsKey(fileDataItem.FullPath))
                    {
                        Document roslynDocument = roslynFileMap[fileDataItem.FullPath];
                        fileDataItem.DocumentId = roslynDocument.Id;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the projects.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="sb">The sb.</param>
        public void GetProjects(IList<ProjectDataItem> items, StringBuilder sb)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Dictionary<string, Project> roslynProjectMap = new Dictionary<string, Project>();

            FillRoslynProjectMap(roslynProjectMap);

            try
            {
                var solutionDte = _dte.Solution;
                Projects dteProjects = solutionDte.Projects;
                foreach (EnvDTE.Project dteProject in dteProjects)
                {
                    ELanguage language = ELanguage.None;
                    if (dteProject.CodeModel != null)
                    {
                        language = GetLanguage(dteProject.CodeModel.Language);
                    }

                    ProjectDataItem projectItem = new ProjectDataItem();
                    projectItem.Name = dteProject.Name;
                    projectItem.Language = language;

                    if (roslynProjectMap.ContainsKey(dteProject.FullName))
                    {
                        projectItem.ProjectId = roslynProjectMap[dteProject.FullName].Id;
                    }

                    items.Add(projectItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Trace.WriteLine(ex);
            }
        }

        /// <summary>
        /// Called when [notify].
        /// </summary>
        /// <param name="notifyData">The notify data.</param>
        protected virtual void OnNotify(NotifyData notifyData)
        {
            Notify?.Invoke(notifyData);
        }

        /// <summary>
        /// Builds the events on build begin.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="action">The action.</param>
        private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
        }

        /// <summary>
        /// Builds the events on build done.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="action">The action.</param>
        private void BuildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
        }

        /// <summary>
        /// Documents the events document saved.
        /// </summary>
        /// <param name="document">The document.</param>
        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
        }

        /// <summary>
        /// Fills the roslyn project files.
        /// </summary>
        /// <param name="projectItem">The project item.</param>
        /// <param name="roslynFileMap">The roslyn file map.</param>
        private void FillRoslynProjectFiles(ProjectDataItem projectItem, Dictionary<string, Document> roslynFileMap)
        {
            Project project = _workspace.CurrentSolution.GetProject(projectItem.ProjectId);
            if (project != null)
            {
                foreach (Document roslynDocument in project.Documents)
                {
                    if (roslynDocument.FilePath != null && roslynDocument.SupportsSyntaxTree)
                    {
                        roslynFileMap.Add(roslynDocument.FilePath, roslynDocument);
                    }
                }
            }
        }

        /// <summary>
        /// Fills the roslyn project map.
        /// </summary>
        /// <param name="roslynProjectMap">The roslyn project map.</param>
        private void FillRoslynProjectMap(Dictionary<string, Project> roslynProjectMap)
        {
            //IComponentModel componentModel = _package.GetService<SComponentModel, IComponentModel>();

            //var workspace = componentModel.GetService<VisualStudioWorkspace>();
            Microsoft.CodeAnalysis.Solution roslynSolution = _workspace.CurrentSolution;
            foreach (Project roslynProject in roslynSolution.Projects)
            {
                if (roslynProject.FilePath != null && roslynProject.SupportsCompilation)
                {
                    //roslynProject.Id
                    roslynProjectMap.Add(roslynProject.FilePath, roslynProject);
                }
            }
        }

        /// <summary>
        /// Gets the kind of the item.
        /// </summary>
        /// <param name="itemTypeGuid">The item type unique identifier.</param>
        /// <returns>EItemKind.</returns>
        private static EItemKind GetItemKind(string itemTypeGuid)
        {
            //itemTypeGuid = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}"
            EItemKind ret = EItemKind.None;
            Guid guid = new Guid(itemTypeGuid);

            if (guid == PhysicalFileGuid)
            {
                ret = EItemKind.PhysicalFile;
            }
            else if (guid == PhysicalFolderGuid)
            {
                ret = EItemKind.PhysicalFolder;
            }
            else if (guid == SubProjectGuid)
            {
                ret = EItemKind.SubProject;
            }
            else if (guid == VirtualFolderGuid)
            {
                ret = EItemKind.VirtualFolder;
            }

            return ret;
        }

        /// <summary>
        /// Gets the language.
        /// </summary>
        /// <param name="languageGuid">The language unique identifier.</param>
        /// <returns>ELanguage.</returns>
        private static ELanguage GetLanguage(string languageGuid)
        {
            ELanguage ret = ELanguage.None;
            if (string.IsNullOrEmpty(languageGuid))
            {
                return ret;
            }

            Guid guid = new Guid(languageGuid);
            if (guid == CsGuid)
            {
                ret = ELanguage.CSharp;
            }
            else if (guid == VbGuid)
            {
                ret = ELanguage.VisualBasic;
            }

            return ret;
        }

        /// <summary>
        /// Gets the project items.
        /// </summary>
        /// <param name="projectDataItem">The project data item.</param>
        /// <param name="roslynProjectItems">The roslyn project items.</param>
        /// <param name="items">The items.</param>
        private void GetProjectItems(
            ProjectDataItem projectDataItem,
            IEnumerable<Document> roslynProjectItems,
            IList<FileDataItem> items)
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(TraceProjectItems));

            foreach (Document documentRoslyn in roslynProjectItems)
            {
                string fullPathStr = documentRoslyn.FilePath;

                string documentName = String.Empty;

                if (documentRoslyn.Folders.Count == 0)
                {
                    documentName = documentRoslyn.Name;
                }
                else
                {
                    if (documentRoslyn.Folders[0] != "obj" && documentRoslyn.Folders[0] != "Properties")
                    {
                        documentName = String.Join("/", documentRoslyn.Folders) + "/" + documentRoslyn.Name;
                    }
                }

                if (!string.IsNullOrEmpty(documentName) && !documentName.Contains("Designer"))
                {
                    FileDataItem fileDataItem = new FileDataItem
                                                    {
                                                        Language = projectDataItem.Language,
                                                        FullPath = fullPathStr,
                                                        Name = documentName
                                                        //DocumentDte = documentRoslyn.Document,
                                                        //ProjectItemDte = documentRoslyn
                                                    };
                    fileDataItem.Parent = projectDataItem;
                    items.Add(fileDataItem);
                }
            }
        }

        /// <summary>
        /// Initializes the events.
        /// </summary>
        private void InitEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(new CancellationToken());

            if (_dte != null)
            {
                Events dteEvents = _dte.Events;
                _solutionEvents = dteEvents.SolutionEvents;
                _solutionEvents.ProjectAdded += SolutionEventsProjectAdded;
                _solutionEvents.ProjectRemoved += SolutionEventsProjectRemoved;
                _solutionEvents.ProjectRenamed += SolutionEventsProjectRenamed;
                _solutionEvents.Opened += SolutionEventOpened;
                _solutionEvents.AfterClosing += SolutionEventsAfterClosing;

                _documentEvents = dteEvents.DocumentEvents;
                _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

                _projectItemsEvents = dteEvents.SolutionItemsEvents;
                _projectItemsEvents.ItemAdded += ProjectItemsEvents_ItemAdded;
                _projectItemsEvents.ItemRemoved += ProjectItemsEvents_ItemRemoved;
                _projectItemsEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;

                _miscFilesEvents = dteEvents.MiscFilesEvents;
                _miscFilesEvents.ItemAdded += ProjectItemsEvents_ItemAdded;
                _miscFilesEvents.ItemRemoved += ProjectItemsEvents_ItemRemoved;
                _miscFilesEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;

                _buildEvents = dteEvents.BuildEvents;
                _buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
                _buildEvents.OnBuildDone += BuildEvents_OnBuildDone;
            }
        }

        /// <summary>
        /// Projects the items events item added.
        /// </summary>
        /// <param name="projectItem">The project item.</param>
        private void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
        }

        /// <summary>
        /// Projects the items events item removed.
        /// </summary>
        /// <param name="projectItem">The project item.</param>
        private void ProjectItemsEvents_ItemRemoved(ProjectItem projectItem)
        {
        }

        /// <summary>
        /// Projects the items events item renamed.
        /// </summary>
        /// <param name="projectItem">The project item.</param>
        /// <param name="oldName">The old name.</param>
        private void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
        }

        /// <summary>
        /// Solutions the event opened.
        /// </summary>
        private void SolutionEventOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IsSolutionOpened = true;
            OnNotify(new NotifyData { NotifyType = NotifyData.ENotifyType.SolutionOpened });
        }

        /// <summary>
        /// Solutions the events after closing.
        /// </summary>
        private void SolutionEventsAfterClosing()
        {
            IsSolutionOpened = false;
            OnNotify(new NotifyData { NotifyType = NotifyData.ENotifyType.SolutionClosed });
        }

        /// <summary>
        /// Solutions the events project added.
        /// </summary>
        /// <param name="p">The p.</param>
        private void SolutionEventsProjectAdded(EnvDTE.Project p)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                //this.AddProject_Async(p);
                //OnNotify(new NotifyData { NotifyType = NotifyData.ENotifyType. });
            }
            catch (Exception exception)
            {
                //ErrorMessageService.ShowErrorDialog(exception, "");
            }
        }

        /// <summary>
        /// Solutions the events project removed.
        /// </summary>
        /// <param name="p">The p.</param>
        private void SolutionEventsProjectRemoved(EnvDTE.Project p)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }

        /// <summary>
        /// Solutions the events project renamed.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="OldName">The old name.</param>
        private void SolutionEventsProjectRenamed(EnvDTE.Project p, string OldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }

        /// <summary>
        /// Traces the project items.
        /// </summary>
        /// <param name="dteProjectItems">The DTE project items.</param>
        /// <param name="sb">The sb.</param>
        /// <param name="level">The level.</param>
        private static void TraceProjectItems(ProjectItems dteProjectItems, StringBuilder sb, int level)
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(TraceProjectItems));
            //PhysicalFile_guid   6bb5f8ee - 4483 - 11d3 - 8bcf - 00c04f8ec28c
            //PhysicalFolder_guid 6bb5f8ef - 4483 - 11d3 - 8bcf - 00c04f8ec28c
            //SubProject_guid     EA6618E8 - 6E24 - 4528 - 94BE - 6889FE16485C
            //VirtualFolder_guid  6bb5f8f0 - 4483 - 11d3 - 8bcf - 00c04f8ec28c

            foreach (ProjectItem dteItem in dteProjectItems)
            {
                for (int i = 0; i < level + 1; i++)
                {
                    sb?.Append("\t");
                }

                EItemKind itemKind = GetItemKind(dteItem.Kind);
                string languageGuid = dteItem.FileCodeModel?.Language;

                ELanguage language = GetLanguage(languageGuid);
                string languageStr = language.ToString();
                if (language == ELanguage.None)
                {
                    languageStr = "'" + languageGuid + "'";
                }

                StringBuilder sbProp = new StringBuilder();

                foreach (Property dteProperty in dteItem.Properties)
                {
                    try
                    {
                        sbProp.AppendFormat("{0}-{1} ", dteProperty?.Name, dteProperty?.Value);
                    }
                    catch (Exception ex)
                    {
                        //ignore all
                    }
                }

                EnvDTE.Document dteDocument = dteItem.Document;
                //dteDocument.
                sb?.AppendFormat(
                    "DteItem:{0},  kind {1}, subItems: {2}, codeModel lang {3}, files{4} count add {5} sub proj: {6}, Prop {7}",
                    dteItem.Name,
                    itemKind,
                    dteItem.ProjectItems.Count,
                    languageStr,
                    dteItem.FileCount,
                    dteItem.Collection.Count,
                    dteItem.SubProject?.FileName,
                    sbProp);
                sb?.AppendLine();

                TraceProjectItems(dteItem.ProjectItems, sb, level + 1);
                //if (level < 2)
                //{
                //    Duplicate current group items  
                //    TraceProjectItems(dteItem.Collection, sb, level + 2);
                //}
            }
        }

        /// <summary>
        /// Handles the WorkspaceChanged event of the Workspace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="WorkspaceChangeEventArgs"/> instance containing the event data.</param>
        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            IVsOperationProgressStatusService service = _package.GetService<SVsOperationProgress, IVsOperationProgressStatusService>();
            IVsOperationProgressStageStatusForSolutionLoad statusForSolutionLoad = null;
            if (service != null)
            {
                try
                {
                    statusForSolutionLoad = service.GetStageStatusForSolutionLoad(CommonOperationProgressStageIds.Intellisense);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            DocumentId documentId = e.DocumentId;
            WorkspaceChangeKind workspaceChangeKind = e.Kind;
            ProjectId projectId = e.ProjectId;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is solution opened.
        /// </summary>
        /// <value><c>true</c> if this instance is solution opened; otherwise, <c>false</c>.</value>
        public bool IsSolutionOpened { get; private set; }
    }
}
