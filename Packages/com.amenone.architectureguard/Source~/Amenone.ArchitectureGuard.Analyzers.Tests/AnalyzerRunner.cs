using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal static class AnalyzerRunner
    {
        private const string RuntimeSourceDirectoryName = "RuntimeSources";
        private const string DefaultAssemblyName = "ArchitectureGuardTarget";

        private static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new UnityTimeAnalyzer(),
            new UnityMessageAnalyzer(),
            new AllowReasonAnalyzer(),
            new SuppressionAnalyzer(),
            new EntityQueryCreationAnalyzer(),
            new MessageSubscriberAnalyzer());

        private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        private static readonly Lazy<ImmutableArray<MetadataReference>> PlatformReferences = new Lazy<ImmutableArray<MetadataReference>>(LoadPlatformReferences);

        public static string[] AnalyzeIds(string source) => IdsOf(Analyze(source));

        public static string[] AnalyzeIdsAllowingCompileErrors(string source) => IdsOf(Analyze(source, allowCompileErrors: true));

        public static string[] AnalyzeIdsWithoutUnityEngine(string source) => IdsOf(Analyze(source, includeUnityEngine: false));

        public static string[] AnalyzeIdsWith(string source, params LibraryStub[] libraries) =>
            IdsOf(Analyze(source, libraries: libraries));

        public static string[] AnalyzeIdsIn(string assemblyName, string source, params LibraryStub[] libraries) =>
            IdsOf(Analyze(source, assemblyName: assemblyName, libraries: libraries));

        public static ImmutableArray<Diagnostic> Analyze(
            string source,
            bool includeUnityEngine = true,
            bool allowCompileErrors = false,
            string assemblyName = DefaultAssemblyName,
            IReadOnlyCollection<LibraryStub>? libraries = null)
        {
            var compilation = CreateCompilation(source, includeUnityEngine, assemblyName, libraries ?? Array.Empty<LibraryStub>());
            if (!allowCompileErrors)
            {
                AssertNoCompileErrors(compilation);
            }

            var analyzerExceptions = new ConcurrentQueue<Exception>();
            var options = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                onAnalyzerException: (exception, _, _) => analyzerExceptions.Enqueue(exception),
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false);
            var diagnostics = compilation
                .WithAnalyzers(Analyzers, options)
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult();

            Assert.Empty(analyzerExceptions);
            return diagnostics;
        }

        public static MetadataReference CompileLibrary(string assemblyName, string source)
        {
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, ParseOptions) },
                PlatformReferences.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            AssertNoCompileErrors(compilation);
            using var image = new MemoryStream();
            Assert.True(compilation.Emit(image).Success);
            return MetadataReference.CreateFromImage(image.ToArray());
        }

        private static CSharpCompilation CreateCompilation(
            string source,
            bool includeUnityEngine,
            string assemblyName,
            IReadOnlyCollection<LibraryStub> libraries)
        {
            var sources = new List<string> { source };
            sources.AddRange(ReadRuntimeSources());
            if (includeUnityEngine)
            {
                sources.Add(UnityEngineStub.Source);
            }

            return CSharpCompilation.Create(
                assemblyName,
                sources.Select(text => CSharpSyntaxTree.ParseText(text, ParseOptions)),
                PlatformReferences.Value.AddRange(libraries.Select(library => library.Reference)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static IEnumerable<string> ReadRuntimeSources() =>
            Directory
                .GetFiles(Path.Combine(AppContext.BaseDirectory, RuntimeSourceDirectoryName), "*.cs")
                .Select(File.ReadAllText);

        private static ImmutableArray<MetadataReference> LoadPlatformReferences() =>
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToImmutableArray();

        private static void AssertNoCompileErrors(Compilation compilation)
        {
            var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Empty(errors);
        }

        private static string[] IdsOf(ImmutableArray<Diagnostic> diagnostics) =>
            diagnostics.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }
}
