﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Analyzer.Utilities;

using Microsoft.CodeAnalysis;

namespace SolutionAnalyzer.Roslyn_analyzers
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class FieldMetricData : CodeAnalysisMetricData
        {
            internal FieldMetricData(
                IFieldSymbol symbol,
                int maintainabilityIndex,
                ComputationalComplexityMetrics computationalComplexityMetrics,
                ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
                long linesOfCode,
                int cyclomaticComplexity,
                int? depthOfInheritance)
                : base(symbol, maintainabilityIndex, computationalComplexityMetrics, coupledNamedTypes,
                      linesOfCode, cyclomaticComplexity, depthOfInheritance, children: ImmutableArray<CodeAnalysisMetricData>.Empty)
            {
            }

            internal static async Task<FieldMetricData> ComputeAsync(IFieldSymbol field, CodeMetricsAnalysisContext context)
            {
                var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);

                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = field.DeclaringSyntaxReferences;
                long linesOfCode = await MetricsHelper.GetLinesOfCodeAsync(declarations, field, context).ConfigureAwait(false);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    await MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(declarations, field, coupledTypesBuilder, context).ConfigureAwait(false);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, wellKnownTypeProvider, field.Type);
                int? depthOfInheritance = null;
                int maintainabilityIndex = CalculateMaintainabilityIndex(computationalComplexityMetrics, cyclomaticComplexity);
                MetricsHelper.RemoveContainingTypes(field, coupledTypesBuilder);

                return new FieldMetricData(field, maintainabilityIndex, computationalComplexityMetrics,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance);
            }

            private static int CalculateMaintainabilityIndex(ComputationalComplexityMetrics computationalComplexityMetrics, int cyclomaticComplexity)
            {
                double computationalComplexityVolume = Math.Max(0.0, Math.Log(computationalComplexityMetrics.Volume));   //avoid Log(0) = -Infinity
                double logEffectiveLinesOfCode = Math.Max(0.0, Math.Log(computationalComplexityMetrics.EffectiveLinesOfCode));          //avoid Log(0) = -Infinity
                return MetricsHelper.NormalizeAndRoundMaintainabilityIndex(171 - 5.2 * computationalComplexityVolume - 0.23 * cyclomaticComplexity - 16.2 * logEffectiveLinesOfCode);
            }
        }
    }
}
