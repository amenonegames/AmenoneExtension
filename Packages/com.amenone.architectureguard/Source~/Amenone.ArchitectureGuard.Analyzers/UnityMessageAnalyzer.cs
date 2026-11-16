using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnityMessageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> GuardedMessageNames = ImmutableHashSet.Create(
            "Awake",
            "Start",
            "OnDestroy",
            "Update",
            "LateUpdate",
            "FixedUpdate");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(ArchitectureGuardDiagnostics.UnityMessageDeclaration);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var monoBehaviour = context.Compilation.GetTypeByMetadataName(MetadataNames.UnityMonoBehaviour);
            if (monoBehaviour is null)
            {
                return;
            }

            var allowAttribute = context.Compilation.GetTypeByMetadataName(MetadataNames.AllowUnityMessageAttribute);
            context.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, monoBehaviour, allowAttribute),
                SymbolKind.Method);
        }

        private static void AnalyzeMethod(
            SymbolAnalysisContext context,
            INamedTypeSymbol monoBehaviour,
            INamedTypeSymbol? allowAttribute)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (!IsGuardedMessage(method)
                || !DerivesFrom(method.ContainingType, monoBehaviour)
                || (allowAttribute is not null && AllowAttributeScope.HasAttribute(method, allowAttribute)))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureGuardDiagnostics.UnityMessageDeclaration,
                method.Locations[0],
                method.ContainingType.Name,
                method.Name));
        }

        private static bool IsGuardedMessage(IMethodSymbol method) =>
            method.MethodKind == MethodKind.Ordinary
            && !method.IsOverride
            && !method.IsStatic
            && !method.IsGenericMethod
            && method.Parameters.IsEmpty
            && GuardedMessageNames.Contains(method.Name);

        private static bool DerivesFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
        {
            for (var current = type?.BaseType; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
