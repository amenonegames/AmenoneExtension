using System.Linq;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class HintNameTests
    {
        [Fact]
        public void HintName_SameTypeNameInDifferentPlaces_ShouldBeDistinct()
        {
            const string source = @"
using NullObjectGenerator;

namespace First
{
    [InterfaceToNullObj]
    public interface IFoo { void Run(); }

    [InterfaceToNullObj]
    public interface IFoo<T> { T Run(); }

    public class Outer
    {
        [InterfaceToNullObj]
        public interface IBar { void Run(); }
    }
}

namespace Second
{
    [InterfaceToNullObj]
    public interface IFoo { void Run(); }

    namespace Outer
    {
        [InterfaceToNullObj]
        public interface IBar { void Run(); }
    }
}

[InterfaceToNullObj]
public interface IFoo { void Run(); }";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Equal(
                new[]
                {
                    "First.IFoo.AsNullObj.g.cs",
                    "First.IFoo`1.AsNullObj.g.cs",
                    "First.Outer+IBar.AsNullObj.g.cs",
                    "IFoo.AsNullObj.g.cs",
                    "NullObjectGenerator.Attributes.g.cs",
                    "Second.IFoo.AsNullObj.g.cs",
                    "Second.Outer.IBar.AsNullObj.g.cs",
                },
                result.HintNames.OrderBy(name => name, System.StringComparer.Ordinal).ToArray());
        }

        [Fact]
        public void Generate_TypesDifferingOnlyInCase_ShouldGenerateBothWithHintNamesUniqueIgnoringCase()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface IFoo { void Run(); }

    [InterfaceToNullObj]
    public interface Ifoo { void Stop(); }
}

namespace Game.UI
{
    [InterfaceToNullObj]
    public interface IView { void Show(); }
}

namespace Game.Ui
{
    [InterfaceToNullObj]
    public interface IView { void Hide(); }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var raw = result.RawHintNames.ToArray();
            Assert.Equal(raw.Length, raw.Distinct(System.StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains("Sample.IFoo.AsNullObj.0b591636.g.cs", raw);
            Assert.Contains("Sample.Ifoo.AsNullObj.6de3add6.g.cs", raw);
            Assert.Contains("Game.UI.IView.AsNullObj.g.cs", result.HintNames);
            Assert.Contains("Game.Ui.IView.AsNullObj.g.cs", result.HintNames);
        }

        [Fact]
        public void Generate_AttributeOnSeveralPartialDeclarations_ShouldGenerateOnce()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public partial interface IFoo { void Run(); }

    [InterfaceToNullObj]
    public partial interface IFoo { void Stop(); }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Null(result.RunResult.Results.Single().Exception);
            Assert.Single(result.HintNames, name => name == "Sample.IFoo.AsNullObj.g.cs");
            Assert.Contains(result.CompileErrors, diagnostic => diagnostic.Id == "CS0579");
        }
    }
}
