using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal sealed class LibraryStub
    {
        public static readonly LibraryStub Entities = new LibraryStub("Unity.Entities", EntitiesStub.Source);
        public static readonly LibraryStub MessagePipe = new LibraryStub("MessagePipe", MessagePipeStub.Source);
        public static readonly LibraryStub NUnit = new LibraryStub("nunit.framework", "namespace NUnit.Framework { public sealed class TestAttribute : System.Attribute {} }");
        public static readonly LibraryStub TestRunner = new LibraryStub("UnityEngine.TestRunner", "namespace UnityEngine.TestTools { public sealed class UnityTestAttribute : System.Attribute {} }");
        public static readonly LibraryStub VContainer = new LibraryStub("VContainer", VContainerStub.Source);
        public static readonly LibraryStub EcsBridge = new LibraryStub("amenone.ecsbridge", EcsBridgeStub.Source, Entities, VContainer);

        private readonly Lazy<MetadataReference> _reference;

        private LibraryStub(string assemblyName, string source, params LibraryStub[] dependencies) =>
            _reference = new Lazy<MetadataReference>(() => AnalyzerRunner.CompileLibrary(
                assemblyName,
                source,
                dependencies.Select(dependency => dependency.Reference)));

        public MetadataReference Reference => _reference.Value;
    }
}
