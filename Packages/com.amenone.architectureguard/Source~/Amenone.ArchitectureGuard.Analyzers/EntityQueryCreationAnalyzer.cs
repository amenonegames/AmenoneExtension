using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EntityQueryCreationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(ArchitectureGuardDiagnostics.EntityQueryCreationInUpdate);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var entityQuery = context.Compilation.GetTypeByMetadataName(MetadataNames.EntityQuery);
            var updateMethods = SystemUpdateMethods.Resolve(context.Compilation);
            if (entityQuery is null || updateMethods is null)
            {
                return;
            }

            var generatedQueryBuilder = context.Compilation.GetTypeByMetadataName(MetadataNames.SystemApiQueryBuilder);
            context.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, entityQuery, generatedQueryBuilder, updateMethods),
                OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(
            OperationAnalysisContext context,
            INamedTypeSymbol entityQuery,
            INamedTypeSymbol? generatedQueryBuilder,
            SystemUpdateMethods updateMethods)
        {
            var method = ((IInvocationOperation)context.Operation).TargetMethod;
            if (!CreatesQuery(method, entityQuery, generatedQueryBuilder) || !updateMethods.IsOnUpdate(context.ContainingSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureGuardDiagnostics.EntityQueryCreationInUpdate,
                context.Operation.Syntax.GetLocation(),
                context.ContainingSymbol.ContainingType.Name,
                $"{method.ContainingType.Name}.{method.Name}"));
        }

        private static bool CreatesQuery(IMethodSymbol method, INamedTypeSymbol entityQuery, INamedTypeSymbol? generatedQueryBuilder) =>
            SymbolEqualityComparer.Default.Equals(method.ReturnType, entityQuery)
            && SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, entityQuery.ContainingAssembly)
            && !SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, generatedQueryBuilder);
    }
}
