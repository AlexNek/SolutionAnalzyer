using System;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SolutionAnalyzer.CodeVisitors
{
    /// <summary>
    /// Class VisitorHelper. Helper functions for tree visitors
    /// </summary>
    internal static class VisitorHelper
    {
        /// <summary>
        /// Gets the name of the syntax node.
        /// </summary>
        /// <param name="nodeName">Name of the node.</param>
        /// <returns>System.String.</returns>
        public static string GetNodeName(NameSyntax nodeName)
        {
            string text = String.Empty;
            if (nodeName is IdentifierNameSyntax nameSyntax)
            {
                text = nameSyntax.Identifier.Text;
            }

            if (nodeName is QualifiedNameSyntax qualifiedNameSyntax)
            {
                string name1 = GetNodeName(qualifiedNameSyntax.Left);
                string name2 = GetNodeName(qualifiedNameSyntax.Right);
                text = name1 + "." + name2;
            }

            return text;
        }
    }
}
