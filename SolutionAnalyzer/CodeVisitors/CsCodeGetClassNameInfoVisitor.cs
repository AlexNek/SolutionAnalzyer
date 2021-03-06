using System;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SolutionAnalyzer.Common;

namespace SolutionAnalyzer.CodeVisitors
{
    /// <summary>
    /// Class CsCodeGetClassNameInfoVisitor.
    /// Find the first class into the file and collect class modifiers and name 
    /// Implements the <see cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    /// </summary>
    /// <seealso cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    internal class CsCodeGetClassNameInfoVisitor : ISyntaxNodeVisitor
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
                    StringBuilder sb = new StringBuilder();
                    foreach (SyntaxToken modifier in classNode.Modifiers)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(" ");
                        }

                        sb.Append(modifier.ValueText);
                    }

                    ClassName = classNode.Identifier.ValueText;
                    ClassModifiers = sb.ToString();

                    //line with 'class' 
                    SyntaxToken nodeKeyword = classNode.Keyword;
                    FileLinePositionSpan lineSpan = nodeKeyword.GetLocation().GetMappedLineSpan();
                    StartPosition = lineSpan.StartLinePosition;
                }

                _classCount++;
            }
        }

        /// <summary>
        /// Gets the class modifiers. private, public, static, partial and so on
        /// </summary>
        /// <value>The class modifiers.</value>
        public string ClassModifiers { get; private set; }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public string ClassName { get; private set; }

        /// <summary>
        /// Gets the class start position.
        /// </summary>
        /// <value>The start position.</value>
        public LinePosition StartPosition { get; private set; }
    }
}
