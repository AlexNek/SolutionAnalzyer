#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using SolutionAnalyzer.CodeVisitors;
using SolutionAnalyzer.Common;
using SolutionAnalyzer.CommonData;

using Document = Microsoft.CodeAnalysis.Document;
using Task = System.Threading.Tasks.Task;

namespace SolutionAnalyzer.Helpers
{
    /// <summary>
    /// Class CodeGeneratorHelper.
    /// Can create a new class part from the selected members
    /// </summary>
    internal class CodeGeneratorHelper
    {
        /// <summary>
        /// The package
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        /// <param name="package">The package.</param>
        public CodeGeneratorHelper(AsyncPackage package)
        {
            _package = package;
        }

        /// <summary>
        /// Creates the new name from the full file name
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <returns>System.String.</returns>
        public static string CreateNewFullFileName(string fullPath)
        {
            int partNumber = 1;
            string newFullFileName;

            string directoryName = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            do
            {
                newFullFileName = string.Format("{0}.Part{1:D2}{2}", Path.Combine(directoryName, fileName), partNumber++, extension);
            }
            while (File.Exists(newFullFileName));

            return newFullFileName;
        }

        /// <summary>
        /// Generate new class part as an asynchronous operation.
        /// </summary>
        /// <param name="fileDataItem">The file data item.</param>
        /// <param name="classMemberDataItems">The class member data items.</param>
        public async Task GenerateNewClassPartAsync(FileDataItem? fileDataItem, List<ClassMemberDataItem>? classMemberDataItems = null)
        {
            if (fileDataItem != null)
            {
                List<ISyntaxNodeVisitor> visitors = new List<ISyntaxNodeVisitor>();

                CsCodeGetUsingInfoVisitor usingInfoVisitor = new CsCodeGetUsingInfoVisitor();
                CsCodeGetNameSpaceInfoVisitor nameSpaceInfoVisitor = new CsCodeGetNameSpaceInfoVisitor();
                CsCodeGetClassNameInfoVisitor classNameInfoVisitor = new CsCodeGetClassNameInfoVisitor();
                CsCodeIsClassPartialVisitor isClassPartialVisitor = new CsCodeIsClassPartialVisitor();

                visitors.Add(usingInfoVisitor);
                visitors.Add(nameSpaceInfoVisitor);
                visitors.Add(classNameInfoVisitor);
                visitors.Add(isClassPartialVisitor);

                VisualStudioWorkspace workspace = await VisualStudioHelper.GetVisualStudioWorkspaceAsync(_package);
                Document? roslynDocument = workspace.CurrentSolution.GetDocument(fileDataItem.DocumentId);
                if (roslynDocument != null)
                {
                    SyntaxTree? tree = await roslynDocument.GetSyntaxTreeAsync();
                    SyntaxNode? syntaxNodeRoot = await tree?.GetRootAsync();
                    if (syntaxNodeRoot != null)
                    {
                        SyntaxNodeWalker walker = new SyntaxNodeWalker(_package, fileDataItem);
                        walker.Visit(syntaxNodeRoot, visitors);
                        //TestInfo test = new TestInfo(Package, this);
                        //DebugText = test.CreateTestInfo();

                        string newFullFileName = CreateNewFullFileName(fileDataItem.FullPath);
                        CodeGeneratorParameters parameters = new CodeGeneratorParameters
                                                                 {
                                                                     UsingInfoVisitor = usingInfoVisitor,
                                                                     NameSpaceInfoVisitor = nameSpaceInfoVisitor,
                                                                     IsClassPartialVisitor = isClassPartialVisitor,
                                                                     ClassNameInfoVisitor = classNameInfoVisitor
                                                                 };

                        string newFileContent = await GetNewClassPartBodyAsync(parameters, classMemberDataItems, fileDataItem.DocumentId);
                        if (string.IsNullOrEmpty(newFileContent))
                        {
                            Trace.WriteLine("Error by file generation");
                            VsShellUtilities.ShowMessageBox(
                                _package,
                                "Error by file generation",
                                "Error",
                                OLEMSGICON.OLEMSGICON_WARNING,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            return;
                        }

                        try
                        {
                            if (!ThreadHelper.CheckAccess())
                            {
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            }

                            if (classMemberDataItems != null && classMemberDataItems.Count > 0)
                            {
                                if (!isClassPartialVisitor.IsClassPartial)
                                {
                                    VisualStudioHelper.AddPartialToClass(syntaxNodeRoot, roslynDocument, workspace);
                                }

                                VisualStudioHelper.AddNewFileToProject(_package, fileDataItem, newFullFileName, newFileContent);
                                IVsStatusbar progressBar = VisualStudioHelper.InitProgress(out uint userId);
                                try
                                {
                                    List<ClassMemberDataItem> copyOfDataMembers = new List<ClassMemberDataItem>(classMemberDataItems);

                                    DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                                    ProjectItem projectItemDte = dte.Solution.FindProjectItem(fileDataItem.FullPath);
                                    //We must delete from the end of file
                                    copyOfDataMembers.Sort((x, y) => -x.StartLine.CompareTo(y.StartLine));
                                    for (int i = 0; i < copyOfDataMembers.Count; i++)
                                    {
                                        ClassMemberDataItem classMemberDataItem = copyOfDataMembers[i];

                                        progressBar.Progress(
                                            ref userId,
                                            1,
                                            "Delete code for moved data members",
                                            (uint)i,
                                            (uint)copyOfDataMembers.Count - 1);
                                        await VisualStudioHelper.ReplaceTextAsync(
                                            projectItemDte,
                                            classMemberDataItem.StartLinePosition,
                                            classMemberDataItem.EndLinePosition,
                                            String.Empty);
                                    }
                                }
                                finally
                                {
                                    VisualStudioHelper.CloseProgress(progressBar, userId);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex);
                            VsShellUtilities.ShowMessageBox(
                                _package,
                                ex.Message,
                                "Error",
                                OLEMSGICON.OLEMSGICON_WARNING,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates the class definition part
        /// </summary>
        /// <param name="isClassPartialVisitor">The is class partial visitor.</param>
        /// <param name="classNameInfoVisitor">The class name information visitor.</param>
        /// <param name="classMemberDataItems">The class member data items.</param>
        /// <param name="documentId">The document identifier.</param>
        /// <param name="sb">The string builder.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        private async Task<bool> GenerateClassDefinition(
            CsCodeIsClassPartialVisitor? isClassPartialVisitor,
            CsCodeGetClassNameInfoVisitor? classNameInfoVisitor,
            List<ClassMemberDataItem>? classMemberDataItems,
            DocumentId documentId,
            StringBuilder sb,
            string offset)
        {
            bool isError = false;
            sb.Append($"{offset}{classNameInfoVisitor.ClassModifiers}");
            if (!string.IsNullOrEmpty(classNameInfoVisitor.ClassModifiers))
            {
                sb.Append(" ");
            }

            if (!isClassPartialVisitor.IsClassPartial)
            {
                sb.Append("partial ");
            }

            sb.AppendLine($"class {classNameInfoVisitor.ClassName}");

            sb.AppendLine($"{offset}{{");
            if (classMemberDataItems == null)
            {
                sb.AppendLine();
            }
            else
            {
                IVsStatusbar progressBar = VisualStudioHelper.InitProgress(out uint userId);
                try
                {
                    VisualStudioWorkspace workspace = await VisualStudioHelper.GetVisualStudioWorkspaceAsync(_package);
                    Document document = workspace.CurrentSolution.GetDocument(documentId);
                    string? documentFilePath = document.FilePath;
                    DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                    ProjectItem projectItemDte = dte.Solution.FindProjectItem(documentFilePath);
                    StringBuilder membersText = new StringBuilder();
                    //IVsTextView textView = await GetTextViewAsync(documentId);
                    for (int i = 0; i < classMemberDataItems.Count; i++)
                    {
                        ClassMemberDataItem classMemberDataItem = classMemberDataItems[i];
                        string selectedText = await VisualStudioHelper.GetTextAsync(
                                                  projectItemDte,
                                                  classMemberDataItem.StartLinePosition,
                                                  classMemberDataItem.EndLinePosition);
                        membersText.Append(selectedText);
                        progressBar.Progress(ref userId, 1, "Get member source code", (uint)i, (uint)classMemberDataItems.Count - 1);
                    }

                    sb.Append(membersText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    VisualStudioHelper.CloseProgress(progressBar, userId);
                }
            }

            sb.AppendLine($"{offset}}}");
            return isError;
        }

        /// <summary>
        /// Get new class part body as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="classMemberDataItems">The class member data items.</param>
        /// <param name="documentId">The document identifier.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        private async Task<string> GetNewClassPartBodyAsync(
            CodeGeneratorParameters parameters,
            List<ClassMemberDataItem>? classMemberDataItems,
            DocumentId documentId)
        {
            bool isError = false;
            StringBuilder sb = new StringBuilder();
            foreach (string text in parameters.UsingInfoVisitor.Trivias)
            {
                sb.AppendLine(text);
            }

            foreach (string text in parameters.UsingInfoVisitor.Usings)
            {
                sb.AppendLine($"using {text};");
            }

            sb.AppendLine();

            if (string.IsNullOrEmpty(parameters.NameSpaceInfoVisitor.Name))
            {
                isError = await GenerateClassDefinition(
                              parameters.IsClassPartialVisitor,
                              parameters.ClassNameInfoVisitor,
                              classMemberDataItems,
                              documentId,
                              sb,
                              string.Empty);
            }
            else
            {
                sb.AppendLine($"namespace {parameters.NameSpaceInfoVisitor.Name}");
                sb.AppendLine("{");
                isError = await GenerateClassDefinition(
                              parameters.IsClassPartialVisitor,
                              parameters.ClassNameInfoVisitor,
                              classMemberDataItems,
                              documentId,
                              sb,
                              "\t");

                sb.AppendLine("}");
            }

            if (isError)
            {
                return String.Empty;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Class CodeGeneratorParameters.
        /// Collection of parameters for class generation
        /// </summary>
        protected class CodeGeneratorParameters
        {
            /// <summary>
            /// The class name information visitor
            /// </summary>
            public CsCodeGetClassNameInfoVisitor? ClassNameInfoVisitor;

            /// <summary>
            /// The is class partial visitor
            /// </summary>
            public CsCodeIsClassPartialVisitor? IsClassPartialVisitor;

            /// <summary>
            /// The name space information visitor
            /// </summary>
            public CsCodeGetNameSpaceInfoVisitor? NameSpaceInfoVisitor;

            /// <summary>
            /// The using information visitor
            /// </summary>
            public CsCodeGetUsingInfoVisitor? UsingInfoVisitor;
        }
    }
}
