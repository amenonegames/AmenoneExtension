using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class AllowAttributeScope
    {
        public static bool Covers(ISymbol? innermostSymbol, INamedTypeSymbol? allowAttribute)
        {
            if (allowAttribute is null)
            {
                return false;
            }

            for (var symbol = innermostSymbol; symbol is not null and not INamespaceSymbol; symbol = OuterOf(symbol))
            {
                if (HasAttribute(symbol, allowAttribute))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute) =>
            symbol.GetAttributes().Any(data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, attribute));

        private static ISymbol? OuterOf(ISymbol symbol) =>
            symbol is IMethodSymbol { AssociatedSymbol: { } owner } ? owner : symbol.ContainingSymbol;
    }
}
