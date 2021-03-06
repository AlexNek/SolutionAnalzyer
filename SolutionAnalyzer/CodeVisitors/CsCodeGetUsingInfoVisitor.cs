using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SolutionAnalyzer.Common;

namespace SolutionAnalyzer.CodeVisitors
{
    /// <summary>
    /// Class CsCodeGetUsingInfoVisitor.
    /// Collect using list and any text before usings
    /// Implements the <see cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    /// </summary>
    /// <seealso cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    internal class CsCodeGetUsingInfoVisitor : ISyntaxNodeVisitor
    {
        /// <summary>
        /// The trivias. Any text before using
        /// </summary>
        private readonly List<string> _trivias = new List<string>();

        /// <summary>
        /// The using list
        /// </summary>
        private readonly List<string> _usings = new List<string>();

        /// <summary>
        /// Determines whether is allowed to visit the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <returns><c>true</c> if is allowed to visit the specified syntax node; otherwise, <c>false</c>.</returns>
        public bool IsAllowedToVisit(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.UsingDirective);
        }

        /// <summary>
        /// Visits the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="syntaxNodeWalker">The syntax node walker.</param>
        public void Visit(SyntaxNode syntaxNode, SyntaxNodeWalker syntaxNodeWalker)
        {
            UsingDirectiveSyntax usingNode = syntaxNode as UsingDirectiveSyntax;
            if (usingNode != null && usingNode.HasLeadingTrivia)
            {
                SyntaxTriviaList leadingTrivia = usingNode.GetLeadingTrivia();
                foreach (SyntaxTrivia syntaxTrivia in leadingTrivia)
                {
                    //DefineDirectiveTriviaSyntax defineDirective = syntaxTrivia.Token as DefineDirectiveTriviaSyntax;
                    string fullString = syntaxTrivia.ToFullString();
                    _trivias.Add(fullString);
                }
            }

            NameSyntax nodeName = usingNode.Name;
            string name = VisitorHelper.GetNodeName(nodeName);
            _usings.Add(name);
        }

        /// <summary>
        /// Gets the trivias. Text before using
        /// </summary>
        /// <value>The trivias.</value>
        public List<string> Trivias
        {
            get
            {
                return _trivias;
            }
        }

        /// <summary>
        /// Gets the using list.
        /// </summary>
        /// <value>The usings.</value>
        public IEnumerable<string> Usings
        {
            get
            {
                return _usings;
            }
        }
    }
}
