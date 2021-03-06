#nullable enable
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

using SolutionAnalyzer.CommonData;

namespace SolutionAnalyzer.Common
{
    /// <summary>
    /// Class SyntaxNodeWalker.
    /// Helper class for visiting SyntaxNodes
    /// </summary>
    internal class SyntaxNodeWalker
    {
        /// <summary>
        /// The file data item
        /// </summary>
        private readonly FileDataItem? _fileDataItem;

        /// <summary>
        /// The package
        /// </summary>
        private readonly AsyncPackage _package;

       /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxNodeWalker"/> class.
        /// </summary>
        /// <param name="package">The VcPackage package.</param>
        /// <param name="fileDataItem">The selected file data item.</param>
        public SyntaxNodeWalker(AsyncPackage package, FileDataItem? fileDataItem = null)
        {
            _fileDataItem = fileDataItem;
            _package = package;
        }

        /// <summary>
        /// Visits the specified syntax node tree with more that one visitor.
        /// Every node will be given to every visitor
        /// Single traverse mode.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="visitors">The visitors.</param>
        public void Visit(SyntaxNode syntaxNode, IList<ISyntaxNodeVisitor> visitors)
        {
            foreach (ISyntaxNodeVisitor visitor in visitors)
            {
                if (visitor.IsAllowedToVisit(syntaxNode))
                {
                    visitor.Visit(syntaxNode, this);
                }
            }

            IEnumerable<SyntaxNode> syntaxNodes = syntaxNode.ChildNodes();
            foreach (SyntaxNode node in syntaxNodes)
            {
                Visit(node, visitors);
            }
        }

        /// <summary>
        /// Visits the specified syntax node tree with a single visitor.
        /// For more then 2 visitors use override function to reduce traversing time
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="visitor">The visitor.</param>
        public void Visit(SyntaxNode syntaxNode, ISyntaxNodeVisitor visitor)
        {
            if (visitor.IsAllowedToVisit(syntaxNode))
            {
                visitor.Visit(syntaxNode, this);
            }

            IEnumerable<SyntaxNode> syntaxNodes = syntaxNode.ChildNodes();
            foreach (SyntaxNode node in syntaxNodes)
            {
                Visit(node, visitor);
            }
        }

        /// <summary>
        /// Gets the file data item.
        /// </summary>
        /// <value>The file data item.</value>
        public FileDataItem? FileDataItem
        {
            get
            {
                return _fileDataItem;
            }
        }

        /// <summary>
        /// Gets the package.
        /// </summary>
        /// <value>The package.</value>
        public AsyncPackage Package
        {
            get
            {
                return _package;
            }
        }
        
    }
}
