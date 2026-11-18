using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            var subscribers = MetadataNames.MessagePipeSubscribers
                .Select(context.Compilation.GetTypeByMetadataName)
                .OfType<INamedTypeSymbol>()
                .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            if (subscribers.IsEmpty)
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
            ImmutableHashSet<INamedTypeSymbol> subscribers,
            string assemblyName)
        {
            foreach (var (declaration, type) in ReceivedTypesOf(context.Symbol))
            {
                if (IsSubscriber(type, subscribers))
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

        private static ImmutableArray<(ISymbol Declaration, ITypeSymbol Type)> ReceivedTypesOf(ISymbol symbol) => symbol switch
        {
            IFieldSymbol { IsImplicitlyDeclared: false } field => ImmutableArray.Create<(ISymbol, ITypeSymbol)>((field, field.Type)),
            IPropertySymbol { IsImplicitlyDeclared: false } property when !IsPrimaryConstructorProperty(property) =>
                ImmutableArray.Create<(ISymbol, ITypeSymbol)>((property, property.Type)),
            IMethodSymbol method when ReceivesThroughParameters(method) =>
                method.Parameters.Select(parameter => ((ISymbol)parameter, parameter.Type)).ToImmutableArray(),
            _ => ImmutableArray<(ISymbol, ITypeSymbol)>.Empty,
        };

        private static bool ReceivesThroughParameters(IMethodSymbol method) =>
            !method.IsImplicitlyDeclared
            && (method.MethodKind is MethodKind.Ordinary
                or MethodKind.Constructor
                or MethodKind.ExplicitInterfaceImplementation
                or MethodKind.UserDefinedOperator
                or MethodKind.Conversion);

        // record の主コンストラクタの引数から合成されるプロパティは、引数の側で 1 度だけ出す。
        private static bool IsPrimaryConstructorProperty(IPropertySymbol property) =>
            property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ParameterSyntax);

        private static bool IsSubscriber(ITypeSymbol type, ImmutableHashSet<INamedTypeSymbol> subscribers) =>
            type is INamedTypeSymbol named
            && (subscribers.Contains(named.OriginalDefinition)
                || named.AllInterfaces.Any(implemented => subscribers.Contains(implemented.OriginalDefinition)));

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
