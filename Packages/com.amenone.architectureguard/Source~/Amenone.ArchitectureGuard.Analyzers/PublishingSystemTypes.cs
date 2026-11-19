using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal sealed class PublishingSystemTypes
    {
        private readonly INamedTypeSymbol? _systemBase;
        private readonly INamedTypeSymbol? _systemInterface;
        private readonly MessagePipeInterfaces _publishers;
        private readonly INamedTypeSymbol _publishSystem;

        private PublishingSystemTypes(
            INamedTypeSymbol? systemBase,
            INamedTypeSymbol? systemInterface,
            MessagePipeInterfaces publishers,
            INamedTypeSymbol publishSystem)
        {
            _systemBase = systemBase;
            _systemInterface = systemInterface;
            _publishers = publishers;
            _publishSystem = publishSystem;
        }

        public static PublishingSystemTypes? Resolve(Compilation compilation)
        {
            var systemBase = compilation.GetTypeByMetadataName(MetadataNames.SystemBase);
            var systemInterface = compilation.GetTypeByMetadataName(MetadataNames.ISystem);
            var publishers = MessagePipeInterfaces.Resolve(compilation, MetadataNames.MessagePipePublishers);
            var publishSystem = compilation.GetTypeByMetadataName(MetadataNames.PublishSystem);
            if ((systemBase is null && systemInterface is null) || publishers is null || publishSystem is null)
            {
                return null;
            }

            return new PublishingSystemTypes(systemBase, systemInterface, publishers, publishSystem);
        }

        public bool IsSystem(INamedTypeSymbol type) =>
            type.TypeKind is TypeKind.Class or TypeKind.Struct
            && (InheritsFrom(type, _systemBase) || ImplementsSystemInterface(type));

        public bool InheritsPublishSystem(INamedTypeSymbol type) => InheritsFrom(type, _publishSystem);

        public ImmutableArray<ITypeSymbol> PublishersReceivedBy(INamedTypeSymbol type) =>
            type.GetMembers()
                .SelectMany(member => ReceivedTypes.Of(member).Select(received => received.Type))
                .Where(_publishers.Covers)
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                .ToImmutableArray();

        private bool ImplementsSystemInterface(INamedTypeSymbol type) =>
            _systemInterface is not null && type.AllInterfaces.Contains(_systemInterface, SymbolEqualityComparer.Default);

        private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol? baseDefinition)
        {
            if (baseDefinition is null)
            {
                return false;
            }

            for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, baseDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
