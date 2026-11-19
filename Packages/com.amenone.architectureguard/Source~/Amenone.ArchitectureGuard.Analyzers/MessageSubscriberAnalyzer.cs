using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MessageSubscriberAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(ArchitectureGuardDiagnostics.MessageSubscriberOutsidePresenter);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            if (TargetAssembly.IsPresenter(context.Compilation) || TargetAssembly.IsTest(context.Compilation))
            {
                return;
            }

            var subscribers = MessagePipeInterfaces.Resolve(context.Compilation, MetadataNames.MessagePipeSubscribers);
            if (subscribers is null)
            {
                return;
            }

            var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
            context.RegisterSymbolAction(
                symbolContext => AnalyzeMember(symbolContext, subscribers, assemblyName),
                SymbolKind.Field,
                SymbolKind.Property,
                SymbolKind.Method);
        }

        private static void AnalyzeMember(
            SymbolAnalysisContext context,
            MessagePipeInterfaces subscribers,
            string assemblyName)
        {
            foreach (var (declaration, type) in ReceivedTypes.Of(context.Symbol))
            {
                if (subscribers.Covers(type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ArchitectureGuardDiagnostics.MessageSubscriberOutsidePresenter,
                        declaration.Locations[0],
                        assemblyName,
                        DescribeDeclaration(declaration),
                        type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                }
            }
        }

        private static string DescribeDeclaration(ISymbol declaration) => declaration switch
        {
            IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } constructor } parameter =>
                $"{constructor.ContainingType.Name} のコンストラクタの引数 {parameter.Name}",
            IParameterSymbol { ContainingSymbol: IMethodSymbol method } parameter =>
                $"{method.ContainingType.Name}.{method.Name} の引数 {parameter.Name}",
            IFieldSymbol field => $"フィールド {field.ContainingType.Name}.{field.Name}",
            _ => $"プロパティ {declaration.ContainingType.Name}.{declaration.Name}",
        };
    }
}
