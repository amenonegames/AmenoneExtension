using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    internal static class GeneratorRunner
    {
        public const string DefaultAssemblyName = "NullObjectTarget";

        // Unity 6 compiles user code as C# 9.
        public static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp9);

        private static readonly Lazy<ImmutableArray<MetadataReference>> PlatformReferences =
            new Lazy<ImmutableArray<MetadataReference>>(LoadPlatformReferences);

        public static GeneratorResult Run(string source, params LibraryStub[] libraries) =>
            Run(new[] { source }, DefaultAssemblyName, libraries, ParseOptions);

        public static GeneratorResult Run(
            IReadOnlyList<string> sources,
            string assemblyName,
            IReadOnlyCollection<LibraryStub> libraries,
            CSharpParseOptions parseOptions,
            bool allowUnsafe = false)
        {
            var allLibraries = libraries.Append(LibraryStub.UnityEngine).SelectMany(library => library.WithDependencies()).Distinct().ToList();
            var compilation = CreateCompilation(sources, assemblyName, allLibraries.Select(library => library.Reference), parseOptions, allowUnsafe);
            GeneratorDriver driver = CreateDriver(parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
            return new GeneratorResult(output, generatorDiagnostics, driver.GetRunResult(), allLibraries);
        }

        public static CSharpGeneratorDriver CreateDriver(CSharpParseOptions parseOptions) =>
            CSharpGeneratorDriver.Create(
                new[] { new NullObjectSourceGenerator().AsSourceGenerator() },
                parseOptions: parseOptions,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        public static CSharpCompilation CreateCompilation(
            IEnumerable<string> sources,
            string assemblyName,
            IEnumerable<MetadataReference> references,
            CSharpParseOptions parseOptions,
            bool allowUnsafe = false) =>
            CSharpCompilation.Create(
                assemblyName,
                sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, $"Source{index}.cs")),
                PlatformReferences.Value.AddRange(references),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable,
                    allowUnsafe: allowUnsafe));

        public static byte[] EmitLibrary(string assemblyName, string source, IEnumerable<LibraryStub> dependencies)
        {
            var compilation = CreateCompilation(new[] { source }, assemblyName, dependencies.Select(dependency => dependency.Reference), ParseOptions);
            return Emit(compilation);
        }

        public static byte[] Emit(Compilation compilation)
        {
            using var image = new MemoryStream();
            var result = compilation.Emit(image);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return image.ToArray();
        }

        private static ImmutableArray<MetadataReference> LoadPlatformReferences() =>
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToImmutableArray();
    }
}
