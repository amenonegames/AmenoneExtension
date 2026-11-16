using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AllowReasonAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(ArchitectureGuardDiagnostics.InvalidAllowReason);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var allowAttributes = new[]
                {
                    context.Compilation.GetTypeByMetadataName(MetadataNames.AllowUnityTimeAttribute),
                    context.Compilation.GetTypeByMetadataName(MetadataNames.AllowUnityMessageAttribute),
                }
                .OfType<INamedTypeSymbol>()
                .ToArray();
            if (allowAttributes.Length == 0)
            {
                return;
            }

            var allowAttributeSet = allowAttributes.ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var writtenNames = AttributeTypeResolver.WrittenNamesOf(allowAttributes);
            context.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeAttribute(nodeContext, allowAttributeSet, writtenNames),
                SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(
            SyntaxNodeAnalysisContext context,
            ImmutableHashSet<INamedTypeSymbol> allowAttributes,
            ImmutableHashSet<string> writtenNames)
        {
            var attribute = (AttributeSyntax)context.Node;
            var attributeType = AttributeTypeResolver.Resolve(attribute, writtenNames, context.SemanticModel, context.CancellationToken);
            if (attributeType is null || !allowAttributes.Contains(attributeType))
            {
                return;
            }

            var reasonArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
            var problem = FindReasonProblem(context, reasonArgument);
            if (problem is null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureGuardDiagnostics.InvalidAllowReason,
                (reasonArgument ?? (SyntaxNode)attribute).GetLocation(),
                AttributeTypeResolver.ShortNameOf(attributeType),
                problem,
                AllowReason.MinimumLength));
        }

        private static string? FindReasonProblem(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax? reasonArgument)
        {
            if (reasonArgument is null)
            {
                return AllowReason.Missing;
            }

            var constant = context.SemanticModel.GetConstantValue(reasonArgument.Expression, context.CancellationToken);
            return constant is { HasValue: true, Value: string or null }
                ? AllowReason.FindProblem(constant.Value as string)
                : AllowReason.NotConstant;
        }
    }
}
