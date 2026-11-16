using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnityTimeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            ArchitectureGuardDiagnostics.ScaledTimeUsage,
            ArchitectureGuardDiagnostics.UnscaledTimeUsage);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var timeType = context.Compilation.GetTypeByMetadataName(MetadataNames.UnityTime);
            if (timeType is null)
            {
                return;
            }

            var classifier = new UnityTimeAccessClassifier(timeType);
            var allowAttribute = context.Compilation.GetTypeByMetadataName(MetadataNames.AllowUnityTimeAttribute);
            context.RegisterOperationAction(
                operationContext => AnalyzePropertyReference(operationContext, classifier, allowAttribute),
                OperationKind.PropertyReference);
        }

        private static void AnalyzePropertyReference(
            OperationAnalysisContext context,
            UnityTimeAccessClassifier classifier,
            INamedTypeSymbol? allowAttribute)
        {
            var reference = (IPropertyReferenceOperation)context.Operation;
            var usage = classifier.Classify(reference);
            if (usage == UnityTimeUsage.Unguarded || IsInsideNameOf(reference) || IsAllowed(context, reference, allowAttribute))
            {
                return;
            }

            var location = reference.Syntax.GetLocation();
            var accessName = $"UnityEngine.Time.{reference.Property.Name}";
            context.ReportDiagnostic(usage switch
            {
                UnityTimeUsage.UnscaledTimeRead =>
                    Diagnostic.Create(ArchitectureGuardDiagnostics.UnscaledTimeUsage, location, accessName),
                UnityTimeUsage.SettingWrite =>
                    Diagnostic.Create(
                        ArchitectureGuardDiagnostics.ScaledTimeUsage,
                        location,
                        $"{accessName} への代入で、全体の時間の設定を書き換えています",
                        ArchitectureGuardDiagnostics.TimeSettingWriteGuidance),
                _ =>
                    Diagnostic.Create(
                        ArchitectureGuardDiagnostics.ScaledTimeUsage,
                        location,
                        $"{accessName} を直接読んでいます。これは timeScale の影響を受ける時間です",
                        ArchitectureGuardDiagnostics.ScaledTimeReadGuidance),
            });
        }

        private static bool IsInsideNameOf(IOperation operation)
        {
            for (var ancestor = operation.Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (ancestor is INameOfOperation)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAllowed(
            OperationAnalysisContext context,
            IPropertyReferenceOperation reference,
            INamedTypeSymbol? allowAttribute)
        {
            var innermostSymbol = reference.SemanticModel?.GetEnclosingSymbol(reference.Syntax.SpanStart, context.CancellationToken)
                ?? context.ContainingSymbol;
            return AllowAttributeScope.Covers(innermostSymbol, allowAttribute);
        }
    }
}
