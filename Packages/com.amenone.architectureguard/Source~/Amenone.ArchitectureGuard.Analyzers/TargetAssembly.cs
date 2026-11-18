using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class TargetAssembly
    {
        private const string TestRunnerAssemblyName = "UnityEngine.TestRunner";

        private static readonly char[] NameSeparators = { '.', '-', '_' };
        private static readonly string[] PresenterSegments = { "presenter", "presenters" };
        private static readonly string[] TestSegments = { "test", "tests" };

        public static bool IsPresenter(Compilation compilation) => HasNameSegment(compilation, PresenterSegments);

        // nunit.framework は Unity のエディタでは、テストでないアセンブリにも参照が付くので、テストの目印にならない。
        public static bool IsTest(Compilation compilation) =>
            HasNameSegment(compilation, TestSegments)
            || compilation.ReferencedAssemblyNames.Any(identity => string.Equals(identity.Name, TestRunnerAssemblyName, StringComparison.OrdinalIgnoreCase));

        private static bool HasNameSegment(Compilation compilation, string[] segments) =>
            (compilation.AssemblyName ?? string.Empty)
                .Split(NameSeparators)
                .Any(segment => segments.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }
}
