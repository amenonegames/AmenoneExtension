using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal sealed class SystemUpdateMethods
    {
        private readonly IMethodSymbol? _systemBaseOnUpdate;
        private readonly INamedTypeSymbol? _systemInterface;
        private readonly IMethodSymbol? _systemInterfaceOnUpdate;

        private SystemUpdateMethods(INamedTypeSymbol? systemBase, INamedTypeSymbol? systemInterface)
        {
            _systemBaseOnUpdate = OnUpdateOf(systemBase);
            _systemInterface = systemInterface;
            _systemInterfaceOnUpdate = OnUpdateOf(systemInterface);
        }

        public static SystemUpdateMethods? Resolve(Compilation compilation)
        {
            var methods = new SystemUpdateMethods(
                compilation.GetTypeByMetadataName(MetadataNames.SystemBase),
                compilation.GetTypeByMetadataName(MetadataNames.ISystem));
            return methods._systemBaseOnUpdate is null && methods._systemInterfaceOnUpdate is null ? null : methods;
        }

        public bool IsOnUpdate(ISymbol? symbol) =>
            symbol is IMethodSymbol method && (OverridesSystemBaseOnUpdate(method) || ImplementsSystemInterfaceOnUpdate(method));

        private bool OverridesSystemBaseOnUpdate(IMethodSymbol method)
        {
            for (var overridden = method.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
            {
                if (SymbolEqualityComparer.Default.Equals(overridden.OriginalDefinition, _systemBaseOnUpdate))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ImplementsSystemInterfaceOnUpdate(IMethodSymbol method)
        {
            if (_systemInterfaceOnUpdate is null
                || !method.ContainingType.AllInterfaces.Contains(_systemInterface!, SymbolEqualityComparer.Default))
            {
                return false;
            }

            var implementation = method.ContainingType.FindImplementationForInterfaceMember(_systemInterfaceOnUpdate);
            return SymbolEqualityComparer.Default.Equals(implementation, method);
        }

        private static IMethodSymbol? OnUpdateOf(INamedTypeSymbol? type) =>
            type?.GetMembers("OnUpdate").OfType<IMethodSymbol>().FirstOrDefault();
    }
}
