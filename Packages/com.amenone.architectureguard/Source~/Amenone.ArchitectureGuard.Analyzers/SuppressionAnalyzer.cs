using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Amenone.ArchitectureGuard.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SuppressionAnalyzer : DiagnosticAnalyzer
    {
        private const string CheckIdParameterName = "checkId";
        private const int CheckIdParameterPosition = 1;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(ArchitectureGuardDiagnostics.DiagnosticSuppression);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(AnalyzePragmas);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void AnalyzePragmas(SyntaxTreeAnalysisContext context)
        {
            var root = (CSharpSyntaxNode)context.Tree.GetRoot(context.CancellationToken);
            for (var directive = root.GetFirstDirective(IsDisablingPragma);
                 directive is not null;
                 directive = directive.GetNextDirective(IsDisablingPragma))
            {
                ReportPragma(context, (PragmaWarningDirectiveTriviaSyntax)directive);
            }
        }

        private static bool IsDisablingPragma(DirectiveTriviaSyntax directive) =>
            directive is PragmaWarningDirectiveTriviaSyntax pragma
            && pragma.IsActive
            && pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);

        private static void ReportPragma(SyntaxTreeAnalysisContext context, PragmaWarningDirectiveTriviaSyntax pragma)
        {
            if (pragma.ErrorCodes.Count == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ArchitectureGuardDiagnostics.DiagnosticSuppression,
                    pragma.GetLocation(),
                    "ID を書かない #pragma warning disable（すべての警告を消すもの）"));
                return;
            }

            foreach (var errorCode in pragma.ErrorCodes)
            {
                var id = IdTextOf(errorCode);
                if (ArchitectureGuardDiagnostics.IsArchitectureGuardId(id))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ArchitectureGuardDiagnostics.DiagnosticSuppression,
                        errorCode.GetLocation(),
                        $"#pragma warning disable {id}"));
                }
            }
        }

        private static string IdTextOf(ExpressionSyntax errorCode) =>
            errorCode is IdentifierNameSyntax identifier ? identifier.Identifier.ValueText : errorCode.ToString();

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var suppressMessageAttribute = context.Compilation.GetTypeByMetadataName(MetadataNames.SuppressMessageAttribute);
            if (suppressMessageAttribute is null)
            {
                return;
            }

            var writtenNames = AttributeTypeResolver.WrittenNamesOf(suppressMessageAttribute);
            context.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeAttribute(nodeContext, suppressMessageAttribute, writtenNames),
                SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol suppressMessageAttribute,
            ImmutableHashSet<string> writtenNames)
        {
            var attribute = (AttributeSyntax)context.Node;
            var attributeType = AttributeTypeResolver.Resolve(attribute, writtenNames, context.SemanticModel, context.CancellationToken);
            if (!SymbolEqualityComparer.Default.Equals(attributeType, suppressMessageAttribute))
            {
                return;
            }

            var checkIdArgument = FindCheckIdArgument(attribute);
            if (checkIdArgument is null)
            {
                return;
            }

            var constant = context.SemanticModel.GetConstantValue(checkIdArgument.Expression, context.CancellationToken);
            if (constant.Value is not string checkId)
            {
                return;
            }

            var id = DiagnosticIdOf(checkId);
            if (ArchitectureGuardDiagnostics.IsArchitectureGuardId(id))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ArchitectureGuardDiagnostics.DiagnosticSuppression,
                    attribute.GetLocation(),
                    $"[SuppressMessage] の CheckId \"{id}\""));
            }
        }

        private static AttributeArgumentSyntax? FindCheckIdArgument(AttributeSyntax attribute)
        {
            var arguments = attribute.ArgumentList?.Arguments ?? default;
            return arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == CheckIdParameterName)
                ?? arguments
                    .Where(argument => argument.NameColon is null && argument.NameEquals is null)
                    .ElementAtOrDefault(CheckIdParameterPosition);
        }

        private static string DiagnosticIdOf(string checkId)
        {
            var separatorIndex = checkId.IndexOf(':');
            return (separatorIndex < 0 ? checkId : checkId.Substring(0, separatorIndex)).Trim();
        }
    }
}
