# Development tips (Q & A)

## VSPackage troubles.

- How can I load referenced assembly for my VS package. I used in my project another DLLs, but nothing loaded by default.<br />
**A.** Try to add the attribute `[ProvideBindingPath]` to the package class.


- What is for curious numbers in this attribute for a package class?   
 `[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]`<br />
**A.** You can apApply this attribute to VSPackage class to provide information that is displayed on the Visual Studio splash screen and the Help About dialog box.   
Create string resource with name 110 for *product name* and with name 112 for *product details* (I can not find any usage of IconResourceID)

- How can I add my own menu items?<br />
**A.** Add first any command with Visual Studio Wizard, then edit *.vcst file
[Visual Studio Command Table (.Vsct) Files](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/visual-studio-command-table-dot-vsct-files?view=vs-2019)  
For this sample is important to use `id="IDM_VS_MENU_TOOLS"` in group and `id="MenuToolsGroup"` in group and button.
```HTML
<Group guid="guidSharpLocalizationHelperPackageCmdSet" id="MenuToolsGroup" priority="0x0600">
   <Parent guid="guidSHLMainMenu" id="IDM_VS_MENU_TOOLS" />
</Group>
...
<Button guid="guidSharpLocalizationHelperPackageCmdSet" id="CustomCommandsId" priority="0x0100" type="Button">
    <Parent guid="guidSharpLocalizationHelperPackageCmdSet" id="MenuToolsGroup" />
    <Icon guid="guidImages" id="bmpPic1" />
    <Strings>
        <ButtonText>Localization Helper - Tools menu command sample</ButtonText>
     </Strings>
</Button>
```
I can not find a way how to add own menu item for non top level VS menu using `Group` tag.
It is possible by adding correct `Parent` tag direct to the `Button` tag or with addtional section `CommandPlacements`
```HTML
<CommandPlacements>
    <CommandPlacement guid="guidSolutionAnalyzerPackageCmdSet" id="MainWindowCommandId" priority="0x100">
      <Parent guid="guidSHLMainMenu" id="IDG_VS_WNDO_OTRWNDWS1"/>
    </CommandPlacement>
  </CommandPlacements>
```

- How can I add my own window to the VS package?<br />
**A.** First, add WPF/Winforms Tollbox control with visual studio wizard. Two classes wil be added: UI wpapper and user control.
Second, add the command for showing the window.
Third, activate the window
```C#
private void Execute(object sender, EventArgs e)
{
     _package.JoinableTaskFactory.RunAsync(
     async delegate
     {
          ToolWindowPane window = await _package.ShowToolWindowAsync(typeof(ToolWindowAsync), 0, true, _package.DisposalToken);
          if (null == window || null == window.Frame)
          {
               throw new NotSupportedException("Cannot create tool window");
          }
     });
}
```

Don't forget that window could be activated in three different ways:  
1. With custom command.  
1. While starting visual studio. If window was previosly opened and activ.  
1. While switching to window tab. If window was previously opend but another tab was activ.  

## Solution troubles

- How can I read all files from solution? Must I use `DTE` or `VisualStudioWorkspace`<br />
**A.** I think correct answer - it is depends... `DTE` is the old way,  `VisualStudioWorkspace` is the new way. Some operation will work under new way only, some under old way.

You can get all files over DTE only 
```C#
AsyncPackage package = <your VSPackage class instance>;  
DTE2 dte = package.GetService<DTE, DTE2>();  
var solutionDte = dte.Solution;  
var dteProjects = solutionDte.Projects;  
foreach (EnvDTE.Project dteProject in dteProjects)  
{
  foreach (ProjectItem dteItem in project.ProjectDte.ProjectItems
  {
      UseItem(dteItem);
      ReadProjectItems(dteItem.dteItem);
  }
}
```
Pay attention that `ProjectItem` could be phisical file or folder, depends from `dteItem.Kind`
```C#
Constants.vsProjectItemKindPhysicalFile
Constants.vsProjectItemKindPhysicalFolder
Constants.vsProjectItemKindSubProject
```

But as we need to use `SyntaxTree` for code parsing then we need to use new way over `VisualStudioWorkspace`. I call its - *using Roslyn files*.  
```C#
public static async Task<VisualStudioWorkspace> GetVisualStudioWorkspaceAsync(AsyncPackage package)
{
    IComponentModel componentModel = await package.GetServiceAsync<SComponentModel, IComponentModel>();

    var workspace = componentModel.GetService<VisualStudioWorkspace>();
    return workspace;
}
```

- How to find that solution is open?  
**A.** Use `dte.Solution?.IsOpen`

- How to find that solution is opened/closed?  
**A.** Add your own DTE event handler. Don't forget to fire fictive SolutionEventOpened when by starting your package solution already opened.
```C#
private SolutionEvents _solutionEvents; //don't use local variable
...
Events dteEvents = _dte.Events;
_solutionEvents = dteEvents.SolutionEvents;
_solutionEvents.Opened += SolutionEventOpened;
```

- Why `WorkspaceChanged` event from `VisualStudioWorkspace` fired by solution opening?  
**A.** Microsoft support tell me that is by design. There is some service `IVsOperationProgressStatusService` which could help to distinguish loading process but it look like that not all functionality is implemented now. UseDTE evetns, please.

- Why my Roslyn files sometimes does not have not the actual state?  
**A.** Check if you use document id and always get actual Roslyn documents from workspace. Never use pointer to the documents as Roslyn objects are immutable. After every change new document is created with increased internal version number.

##Document troubles

- How can I find a specific node in the C# file?  
**A.** You need to know Roslyn `project` and `documentId` first. Then get compilation from project and Roslyn document. From Roslyn document get Syntax tree and from syntax tree  get the root node.
After all into VisitNode function use `syntaxNode.IsKind(SyntaxKind.StringLiteralExpression)` for sample, to get the the string.
```C#  
IComponentModel componentModel = await package.GetServiceAsync<SComponentModel, IComponentModel>();
VisualStudioWorkspace visualStudioWorkspace = componentModel.GetService<VisualStudioWorkspace>()

Compilation? compilation = await project.GetCompilationAsync();
Document? roslynDocument = visualStudioWorkspace.CurrentSolution.GetDocument(documentId);
SyntaxTree? tree = await roslynDocument.GetSyntaxTreeAsync();
// Get semantic model if need
//SemanticModel? model = compilation.GetSemanticModel(tree);
SyntaxNode syntaxNode = await tree.GetRootAsync();
Visit(syntaxNode);

public void Visit(SyntaxNode syntaxNode)
{
    VisitNode(syntaxNode);

    foreach (SyntaxNode node in syntaxNode.ChildNodes())
    {
        Visit(node, visitors);
    }
}
```

>     
>Install [.NET Compiler Platform SDK](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.NETCompilerPlatformSDK) to get Syntax visualzer tool
>


- How can I select the text into a document?  
**A.** You need to open the document, get window frame, text buffer and do selection on the frame. Something like this (simplified):
```C#
IVsWindowFrame windowFrame = GetDocumentFrame(filePath);

object docData;
windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
IVsTextBuffer buffer = docData as IVsTextBuffer;
IVsTextManager mgr = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;
Guid logicalView = VSConstants.LOGVIEWID_Code;
mgr.NavigateToLineAndColumn(
    buffer,
    ref logicalView,
    startLinePosition.Line,
    startLinePosition.Character,
    endLinePosition.Line,
    endLinePosition.Character);


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
            out IServiceProvider _,
            out IVsUIHierarchy _,
            out uint _,
            out frame);
    }

    return frame;
}
```

  
