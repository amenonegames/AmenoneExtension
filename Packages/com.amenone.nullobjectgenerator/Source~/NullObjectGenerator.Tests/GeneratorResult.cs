using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    internal sealed class GeneratorResult
    {
        public GeneratorResult(
            Compilation output,
            ImmutableArray<Diagnostic> generatorDiagnostics,
            GeneratorDriverRunResult runResult,
            IReadOnlyList<LibraryStub> libraries)
        {
            Output = output;
            GeneratorDiagnostics = generatorDiagnostics;
            RunResult = runResult;
            Libraries = libraries;
        }

        public Compilation Output { get; }

        public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

        public GeneratorDriverRunResult RunResult { get; }

        public IReadOnlyList<LibraryStub> Libraries { get; }

        public IEnumerable<string> RawHintNames => RunResult.Results.Single().GeneratedSources.Select(source => source.HintName);

        // Tests name generated files without the per-type hash, which StableHashTests checks on its own.
        public IEnumerable<string> HintNames => RawHintNames.Select(WithoutHash);

        public string[] GeneratorDiagnosticIds => GeneratorDiagnostics.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();

        public string Source(string hintName) =>
            RunResult.Results.Single().GeneratedSources.Single(source => WithoutHash(source.HintName) == hintName).SourceText.ToString();

        public IEnumerable<Diagnostic> CompileErrors =>
            Output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        public static string WithoutHash(string hintName) =>
            Regex.Replace(hintName, @"\.[0-9a-f]{8}\.g\.cs$", ".g.cs");

        public GeneratorResult AssertCompilesCleanly()
        {
            Assert.Empty(RunResult.Results.Single().Exception is { } exception ? new[] { exception.ToString() } : Array.Empty<string>());
            Assert.Empty(GeneratorDiagnostics);
            Assert.Empty(CompileErrors);
            var generatedTrees = RunResult.GeneratedTrees.ToHashSet();
            var generatedWarnings = Output.GetDiagnostics().Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Warning
                && diagnostic.Location.SourceTree is { } tree
                && generatedTrees.Contains(tree));
            Assert.Empty(generatedWarnings);
            return this;
        }
    }
}
