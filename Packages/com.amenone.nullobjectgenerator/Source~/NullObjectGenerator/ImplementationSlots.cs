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

        private readonly HashSet<string> _nonMethodNames = new HashSet<string>();
        private readonly HashSet<string> _methodNames = new HashSet<string> { "ToString", "GetHashCode", "GetType", "Finalize", "MemberwiseClone", "Equals", "ReferenceEquals" };
        private readonly HashSet<string> _methodSignatures = new HashSet<string>(ObjectMethodSignatures);
        private readonly HashSet<string> _indexerSignatures = new HashSet<string>();

        public ImplementationSlots(string generatedTypeName) => _nonMethodNames.Add(generatedTypeName);

        public bool TryClaim(ISymbol member) =>
            member switch
            {
                IMethodSymbol method => TryClaimMethod(method),
                IPropertySymbol { IsIndexer: true } indexer => _indexerSignatures.Add(ParameterKey(indexer.Parameters)),
                _ => TryClaimName(member.Name),
            };

        private bool TryClaimMethod(IMethodSymbol method)
        {
            if (_nonMethodNames.Contains(method.Name))
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

        private bool TryClaimName(string name) =>
            !_methodNames.Contains(name) && _nonMethodNames.Add(name);

        private static string ParameterKey(IEnumerable<IParameterSymbol> parameters) =>
            string.Join(",", parameters.Select(parameter =>
                (parameter.RefKind == RefKind.None ? string.Empty : "&") + TypeNames.SignatureKey(parameter.Type)));
    }
}
