using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PublishingSystemAnalyzer : DiagnosticAnalyzer
    {
        private const string ClassGuidance = "PublishSystem<TPublisher> を継いでください。";
        private const string StructGuidance = "ISystem の struct は基底クラスを継げないので、発行は PublishSystem<TPublisher> を継ぐクラスの System で行ってください。";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(ArchitectureGuardDiagnostics.PublishingSystemOutsidePublishSystem);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            if (TargetAssembly.IsTest(context.Compilation))
            {
                return;
            }

            var types = PublishingSystemTypes.Resolve(context.Compilation);
            if (types is null)
            {
                return;
            }

            context.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, types), SymbolKind.NamedType);
        }

        private static void AnalyzeType(SymbolAnalysisContext context, PublishingSystemTypes types)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (!types.IsSystem(type) || types.InheritsPublishSystem(type))
            {
                return;
            }

            var publishers = types.PublishersReceivedBy(type);
            if (publishers.IsEmpty)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureGuardDiagnostics.PublishingSystemOutsidePublishSystem,
                HandWrittenLocationOf(type, context),
                type.Name,
                DescribePublishers(publishers),
                type.TypeKind == TypeKind.Struct ? StructGuidance : ClassGuidance));
        }

        // 生成コードの位置に出した診断は捨てられるので、partial の宣言のうち手で書いた物の位置を選ぶ。
        private static Location HandWrittenLocationOf(INamedTypeSymbol type, SymbolAnalysisContext context) =>
            type.Locations.FirstOrDefault(location =>
                location.IsInSource && !GeneratedCodeFiles.IsGenerated(location.SourceTree!, context.Options, context.CancellationToken))
            ?? type.Locations[0];

        private static string DescribePublishers(ImmutableArray<ITypeSymbol> publishers) =>
            string.Join("、", publishers.Select(publisher => publisher.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
