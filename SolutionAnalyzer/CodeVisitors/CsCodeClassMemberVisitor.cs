using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SolutionAnalyzer.Common;
using SolutionAnalyzer.CommonData;

namespace SolutionAnalyzer.CodeVisitors
{
    /// <summary>
    /// Class CsCodeClassMemberVisitor.
    /// Find all code class members for the first class into the file
    /// Implements the <see cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    /// </summary>
    /// <seealso cref="SolutionAnalyzer.Common.ISyntaxNodeVisitor" />
    internal class CsCodeClassMemberVisitor : ISyntaxNodeVisitor
    {
       
        /// <summary>
        /// The class member code items
        /// </summary>
        private readonly IList<ClassMemberDataItem> _classMemberDataItems = new List<ClassMemberDataItem>();

        /// <summary>
        /// The first parent class name
        /// </summary>
        private string _firstParentClassName = null;

        /// <summary>
        /// Determines whether is allowed to visit the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <returns><c>true</c> if is allowed to visit the specified syntax node; otherwise, <c>false</c>.</returns>
        public bool IsAllowedToVisit(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.MethodDeclaration)
                   || syntaxNode.IsKind(SyntaxKind.ConstructorDeclaration)
                   || syntaxNode.IsKind(SyntaxKind.PropertyDeclaration);
        }

        /// <summary>
        /// Visits the specified syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="syntaxNodeWalker">The syntax node walker.</param>
        public void Visit(SyntaxNode syntaxNode, SyntaxNodeWalker syntaxNodeWalker)
        {
            Location location = syntaxNode.GetLocation();
            FileLinePositionSpan fileLinePositionSpan = location.GetLineSpan();

            //check if members have the same parent
            SyntaxNode nodeParent = syntaxNode.Parent;
            if (nodeParent is ClassDeclarationSyntax parentClass)
            {
                string parentClassName = parentClass.Identifier.ValueText;
                if (_firstParentClassName == null)
                {
                    _firstParentClassName = parentClassName;
                }

                if (_firstParentClassName != parentClassName)
                {
                    return;
                }
            }

            ClassMemberDataItem classMemberDataItem = new ClassMemberDataItem(); //syntaxNodeWalker.FileDataItem
            SyntaxToken? nameToken = null;
            if (syntaxNode is MethodDeclarationSyntax methodNode)
            {
                nameToken = methodNode.Identifier;
                classMemberDataItem.CodeType = ClassMemberDataItem.ECodeType.Method;
            }

            if (syntaxNode is ConstructorDeclarationSyntax ctorNode)
            {
                nameToken = ctorNode.Identifier;
                classMemberDataItem.CodeType = ClassMemberDataItem.ECodeType.Constructor;
            }

            if (syntaxNode is PropertyDeclarationSyntax propNode)
            {
                nameToken = propNode.Identifier;
                classMemberDataItem.CodeType = ClassMemberDataItem.ECodeType.Property;
            }

            if (nameToken != null)
            {
                classMemberDataItem.Name = nameToken?.ValueText;
            }

            classMemberDataItem.StartLinePosition = fileLinePositionSpan.StartLinePosition;

            if (syntaxNode.HasLeadingTrivia)
            {
                SyntaxTriviaList syntaxTriviaList = syntaxNode.GetLeadingTrivia();
                int pos = 0;
                foreach (SyntaxTrivia syntaxTrivia in syntaxTriviaList)
                {
                    if (pos == 0)
                    {
                        Location commentFirstLocation = syntaxTrivia.GetLocation();
                        FileLinePositionSpan commentLinePosition = commentFirstLocation.GetLineSpan();
                        classMemberDataItem.StartLinePosition = commentLinePosition.StartLinePosition;
                        pos++;
                    }

                    //string fullString = syntaxTrivia.ToFullString();
                    //_comments.Add(fullString);
                }
            }

            classMemberDataItem.EndLinePosition = fileLinePositionSpan.EndLinePosition;
            if (syntaxNode.HasTrailingTrivia)
            {
                SyntaxTriviaList syntaxTriviaList = syntaxNode.GetTrailingTrivia();
                int pos = 0;
                foreach (SyntaxTrivia syntaxTrivia in syntaxTriviaList)
                {
                    if (pos == syntaxTriviaList.Count - 1)
                    {
                        Location commentLastLocation = syntaxTrivia.GetLocation();
                        FileLinePositionSpan commentLinePosition = commentLastLocation.GetLineSpan();

                        classMemberDataItem.EndLinePosition = commentLinePosition.EndLinePosition;
                    }

                    pos++;
                    //string fullString = syntaxTrivia.ToFullString();
                    //_commentsTrailing.Add(fullString);
                }
            }

            classMemberDataItem.Parent = syntaxNodeWalker.FileDataItem;
            _classMemberDataItems.Add(classMemberDataItem);
        }

       
        /// <summary>
        /// Gets the class member code items.
        /// </summary>
        /// <value>The class member code items.</value>
        public IList<ClassMemberDataItem> ClassMemberDataItems
        {
            get
            {
                return _classMemberDataItems;
            }
        }
    }
}
