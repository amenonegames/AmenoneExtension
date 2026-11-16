using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class AttributeTypeResolver
    {
        private const string AttributeSuffix = "Attribute";

        public static ImmutableHashSet<string> WrittenNamesOf(params INamedTypeSymbol[] attributeTypes) =>
            attributeTypes
                .SelectMany(attributeType => new[] { attributeType.Name, ShortNameOf(attributeType) })
                .ToImmutableHashSet();

        public static string ShortNameOf(INamedTypeSymbol attributeType) =>
            attributeType.Name.EndsWith(AttributeSuffix)
                ? attributeType.Name.Substring(0, attributeType.Name.Length - AttributeSuffix.Length)
                : attributeType.Name;

        public static INamedTypeSymbol? Resolve(
            AttributeSyntax attribute,
            ImmutableHashSet<string> writtenNames,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            if (!MayName(attribute, writtenNames, model, cancellationToken))
            {
                return null;
            }

            var symbolInfo = model.GetSymbolInfo(attribute, cancellationToken);
            var constructor = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            return constructor?.ContainingType;
        }

        private static bool MayName(
            AttributeSyntax attribute,
            ImmutableHashSet<string> writtenNames,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            var simpleName = SimpleNameOf(attribute.Name);
            return simpleName is null
                || writtenNames.Contains(simpleName.Identifier.ValueText)
                || model.GetAliasInfo(simpleName, cancellationToken) is not null;
        }

        private static SimpleNameSyntax? SimpleNameOf(NameSyntax name) => name switch
        {
            SimpleNameSyntax simple => simple,
            QualifiedNameSyntax qualified => qualified.Right,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name,
            _ => null,
        };
    }
}
