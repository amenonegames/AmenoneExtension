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
            new MessageSubscriberAnalyzer(),
            new PublishingSystemAnalyzer());

        private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        private static readonly Lazy<ImmutableArray<MetadataReference>> PlatformReferences = new Lazy<ImmutableArray<MetadataReference>>(LoadPlatformReferences);

        public static string[] AnalyzeIds(string source) => IdsOf(Analyze(source));

        public static string[] AnalyzeIdsAllowingCompileErrors(string source) => IdsOf(Analyze(source, allowCompileErrors: true));

        public static string[] AnalyzeIdsWithoutUnityEngine(string source) => IdsOf(Analyze(source, includeUnityEngine: false));

        public static string[] AnalyzeIdsWith(string source, params LibraryStub[] libraries) =>
            IdsOf(Analyze(source, libraries: libraries));

        public static string[] AnalyzeIdsIn(string assemblyName, string source, params LibraryStub[] libraries) =>
            IdsOf(Analyze(source, assemblyName: assemblyName, libraries: libraries));

        public static ImmutableArray<Diagnostic> AnalyzeFiles(IReadOnlyList<SourceFile> files, params LibraryStub[] libraries) =>
            Analyze(files, includeUnityEngine: true, allowCompileErrors: false, DefaultAssemblyName, libraries);

        public static ImmutableArray<Diagnostic> Analyze(
            string source,
            bool includeUnityEngine = true,
            bool allowCompileErrors = false,
            string assemblyName = DefaultAssemblyName,
            IReadOnlyCollection<LibraryStub>? libraries = null) =>
            Analyze(new[] { new SourceFile(string.Empty, source) }, includeUnityEngine, allowCompileErrors, assemblyName, libraries ?? Array.Empty<LibraryStub>());

        private static ImmutableArray<Diagnostic> Analyze(
            IReadOnlyList<SourceFile> files,
            bool includeUnityEngine,
            bool allowCompileErrors,
            string assemblyName,
            IReadOnlyCollection<LibraryStub> libraries)
        {
            var compilation = CreateCompilation(files, includeUnityEngine, assemblyName, libraries);
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

        public static MetadataReference CompileLibrary(string assemblyName, string source, IEnumerable<MetadataReference> dependencies)
        {
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, ParseOptions) },
                PlatformReferences.Value.AddRange(dependencies),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            AssertNoCompileErrors(compilation);
            using var image = new MemoryStream();
            Assert.True(compilation.Emit(image).Success);
            return MetadataReference.CreateFromImage(image.ToArray());
        }

        private static CSharpCompilation CreateCompilation(
            IReadOnlyList<SourceFile> files,
            bool includeUnityEngine,
            string assemblyName,
            IReadOnlyCollection<LibraryStub> libraries)
        {
            var sources = new List<SourceFile>(files);
            sources.AddRange(ReadRuntimeSources().Select(text => new SourceFile(string.Empty, text)));
            if (includeUnityEngine)
            {
                sources.Add(new SourceFile(string.Empty, UnityEngineStub.Source));
            }

            return CSharpCompilation.Create(
                assemblyName,
                sources.Select(file => CSharpSyntaxTree.ParseText(file.Text, ParseOptions, file.Path)),
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
