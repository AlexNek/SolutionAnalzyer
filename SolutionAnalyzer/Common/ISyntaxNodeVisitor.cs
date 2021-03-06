using Microsoft.CodeAnalysis;

namespace SolutionAnalyzer.Common
{
    /// <summary>
    /// Interface ISyntaxNodeVisitor
    /// </summary>
    internal interface ISyntaxNodeVisitor
    {
        /// <summary>
        /// Determines whether is allowed to visit the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <returns><c>true</c> if is allowed to visit the specified syntax node; otherwise, <c>false</c>.</returns>
        bool IsAllowedToVisit(SyntaxNode syntaxNode);

        /// <summary>
        /// Visits the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="syntaxNodeWalker">The syntax node walker.</param>
        void Visit(SyntaxNode syntaxNode, SyntaxNodeWalker syntaxNodeWalker);
    }
}
