using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class AssemblyBoundaryTests
    {
        private const string AbstractionSource = @"
using NullObjectGenerator;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Friend"")]

namespace Sample.Abstractions
{
    [InterfaceToNullObj]
    public interface IService
    {
        void Run();
    }
}";

        [Fact]
        public void Generate_InterfaceInReferencedAssembly_ShouldBeUsableFromReferencingAssembly()
        {
            var abstractions = GeneratorRunner.Run(AbstractionSource).AssertCompilesCleanly();
            var reference = MetadataReference.CreateFromImage(GeneratorRunner.Emit(abstractions.Output));
            const string consumer = @"
namespace Sample.Game
{
    public static class Usage
    {
        public static Sample.Abstractions.IService Create() => new Sample.Abstractions.IServiceAsNullObj();
    }
}";
            var compilation = GeneratorRunner.CreateCompilation(new[] { consumer }, "Consumer", new[] { reference }, GeneratorRunner.ParseOptions);

            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public void Generate_FriendAssemblyWithoutAttributeUse_ShouldNotWarn()
        {
            var abstractions = GeneratorRunner.Run(AbstractionSource).AssertCompilesCleanly();
            var reference = LibraryFrom(abstractions);
            const string friend = @"
namespace Sample.Friend
{
    public interface IUnmarked
    {
        void Run();
    }
}";

            GeneratorRunner.Run(new[] { friend }, "Friend", new[] { reference }, GeneratorRunner.ParseOptions).AssertCompilesCleanly();
        }

        [Fact]
        public void Generate_FriendAssemblyUsingAttribute_ShouldWarnCs0436AtTheUseOnly()
        {
            var abstractions = GeneratorRunner.Run(AbstractionSource).AssertCompilesCleanly();
            var reference = LibraryFrom(abstractions);
            const string friend = @"
using NullObjectGenerator;

namespace Sample.Friend
{
    [InterfaceToNullObj(NullObjLog.DebugLog)]
    public interface IFriendService
    {
        void Run();
    }
}";

            var result = GeneratorRunner.Run(new[] { friend }, "Friend", new[] { reference }, GeneratorRunner.ParseOptions).AssertCompilesCleanly();

            var warnings = result.Output.GetDiagnostics().Where(diagnostic => diagnostic.Id == "CS0436").ToList();
            Assert.NotEmpty(warnings);
            Assert.All(warnings, warning => Assert.Equal("Source0.cs", warning.Location.SourceTree!.FilePath));
            Assert.Contains("Sample.Friend.IFriendService.AsNullObj.g.cs", result.HintNames);
        }

        private static LibraryStub LibraryFrom(GeneratorResult result) =>
            LibraryStub.FromImage(result.Output.AssemblyName!, GeneratorRunner.Emit(result.Output));
    }
}
