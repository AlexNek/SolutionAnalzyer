using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SolutionAnalyzer.Common;

namespace SolutionAnalyzer.CodeVisitors
{
    /// <summary>
    /// Class CsCodeIsClassPartialVisitor.
    /// Check if the first class into file has the partial modifiers.
    /// Implements the <see cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    /// </summary>
    /// <seealso cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    internal class CsCodeIsClassPartialVisitor : ISyntaxNodeVisitor
    {
        /// <summary>
        /// The class count. As we need to use first class only
        /// </summary>
        private int _classCount;

        /// <summary>
        /// Determines whether is allowed to visit the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <returns><c>true</c> if is allowed to visit the specified syntax node; otherwise, <c>false</c>.</returns>
        public bool IsAllowedToVisit(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.ClassDeclaration);
        }

        /// <summary>
        /// Visits the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="syntaxNodeWalker">The syntax node walker.</param>
        public void Visit(SyntaxNode syntaxNode, SyntaxNodeWalker syntaxNodeWalker)
        {
            ClassDeclarationSyntax classNode = syntaxNode as ClassDeclarationSyntax;
            Type parentType = classNode?.Parent?.GetType();
            if (parentType == typeof(CompilationUnitSyntax) || parentType == typeof(NamespaceDeclarationSyntax))
            {
                if (_classCount == 0 && classNode != null)
                {
                    foreach (SyntaxToken modifier in classNode.Modifiers)
                    {
                        IsClassPartial = modifier.ValueText == "partial";
                    }
                }

                _classCount++;
            }
        }

        /// <summary>
        /// Gets a value indicating whether analyzed class has the partial modifiers.
        /// </summary>
        /// <value><c>true</c> if this instance is class partial; otherwise, <c>false</c>.</value>
        public bool IsClassPartial { get; private set; }
    }
}
