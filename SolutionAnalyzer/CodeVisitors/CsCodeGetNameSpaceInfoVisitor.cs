using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SolutionAnalyzer.Common;

namespace SolutionAnalyzer.CodeVisitors
{
    /// <summary>
    /// Class CsCodeGetNameSpaceInfoVisitor.
    /// Find the namespace name
    /// Implements the <see cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    /// </summary>
    /// <seealso cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    internal class CsCodeGetNameSpaceInfoVisitor : ISyntaxNodeVisitor
    {
        /// <summary>
        /// Determines whether is allowed to visit the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <returns><c>true</c> if is allowed to visit the specified syntax node; otherwise, <c>false</c>.</returns>
        public bool IsAllowedToVisit(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.NamespaceDeclaration);
        }

        /// <summary>
        /// Visits the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="syntaxNodeWalker">The syntax node walker.</param>
        public void Visit(SyntaxNode syntaxNode, SyntaxNodeWalker syntaxNodeWalker)
        {
            NamespaceDeclarationSyntax namespaceNode = syntaxNode as NamespaceDeclarationSyntax;
            Name = VisitorHelper.GetNodeName(namespaceNode.Name);
        }

        /// <summary>
        /// Gets the namespace name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }
    }
}
