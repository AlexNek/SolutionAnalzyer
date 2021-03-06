using System;
using System.Collections.Concurrent;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace SolutionAnalyzer
{
    public sealed class CodeMetricsAnalysisContext
    {
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelMap;

        public CodeMetricsAnalysisContext(
            Compilation compilation,
            CancellationToken cancellationToken,
            Func<INamedTypeSymbol, bool>? isExcludedFromInheritanceCountFunc = null)
        {
            Compilation = compilation;
            CancellationToken = cancellationToken;
            _semanticModelMap = new ConcurrentDictionary<SyntaxTree, SemanticModel>();
            IsExcludedFromInheritanceCountFunc = isExcludedFromInheritanceCountFunc ?? (x => false); // never excluded by default
        }

        internal SemanticModel GetSemanticModel(SyntaxNode node)
        {
            return _semanticModelMap.GetOrAdd(node.SyntaxTree, tree => Compilation.GetSemanticModel(node.SyntaxTree));
        }

        public CancellationToken CancellationToken { get; }

        public Compilation Compilation { get; }

        public Func<INamedTypeSymbol, bool> IsExcludedFromInheritanceCountFunc { get; }
    }
}
