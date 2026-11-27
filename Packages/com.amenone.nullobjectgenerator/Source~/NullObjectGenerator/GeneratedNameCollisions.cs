using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator
{
    internal static class GeneratedNameCollisions
    {
        public static EquatableArray<CollisionCandidate> Merge(ImmutableArray<CollisionCandidate> interfaceCandidates, ImmutableArray<CollisionCandidate> classCandidates) =>
            interfaceCandidates.Concat(classCandidates).ToEquatableArray();

        public static EquatableArray<GeneratedTypeKey> CollidingKeys(EquatableArray<CollisionCandidate> candidates) =>
            Colliding(candidates)
                .Select(candidate => candidate.Key)
                .Distinct()
                .OrderBy(key => key.Namespace, System.StringComparer.Ordinal)
                .ThenBy(key => key.Name, System.StringComparer.Ordinal)
                .ThenBy(key => key.Arity)
                .ToEquatableArray();

        public static void Report(SourceProductionContext context, EquatableArray<CollisionCandidate> candidates)
        {
            foreach (var candidate in Colliding(candidates))
            {
                var generatedName = candidate.Key.Namespace.Length == 0 ? candidate.Key.Name : $"{candidate.Key.Namespace}.{candidate.Key.Name}";
                context.ReportDiagnostic(candidate.Location.With(NullObjectDiagnostics.GeneratedNameCollision, candidate.TargetName, generatedName).ToDiagnostic());
            }
        }

        private static IEnumerable<CollisionCandidate> Colliding(IEnumerable<CollisionCandidate> candidates) =>
            candidates.GroupBy(candidate => candidate.Key).Where(group => group.Count() > 1).SelectMany(group => group);
    }
}
