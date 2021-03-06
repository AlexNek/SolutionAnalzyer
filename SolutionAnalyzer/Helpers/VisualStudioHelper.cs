using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EnvDTE;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using SolutionAnalyzer.CommonData;

using Document = Microsoft.CodeAnalysis.Document; 
using Project = Microsoft.CodeAnalysis.Project;
using Solution = Microsoft.CodeAnalysis.Solution;
using Task = System.Threading.Tasks.Task;
using TextDocument = EnvDTE.TextDocument;

namespace SolutionAnalyzer.Helpers
{
    /// <summary>
    /// Class VisualStudioHelper.
    /// Helper class for tasks related to visual studio.
    /// </summary>
    internal static class VisualStudioHelper
    {
        /// <summary>
        /// Adds the new file to project.
        /// </summary>
        /// <param name="asyncPackage">The asynchronous package.</param>
        /// <param name="fileDataItem">The file data item.</param>
        /// <param name="newFullFileName">New name of the full file.</param>
        /// <param name="newFileContent">New content of the file.</param>
        public static void AddNewFileToProject(
            AsyncPackage asyncPackage,
            FileDataItem fileDataItem,
            string newFullFileName,
            string newFileContent)
        {
            VisualStudioWorkspace workspace = GetVisualStudioWorkspace(asyncPackage);
            ProjectDataItem projectDataItem = fileDataItem.Parent;
            Project? project = workspace.CurrentSolution.GetProject(projectDataItem.ProjectId);

            List<string> folders = GetFileSubDirs(project, newFullFileName);

            string fileName = Path.GetFileName(newFullFileName);
            Document? document = project?.AddDocument(fileName, newFileContent, folders);
            Solution? newSolution = document?.Project.Solution;
            bool canApplyChange = workspace.CanApplyChange(ApplyChangesKind.AddDocument);
            bool applyChanges = newSolution != null && workspace.TryApplyChanges(newSolution);
        }

        /// <summary>
        /// Adds the partial keyword to the class.
        /// </summary>
        /// <param name="syntaxNodeRoot">The syntax node root.</param>
        /// <param name="roslynDocument">The roslyn document.</param>
        /// <param name="workspace">The workspace.</param>
        public static void AddPartialToClass(SyntaxNode syntaxNodeRoot, Document roslynDocument, VisualStudioWorkspace workspace)
        {
            var nodesWithClassDirectives =
                from node in syntaxNodeRoot.DescendantNodes()
                where node.Kind() == SyntaxKind.ClassDeclaration
                select node;
            int count = 0;
            foreach (var syntaxNode in nodesWithClassDirectives)
            {
                if (syntaxNode is ClassDeclarationSyntax classNode)
                {
                    ClassDeclarationSyntax newclassDeclaration = classNode.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                    syntaxNodeRoot = syntaxNodeRoot.ReplaceNode(classNode, newclassDeclaration).NormalizeWhitespace();
                    roslynDocument = roslynDocument.WithSyntaxRoot(syntaxNodeRoot);
                    Solution? newSolution = roslynDocument?.Project.Solution;
                    bool canApplyChange = workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);
                    bool applyChanges = workspace.TryApplyChanges(newSolution);
                }

                count++;
                if (count > 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Closes the progress.
        /// </summary>
        /// <param name="statusBar">The status bar.</param>
        /// <param name="userId">The user identifier.</param>
        public static void CloseProgress(IVsStatusbar statusBar, uint userId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Clear the progress bar.
            statusBar.Progress(ref userId, 0, "", 0, 0);
        }

        /// <summary>
        /// Gets the edit point for text document
        /// </summary>
        /// <param name="textDoc">The text document.</param>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        /// <returns>EditPoint.</returns>
        public static EditPoint GetEditPoint(TextDocument textDoc, int line, int column)
        {
            EditPoint editPoint = textDoc.StartPoint.CreateEditPoint();
            try
            {
                editPoint.MoveToLineAndOffset(line, column);
            }
            catch (Exception exception)
            {
                //ProjectData.SetProjectError(exception);
                //throw;
            }

            return editPoint;
        }

        /// <summary>
        /// Gets the visual studio workspace.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns>VisualStudioWorkspace.</returns>
        public static VisualStudioWorkspace GetVisualStudioWorkspace(AsyncPackage package)
        {
            //IComponentModel componentModel = package.GetService<SComponentModel, IComponentModel>();
            IComponentModel componentModel = package.GetService<SComponentModel, IComponentModel>();

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            return workspace;
        }

        /// <summary>
        /// Get visual studio workspace as an asynchronous operation.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns>Task&lt;VisualStudioWorkspace&gt;.</returns>
        public static async Task<VisualStudioWorkspace> GetVisualStudioWorkspaceAsync(AsyncPackage package)
        {
            IComponentModel componentModel = await package.GetServiceAsync<SComponentModel, IComponentModel>();
            //IComponentModel componentModel = package.GetService<SComponentModel, IComponentModel>();

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            return workspace;
        }

        /// <summary>
        /// Initializes the progress bar
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IVsStatusbar.</returns>
        public static IVsStatusbar InitProgress(out uint userId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsStatusbar statusBar = (IVsStatusbar)Package.GetGlobalService(typeof(SVsStatusbar));
            userId = 0;
            string label = "Writing to the progress bar";

            // Initialize the progress bar.
            statusBar.Progress(ref userId, 1, "", 0, 0);

            return statusBar;
        }

        /// <summary>
        /// Opens the document and select the text block.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="startLinePosition">The start line position.</param>
        /// <param name="endLinePosition">The end line position.</param>
        public static void OpenDocumentSelectTextBlock(string filePath, in LinePosition startLinePosition, in LinePosition endLinePosition)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //VSConstants.GUID_ProjectDesignerEditor
            IVsWindowFrame windowFrame = GetDocumentFrame(filePath);

            object docData;
            if (windowFrame != null)
            {
                windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
                // Get the VsTextBuffer  
                IVsTextBuffer buffer = docData as IVsTextBuffer;
                //buffer = {Microsoft.VisualStudio.Editor.Implementation.VsTextBufferAdapter}
                if (buffer == null)
                {
                    if (docData is IVsTextBufferProvider bufferProvider)
                    {
                        int textBuffer = bufferProvider.GetTextBuffer(out IVsTextLines lines);
                        ErrorHandler.ThrowOnFailure(textBuffer);
                        buffer = lines as VsTextBuffer;
                        //Debug.Assert(buffer != null, "IVsTextLines does not implement IVsTextBuffer");
                        if (buffer == null)
                        {
                            OutputWindowHelper.WarningWriteLine("IVsTextLines does not implement IVsTextBuffer");
                        }
                    }
                }
                else
                {
                    IVsTextManager mgr = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;
                    Guid logicalView = VSConstants.LOGVIEWID_Code;
                    mgr.NavigateToLineAndColumn(
                        buffer,
                        ref logicalView,
                        startLinePosition.Line,
                        startLinePosition.Character,
                        endLinePosition.Line,
                        endLinePosition.Character);
                }
            }
        }

        /// <summary>
        /// Replaces the text.
        /// </summary>
        /// <param name="epStart">The ep start.</param>
        /// <param name="tpEnd">The tp end.</param>
        /// <param name="text">The text.</param>
        /// <param name="flags">The flags.</param>
        public static void ReplaceText(EditPoint epStart, TextPoint tpEnd, string text, int flags = 0)
        {
            try
            {
                epStart.ReplaceText(tpEnd, text, flags);
            }
            catch (Exception exception)
            {
                //ProjectData.SetProjectError(exception);
            }
        }

        /// <summary>
        /// Get text from DTE file as an asynchronous operation.
        /// </summary>
        /// <param name="projectItemDte">The project item DTE.</param>
        /// <param name="startLinePosition">The start line position.</param>
        /// <param name="endLinePosition">The end line position.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        internal static async Task<string> GetTextAsync(ProjectItem projectItemDte, LinePosition? startLinePosition, LinePosition? endLinePosition)
        {
            if (projectItemDte == null)
            {
                return await Task.FromResult<string>(null);
            }

            if (startLinePosition == null || endLinePosition == null)
            {
                return await Task.FromResult<string>(null);
            }

            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            //var fileName = Path.GetFileNameWithoutExtension(document.FullName);
            // don't work when source code window reopened
            //ProjectItem projectItem = dteDocument.ProjectItem;
            //LogicalView.Text - 5 	The corresponding GUID value is 7651A703-06E5-11D1-8EBD-00A0C90F26EA.
            //GuidList.LOGVIEWID_TextView;
            //VSConstants.LOGVIEWID_TextView;
            //{7651A703-06E5-11D1-8EBD-00A0C90F26EA}
            Window window = projectItemDte.Open("{7651A703-06E5-11D1-8EBD-00A0C90F26EA}");
            window.Activate();
            TextDocument textDoc = (TextDocument)window.Document.Object();
            EditPoint ep1 = GetEditPoint(textDoc, startLinePosition.Value.Line + 1, startLinePosition.Value.Character + 1);
            EditPoint ep2 = GetEditPoint(textDoc, endLinePosition.Value.Line + 1, endLinePosition.Value.Character + 1);
            return ep1.GetText(ep2);
        }

        /// <summary>
        /// Replace text as an asynchronous operation.
        /// </summary>
        /// <param name="projectItemDte">The project item DTE.</param>
        /// <param name="startLinePosition">The start line position.</param>
        /// <param name="endLinePosition">The end line position.</param>
        /// <param name="newText">The new text.</param>
        /// <returns>Task&lt;System.Nullable&lt;System.Object&gt;&gt;.</returns>
        internal static async Task<object?> ReplaceTextAsync(
            ProjectItem projectItemDte,
            LinePosition? startLinePosition,
            LinePosition? endLinePosition,
            string newText)
        {
            if (projectItemDte == null)
            {
                return await Task.FromResult<object?>(null);
            }

            if (startLinePosition == null || endLinePosition == null)
            {
                return await Task.FromResult<object?>(null);
            }

            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            //var extension = Path.GetExtension(document.FullName);
            //if (extension == null)
            //{
            //    return await Task.FromResult<object?>(null);
            //}

            //var fileName = Path.GetFileNameWithoutExtension(document.FullName);
            // don't work when source code window reopened
            //ProjectItem projectItem = dteDocument.ProjectItem;
            //LogicalView.Text - 5 	The corresponding GUID value is 7651A703-06E5-11D1-8EBD-00A0C90F26EA.
            //GuidList.LOGVIEWID_TextView;
            //VSConstants.LOGVIEWID_TextView;
            //{7651A703-06E5-11D1-8EBD-00A0C90F26EA}
            Window window = projectItemDte.Open("{7651A703-06E5-11D1-8EBD-00A0C90F26EA}");
            window.Activate();
            TextDocument textDoc = (TextDocument)window.Document.Object();
            EditPoint ep1 = GetEditPoint(textDoc, startLinePosition.Value.Line + 1, startLinePosition.Value.Character + 1);
            EditPoint ep2 = GetEditPoint(textDoc, endLinePosition.Value.Line + 1, endLinePosition.Value.Character + 1);
            ReplaceText(ep1, ep2, newText);

            //Selection? selection = GetSelection(dteDocument);
            //if (selection == null)
            //{
            //    return await Task.FromResult<object?>(null);
            //}

            //selection.ReplaceWith(replacement);

            ////fake delay for hiding return error
            //await System.Threading.Tasks.Task.Delay(10);
            return await Task.FromResult<object?>(null);
        }

        /// <summary>
        /// Gets the document frame.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>IVsWindowFrame.</returns>
        private static IVsWindowFrame GetDocumentFrame(string filePath)
        {
            IVsUIShellOpenDocument openDoc = Package.GetGlobalService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

            IVsWindowFrame frame = null;
            Guid logicalView = VSConstants.LOGVIEWID_Code;
            if (openDoc != null)
            {
                int documentViaProject = openDoc.OpenDocumentViaProject(
                    filePath,
                    ref logicalView,
                    out Microsoft.VisualStudio.OLE.Interop.IServiceProvider _,
                    out IVsUIHierarchy _,
                    out uint _,
                    out frame);
                if (ErrorHandler.Failed(documentViaProject) || frame == null)
                {
                    OutputWindowHelper.WarningWriteLine($"Can't not open document {filePath}");
                    return frame;
                }
            }

            return frame;
        }

        /// <summary>
        /// Gets the file sub directories.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="newFullFileName">New name of the full file.</param>
        /// <returns>List&lt;System.String&gt;.</returns>
        private static List<string> GetFileSubDirs(Project? project, string newFullFileName)
        {
            string? projectDirectoryName = Path.GetDirectoryName(project?.FilePath);
            string[] projectDirs = projectDirectoryName.Split(Path.DirectorySeparatorChar);

            string directoryName = Path.GetDirectoryName(newFullFileName);
            string[] fileDirs = directoryName.Split(Path.DirectorySeparatorChar);

            List<string> folders = new List<string>();
            for (int i = 0; i < fileDirs.Length; i++)
            {
                string fileDir = fileDirs[i];
                if (i < projectDirs.Length)
                {
                    string projectDir = projectDirs[i];
                    if (projectDir != fileDir)
                    {
                        folders.Add(fileDir);
                    }
                }
                else
                {
                    folders.Add(fileDir);
                }
            }

            return folders;
        }
    }
}
