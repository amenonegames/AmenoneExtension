using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal sealed class MessagePipeInterfaces
    {
        private readonly ImmutableHashSet<INamedTypeSymbol> _definitions;

        private MessagePipeInterfaces(ImmutableHashSet<INamedTypeSymbol> definitions) => _definitions = definitions;

        public static MessagePipeInterfaces? Resolve(Compilation compilation, IEnumerable<string> metadataNames)
        {
            var definitions = metadataNames
                .Select(compilation.GetTypeByMetadataName)
                .OfType<INamedTypeSymbol>()
                .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            return definitions.IsEmpty ? null : new MessagePipeInterfaces(definitions);
        }

        public bool Covers(ITypeSymbol type) =>
            type is INamedTypeSymbol named
            && (_definitions.Contains(named.OriginalDefinition)
                || named.AllInterfaces.Any(implemented => _definitions.Contains(implemented.OriginalDefinition)));
    }
}
