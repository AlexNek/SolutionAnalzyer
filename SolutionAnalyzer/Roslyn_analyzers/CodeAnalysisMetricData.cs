using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace SolutionAnalyzer.Roslyn_analyzers
{
    public abstract partial class CodeAnalysisMetricData
    {
        internal CodeAnalysisMetricData(
            ISymbol symbol,
            int maintainabilityIndex,
            ComputationalComplexityMetrics computationalComplexityMetrics,
            ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
            long linesOfCode,
            int cyclomaticComplexity,
            int? depthOfInheritance,
            ImmutableArray<CodeAnalysisMetricData> children)
        {
            Debug.Assert(
                symbol.Kind is SymbolKind.Assembly or
                    SymbolKind.Namespace or
                    SymbolKind.NamedType or
                    SymbolKind.Method or
                    SymbolKind.Field or
                    SymbolKind.Event or
                    SymbolKind.Property);
            Debug.Assert(
                depthOfInheritance.HasValue == (symbol.Kind == SymbolKind.Assembly || symbol.Kind == SymbolKind.Namespace
                                                                                   || symbol.Kind == SymbolKind.NamedType));

            var executableLines = !computationalComplexityMetrics.IsDefault
                                      ? computationalComplexityMetrics.ExecutableLines
                                      : children.Sum(c => c.ExecutableLines);

            Symbol = symbol;
            MaintainabilityIndex = maintainabilityIndex;
            ComputationalComplexityMetrics = computationalComplexityMetrics;
            CoupledNamedTypes = coupledNamedTypes;
            SourceLines = linesOfCode;
            ExecutableLines = executableLines;
            CyclomaticComplexity = cyclomaticComplexity;
            DepthOfInheritance = depthOfInheritance;
            Children = children;
        }

        /// <summary>
        /// Computes <see cref="CodeAnalysisMetricData"/> for the given <paramref name="compilation"/>.
        /// </summary>
        [Obsolete("Use ComputeAsync(CodeMetricsAnalysisContext) instead.")]
        public static Task<CodeAnalysisMetricData> ComputeAsync(Compilation compilation, CancellationToken cancellationToken)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return ComputeAsync(compilation.Assembly, new CodeMetricsAnalysisContext(compilation, cancellationToken));
        }

        /// <summary>
        /// Computes <see cref="CodeAnalysisMetricData"/> for the given <paramref name="context"/>.
        /// </summary>
        public static Task<CodeAnalysisMetricData> ComputeAsync(CodeMetricsAnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return ComputeAsync(context.Compilation.Assembly, context);
        }

        /// <summary>
        /// Computes <see cref="CodeAnalysisMetricData"/> for the given <paramref name="symbol"/> from the given <paramref name="compilation"/>.
        /// </summary>
        [Obsolete("Use ComputeAsync(ISymbol, CodeMetricsAnalysisContext) instead.")]
        public static Task<CodeAnalysisMetricData> ComputeAsync(ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return ComputeAsync(symbol, new CodeMetricsAnalysisContext(compilation, cancellationToken));
        }

        /// <summary>
        /// Computes <see cref="CodeAnalysisMetricData"/> for the given <paramref name="symbol"/> from the given <paramref name="context"/>.
        /// </summary>
        public static async Task<CodeAnalysisMetricData> ComputeAsync(ISymbol symbol, CodeMetricsAnalysisContext context)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    return await AssemblyMetricData.ComputeAsync((IAssemblySymbol)symbol, context).ConfigureAwait(false);
                case SymbolKind.Namespace:
                    return await NamespaceMetricData.ComputeAsync((INamespaceSymbol)symbol, context).ConfigureAwait(false);
                case SymbolKind.NamedType:
                    return await NamedTypeMetricData.ComputeAsync((INamedTypeSymbol)symbol, context).ConfigureAwait(false);
                case SymbolKind.Method:
                    return await MethodMetricData.ComputeAsync((IMethodSymbol)symbol, context).ConfigureAwait(false);
                case SymbolKind.Property:
                    return await PropertyMetricData.ComputeAsync((IPropertySymbol)symbol, context).ConfigureAwait(false);
                case SymbolKind.Field:
                    return await FieldMetricData.ComputeAsync((IFieldSymbol)symbol, context).ConfigureAwait(false);
                case SymbolKind.Event:
                    return await EventMetricData.ComputeAsync((IEventSymbol)symbol, context).ConfigureAwait(false);
                default:
                    throw new NotSupportedException();
            }

            //static async Task<CodeAnalysisMetricData> ComputeAsync(ISymbol symbol, CodeMetricsAnalysisContext context)
            //{
            //    switch (symbol.Kind)
            //    {
            //        case SymbolKind.Assembly:
            //            return await AssemblyMetricData.ComputeAsync((IAssemblySymbol)symbol, context).ConfigureAwait(false);
            //        case SymbolKind.Namespace:
            //            return await NamespaceMetricData.ComputeAsync((INamespaceSymbol)symbol, context).ConfigureAwait(false);
            //        case SymbolKind.NamedType:
            //            return await NamedTypeMetricData.ComputeAsync((INamedTypeSymbol)symbol, context).ConfigureAwait(false);
            //        case SymbolKind.Method:
            //            return await MethodMetricData.ComputeAsync((IMethodSymbol)symbol, context).ConfigureAwait(false);
            //        case SymbolKind.Property:
            //            return await PropertyMetricData.ComputeAsync((IPropertySymbol)symbol, context).ConfigureAwait(false);
            //        case SymbolKind.Field:
            //            return await FieldMetricData.ComputeAsync((IFieldSymbol)symbol, context).ConfigureAwait(false);
            //        case SymbolKind.Event:
            //            return await EventMetricData.ComputeAsync((IEventSymbol)symbol, context).ConfigureAwait(false);
            //        default:
            //            throw new NotSupportedException();
            //    }
            //}
        }

        /// <summary>
        /// Computes string representation of metrics data.
        /// </summary>
        public sealed override string ToString()
        {
            var builder = new StringBuilder();
            string symbolName;
            switch (Symbol.Kind)
            {
                case SymbolKind.Assembly:
                    symbolName = "Assembly";
                    break;

                case SymbolKind.Namespace:
                    // Skip explicit display for global namespace.
                    if (((INamespaceSymbol)Symbol).IsGlobalNamespace)
                    {
                        appendChildren(indent: string.Empty);
                        return builder.ToString();
                    }

                    symbolName = Symbol.Name;
                    break;

                case SymbolKind.NamedType:
                    symbolName = Symbol.ToDisplayString();
                    var index = symbolName.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0 && index < symbolName.Length)
                    {
                        //symbolName = symbolName[(index + 1)..];
                        symbolName = symbolName.Substring(index + 1);
                    }

                    break;

                default:
                    symbolName = Symbol.ToDisplayString();
                    break;
            }

            builder.Append(
                $"{symbolName}: (Lines: {SourceLines}, ExecutableLines: {ExecutableLines}, MntIndex: {MaintainabilityIndex}, CycCxty: {CyclomaticComplexity}");
            if (!CoupledNamedTypes.IsEmpty)
            {
                var coupledNamedTypesStr = string.Join(", ", CoupledNamedTypes.Select(t => t.ToDisplayString()).OrderBy(n => n));
                builder.Append($", CoupledTypes: {{{coupledNamedTypesStr}}}");
            }

            if (DepthOfInheritance.HasValue)
            {
                builder.Append($", DepthInherit: {DepthOfInheritance}");
            }

            builder.Append(")");
            appendChildren(indent: "   ");
            return builder.ToString();

            void appendChildren(string indent)
            {
                foreach (var child in Children)
                {
                    foreach (var line in child.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        builder.AppendLine();
                        builder.Append($"{indent}{line}");
                    }
                }
            }
        }

        internal static async Task<ImmutableArray<CodeAnalysisMetricData>> ComputeAsync(
            IEnumerable<ISymbol> children,
            CodeMetricsAnalysisContext context) =>
            (await Task.WhenAll(
                 from child in children
#if !LEGACY_CODE_METRICS_MODE // Skip implicitly declared symbols, such as default constructor, for non-legacy mode.
                 where !child.IsImplicitlyDeclared || (child as INamespaceSymbol)?.IsGlobalNamespace == true
#endif
                 select Task.Run(() => ComputeAsync(child, context))).ConfigureAwait(false)).ToImmutableArray();

        /// <summary>
        /// Array of code metrics data for symbolic children of <see cref="Symbol"/>, if any.
        /// </summary>
        public ImmutableArray<CodeAnalysisMetricData> Children { get; }

        /// <summary>
        /// Indicates the coupling to unique named types through parameters, local variables, return types, method calls,
        /// generic or template instantiations, base classes, interface implementations, fields defined on external types, and attribute decoration.
        /// Good software design dictates that types and methods should have high cohesion and low coupling.
        /// High coupling indicates a design that is difficult to reuse and maintain because of its many interdependencies on other types.
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CoupledNamedTypes { get; }

        /// <summary>
        /// Measures the structural complexity of the code.
        /// It is created by calculating the number of different code paths in the flow of the program.
        /// A program that has complex control flow requires more tests to achieve good code coverage and is less maintainable.
        /// </summary>
        public int CyclomaticComplexity { get; }

        /// <summary>
        /// Indicates the number of different classes that inherit from one another, all the way back to the base class.
        /// Depth of Inheritance is similar to class coupling in that a change in a base class can affect any of its inherited classes.
        /// The higher this number, the deeper the inheritance and the higher the potential for base class modifications to result in a breaking change.
        /// For Depth of Inheritance, a low value is good and a high value is bad.
        /// </summary>
        public int? DepthOfInheritance { get; }

        /// <summary>
        /// Indicates the approximate number of executable statements/lines in code.
        /// The count is based on the executable <see cref="IOperation"/>s in code and is therefore not the exact number of lines in the source code file.
        /// A high count might indicate that a type or method is trying to do too much work and should be split up.
        /// It might also indicate that the type or method might be hard to maintain.
        /// </summary>
        public long ExecutableLines { get; }

        /// <summary>
        /// Indicates an index value between 0 and 100 that represents the relative ease of maintaining the code.
        /// A high value means better maintainability.
        /// </summary>
        public int MaintainabilityIndex { get; }

        /// <summary>
        /// Indicates the exact number of lines in source code file.
        /// </summary>
        public long SourceLines { get; }

        /// <summary>
        /// Symbol corresponding to the metric data.
        /// </summary>
        public ISymbol Symbol { get; }

        internal ComputationalComplexityMetrics ComputationalComplexityMetrics { get; }
    }
}
