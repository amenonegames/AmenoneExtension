using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal sealed class UnityTimeAccessClassifier
    {
        private static readonly string[] ScaledPropertyNames =
        {
            "deltaTime",
            "time",
            "timeAsDouble",
            "smoothDeltaTime",
            "fixedDeltaTime",
            "fixedTime",
            "fixedTimeAsDouble",
            "timeSinceLevelLoad",
            "timeSinceLevelLoadAsDouble",
        };

        private static readonly string[] UnscaledPropertyNames =
        {
            "unscaledDeltaTime",
            "unscaledTime",
            "unscaledTimeAsDouble",
            "fixedUnscaledDeltaTime",
            "fixedUnscaledTime",
            "fixedUnscaledTimeAsDouble",
            "realtimeSinceStartup",
            "realtimeSinceStartupAsDouble",
        };

        private readonly INamedTypeSymbol _timeType;
        private readonly ImmutableHashSet<IPropertySymbol> _scaled;
        private readonly ImmutableHashSet<IPropertySymbol> _unscaled;

        public UnityTimeAccessClassifier(INamedTypeSymbol timeType)
        {
            _timeType = timeType;
            _scaled = PropertiesNamed(timeType, ScaledPropertyNames);
            _unscaled = PropertiesNamed(timeType, UnscaledPropertyNames);
        }

        public UnityTimeUsage Classify(IPropertyReferenceOperation reference)
        {
            var property = reference.Property.OriginalDefinition;
            if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, _timeType))
            {
                return UnityTimeUsage.Unguarded;
            }

            if (IsAssignmentTarget(reference))
            {
                return UnityTimeUsage.SettingWrite;
            }

            if (_scaled.Contains(property))
            {
                return UnityTimeUsage.ScaledTimeRead;
            }

            return _unscaled.Contains(property) ? UnityTimeUsage.UnscaledTimeRead : UnityTimeUsage.Unguarded;
        }

        private static bool IsAssignmentTarget(IOperation operation) => operation.Parent switch
        {
            IAssignmentOperation assignment => assignment.Target == operation,
            IIncrementOrDecrementOperation increment => increment.Target == operation,
            ITupleOperation tuple => IsAssignmentTarget(tuple),
            _ => false,
        };

        private static ImmutableHashSet<IPropertySymbol> PropertiesNamed(INamedTypeSymbol timeType, string[] names) =>
            names
                .SelectMany(name => timeType.GetMembers(name))
                .OfType<IPropertySymbol>()
                .ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
    }
}
