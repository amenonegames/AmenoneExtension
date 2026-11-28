using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class IncrementalTests
    {
        private const string FooSource = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface IFoo
    {
        void Run();
    }
}";

        private const string BarSource = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface IBar
    {
        int Count { get; }
    }

    [InheritsToNullObj]
    public class Baz : IBar
    {
        public int Count => 0;
    }
}";

        private const string UnrelatedSource = @"
namespace Sample
{
    public class Unrelated
    {
    }
}";

        private const string FirstDuplicate = @"
using NullObjectGenerator;

namespace Sample
{
    public class First
    {
        [InterfaceToNullObj]
        public interface IDup { void Run(); }
    }
}";

        private const string SecondDuplicate = @"
using NullObjectGenerator;

namespace Sample
{
    public class Second
    {
        [InterfaceToNullObj]
        public interface IDup { void Stop(); }
    }
}";

        [Fact]
        public void Rerun_EditingOneInterface_ShouldLeaveOtherTypesCached()
        {
            var (first, second) = RunTwice(FooSource.Replace("void Run();", "void Run();\n        void Stop();"), BarSource, UnrelatedSource);

            Assert.Equal(IncrementalStepRunReason.Modified, TargetReason(second, NullObjectSourceGenerator.InterfaceTargetsStep, "IFooAsNullObj"));
            AssertReused(TargetReason(second, NullObjectSourceGenerator.InterfaceTargetsStep, "IBarAsNullObj"));
            AssertReused(TargetReason(second, NullObjectSourceGenerator.ClassTargetsStep, "BazAsNullObj"));
            Assert.Equal(1, ModifiedSourceOutputs(second));
            Assert.Equal(
                first.GeneratedSources.Single(source => GeneratorResult.WithoutHash(source.HintName) == "Sample.IBar.AsNullObj.g.cs").SourceText.ToString(),
                second.GeneratedSources.Single(source => GeneratorResult.WithoutHash(source.HintName) == "Sample.IBar.AsNullObj.g.cs").SourceText.ToString());
        }

        [Fact]
        public void Rerun_EditingUnrelatedFile_ShouldLeaveEveryTargetCached()
        {
            var (_, second) = RunTwice(FooSource, BarSource, UnrelatedSource.Replace("public class Unrelated", "public class Renamed"));

            AssertEverythingReused(second);
        }

        [Fact]
        public void Rerun_EditingOnlyWhitespaceAndComments_ShouldLeaveEveryTargetCached()
        {
            var (_, second) = RunTwice(
                "// moved down\n\n" + FooSource.Replace("void Run();", "void Run(); // still the same"),
                "\n\n/* two lines */\n" + BarSource.Replace("int Count { get; }", "int   Count   { get; }"),
                UnrelatedSource);

            AssertEverythingReused(second);
        }

        [Fact]
        public void Rerun_RemovingOneOfCollidingTypes_ShouldLeaveUnrelatedOutputsCached()
        {
            var (first, second) = RunTwice(
                new[] { FooSource, BarSource, UnrelatedSource, FirstDuplicate, SecondDuplicate },
                new[] { FooSource, BarSource, UnrelatedSource, FirstDuplicate });

            Assert.Equal(new[] { "NOG004", "NOG004" }, first.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
            AssertUnrelatedOutputsReused(second);
            Assert.Empty(second.Diagnostics);
            Assert.Contains(second.GeneratedSources, source => GeneratorResult.WithoutHash(source.HintName) == "Sample.First+IDup.AsNullObj.g.cs");
        }

        [Fact]
        public void Rerun_AddingCollidingType_ShouldLeaveUnrelatedOutputsCached()
        {
            var (first, second) = RunTwice(
                new[] { FooSource, BarSource, UnrelatedSource, FirstDuplicate },
                new[] { FooSource, BarSource, UnrelatedSource, FirstDuplicate, SecondDuplicate });

            Assert.Contains(first.GeneratedSources, source => GeneratorResult.WithoutHash(source.HintName) == "Sample.First+IDup.AsNullObj.g.cs");
            AssertUnrelatedOutputsReused(second);
            Assert.Equal(new[] { "NOG004", "NOG004" }, second.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
            Assert.DoesNotContain(second.GeneratedSources, source => source.HintName.Contains("IDup"));
        }

        private static void AssertEverythingReused(GeneratorRunResult result)
        {
            AssertReused(TargetReason(result, NullObjectSourceGenerator.InterfaceTargetsStep, "IFooAsNullObj"));
            AssertReused(TargetReason(result, NullObjectSourceGenerator.InterfaceTargetsStep, "IBarAsNullObj"));
            AssertReused(TargetReason(result, NullObjectSourceGenerator.ClassTargetsStep, "BazAsNullObj"));
            Assert.All(result.TrackedSteps[NullObjectSourceGenerator.CollidingKeysStep].SelectMany(step => step.Outputs), output => AssertReused(output.Reason));
            Assert.Equal(0, ModifiedSourceOutputs(result));
        }

        private static (GeneratorRunResult First, GeneratorRunResult Second) RunTwice(string editedFoo, string editedBar, string editedUnrelated) =>
            RunTwice(new[] { FooSource, BarSource, UnrelatedSource }, new[] { editedFoo, editedBar, editedUnrelated });

        private static (GeneratorRunResult First, GeneratorRunResult Second) RunTwice(string[] before, string[] after)
        {
            var compilation = GeneratorRunner.CreateCompilation(
                before,
                GeneratorRunner.DefaultAssemblyName,
                new[] { LibraryStub.UnityEngine.Reference },
                GeneratorRunner.ParseOptions);
            GeneratorDriver driver = GeneratorRunner.CreateDriver(GeneratorRunner.ParseOptions);
            driver = driver.RunGenerators(compilation);
            var first = driver.GetRunResult().Results.Single();

            var trees = compilation.SyntaxTrees.ToArray();
            Compilation edited = compilation;
            for (var i = 0; i < Math.Min(before.Length, after.Length); i++)
            {
                if (before[i] != after[i])
                {
                    edited = edited.ReplaceSyntaxTree(trees[i], trees[i].WithChangedText(SourceText.From(after[i])));
                }
            }

            edited = edited
                .RemoveSyntaxTrees(trees.Skip(after.Length))
                .AddSyntaxTrees(after.Skip(before.Length).Select((text, index) =>
                    CSharpSyntaxTree.ParseText(text, GeneratorRunner.ParseOptions, $"Added{index}.cs")));
            driver = driver.RunGenerators(edited);
            return (first, driver.GetRunResult().Results.Single());
        }

        private static IncrementalStepRunReason OutputReason(GeneratorRunResult result, string stepName, string typeName) =>
            result.TrackedSteps[stepName]
                .SelectMany(step => step.Outputs)
                .Single(output => output.Value is ValueTuple<NullObjectTarget, bool> { Item1.Spec.TypeName: var name } && name == typeName)
                .Reason;

        private static void AssertUnrelatedOutputsReused(GeneratorRunResult result)
        {
            AssertReused(OutputReason(result, NullObjectSourceGenerator.InterfaceOutputsStep, "IFooAsNullObj"));
            AssertReused(OutputReason(result, NullObjectSourceGenerator.InterfaceOutputsStep, "IBarAsNullObj"));
            AssertReused(OutputReason(result, NullObjectSourceGenerator.ClassOutputsStep, "BazAsNullObj"));
        }

        private static IncrementalStepRunReason TargetReason(GeneratorRunResult result, string stepName, string typeName) =>
            result.TrackedSteps[stepName]
                .SelectMany(step => step.Outputs)
                .Single(output => output.Value is NullObjectTarget { Spec.TypeName: var name } && name == typeName)
                .Reason;

        // Only the per-type outputs: the collision report re-runs whenever a target moves, and only reports diagnostics.
        private static int ModifiedSourceOutputs(GeneratorRunResult result) =>
            result.TrackedOutputSteps["SourceOutput"]
                .Where(step => step.Inputs.Any(input =>
                    input.Source.Name is NullObjectSourceGenerator.InterfaceOutputsStep or NullObjectSourceGenerator.ClassOutputsStep))
                .SelectMany(step => step.Outputs)
                .Count(output => output.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New);

        private static void AssertReused(IncrementalStepRunReason reason) =>
            Assert.Contains(reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged });
    }
}
