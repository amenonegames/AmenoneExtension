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

        [Fact]
        public void Rerun_EditingOneInterface_ShouldLeaveOtherTypesCached()
        {
            var (first, second) = RunTwice(FooSource.Replace("void Run();", "void Run();\n        void Stop();"), BarSource, UnrelatedSource);

            Assert.Equal(IncrementalStepRunReason.Modified, TargetReason(second, NullObjectSourceGenerator.InterfaceTargetsStep, "IFooAsNullObj"));
            AssertReused(TargetReason(second, NullObjectSourceGenerator.InterfaceTargetsStep, "IBarAsNullObj"));
            AssertReused(TargetReason(second, NullObjectSourceGenerator.ClassTargetsStep, "BazAsNullObj"));
            Assert.Equal(1, ModifiedSourceOutputs(second));
            Assert.Equal(
                first.GeneratedSources.Single(source => source.HintName == "Sample.IBar.AsNullObj.g.cs").SourceText.ToString(),
                second.GeneratedSources.Single(source => source.HintName == "Sample.IBar.AsNullObj.g.cs").SourceText.ToString());
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

        private static void AssertEverythingReused(GeneratorRunResult result)
        {
            AssertReused(TargetReason(result, NullObjectSourceGenerator.InterfaceTargetsStep, "IFooAsNullObj"));
            AssertReused(TargetReason(result, NullObjectSourceGenerator.InterfaceTargetsStep, "IBarAsNullObj"));
            AssertReused(TargetReason(result, NullObjectSourceGenerator.ClassTargetsStep, "BazAsNullObj"));
            Assert.All(result.TrackedSteps[NullObjectSourceGenerator.CollidingKeysStep].SelectMany(step => step.Outputs), output => AssertReused(output.Reason));
            Assert.Equal(0, ModifiedSourceOutputs(result));
        }

        private static (GeneratorRunResult First, GeneratorRunResult Second) RunTwice(string editedFoo, string editedBar, string editedUnrelated)
        {
            var compilation = GeneratorRunner.CreateCompilation(
                new[] { FooSource, BarSource, UnrelatedSource },
                GeneratorRunner.DefaultAssemblyName,
                new[] { LibraryStub.UnityEngine.Reference },
                GeneratorRunner.ParseOptions);
            GeneratorDriver driver = GeneratorRunner.CreateDriver(GeneratorRunner.ParseOptions);
            driver = driver.RunGenerators(compilation);
            var first = driver.GetRunResult().Results.Single();

            var trees = compilation.SyntaxTrees.ToArray();
            var edited = compilation
                .ReplaceSyntaxTree(trees[0], trees[0].WithChangedText(SourceText.From(editedFoo)))
                .ReplaceSyntaxTree(trees[1], trees[1].WithChangedText(SourceText.From(editedBar)))
                .ReplaceSyntaxTree(trees[2], trees[2].WithChangedText(SourceText.From(editedUnrelated)));
            driver = driver.RunGenerators(edited);
            return (first, driver.GetRunResult().Results.Single());
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
