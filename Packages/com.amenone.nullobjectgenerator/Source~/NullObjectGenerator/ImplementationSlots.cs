using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator
{
    internal sealed class ImplementationSlots
    {
        // Public members with these shapes would hide or fail to override System.Object members (CS0108 / CS0114).
        private static readonly string[] ObjectMethodSignatures =
        {
            "ToString`0()",
            "GetHashCode`0()",
            "GetType`0()",
            "Finalize`0()",
            "MemberwiseClone`0()",
            "Equals`0(object)",
            "Equals`0(object,object)",
            "ReferenceEquals`0(object,object)",
        };

        // The generated indexers carry no IndexerName attribute, so they always take these names (CS0102 / CS0082).
        private static readonly string[] IndexerNames = { "Item", "get_Item", "set_Item" };

        private readonly HashSet<string> _methodNames = new HashSet<string> { "ToString", "GetHashCode", "GetType", "Finalize", "MemberwiseClone", "Equals", "ReferenceEquals" };
        private readonly HashSet<string> _methodSignatures = new HashSet<string>(ObjectMethodSignatures);
        private readonly HashSet<string> _nonMethodNames = new HashSet<string>();
        private readonly HashSet<string> _indexerSignatures = new HashSet<string>();
        private bool _hasIndexer;

        public ImplementationSlots(string generatedTypeName) => _nonMethodNames.Add(generatedTypeName);

        public bool TryClaim(ISymbol member) =>
            member switch
            {
                IMethodSymbol method => TryClaimMethod(method),
                IPropertySymbol { IsIndexer: true } indexer => TryClaimIndexer(indexer),
                IPropertySymbol property => TryClaimNames(PropertyNames(property)),
                _ => TryClaimNames(new[] { member.Name, "add_" + member.Name, "remove_" + member.Name }),
            };

        private bool TryClaimMethod(IMethodSymbol method)
        {
            if (_nonMethodNames.Contains(method.Name) || (_hasIndexer && IndexerNames.Contains(method.Name)))
            {
                return false;
            }

            var signature = $"{method.Name}`{method.Arity}({ParameterKey(method.Parameters)})";
            if (!_methodSignatures.Add(signature))
            {
                return false;
            }

            _methodNames.Add(method.Name);
            return true;
        }

        private bool TryClaimIndexer(IPropertySymbol indexer)
        {
            if (!_hasIndexer && IndexerNames.Any(name => _methodNames.Contains(name) || _nonMethodNames.Contains(name)))
            {
                return false;
            }

            if (!_indexerSignatures.Add(ParameterKey(indexer.Parameters)))
            {
                return false;
            }

            _hasIndexer = true;
            return true;
        }

        private bool TryClaimNames(IReadOnlyCollection<string> names)
        {
            if (names.Any(name => _methodNames.Contains(name) || _nonMethodNames.Contains(name) || (_hasIndexer && IndexerNames.Contains(name))))
            {
                return false;
            }

            _nonMethodNames.UnionWith(names);
            return true;
        }

        private static IReadOnlyCollection<string> PropertyNames(IPropertySymbol property)
        {
            var names = new List<string> { property.Name };
            if (property.GetMethod != null)
            {
                names.Add("get_" + property.Name);
            }

            if (property.SetMethod != null)
            {
                names.Add("set_" + property.Name);
            }

            return names;
        }

        private static string ParameterKey(IEnumerable<IParameterSymbol> parameters) =>
            string.Join(",", parameters.Select(parameter =>
                (parameter.RefKind == RefKind.None ? string.Empty : "&") + TypeNames.SignatureKey(parameter.Type)));
    }
}
