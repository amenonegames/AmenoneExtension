using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class DiagnosticTests
    {
        private static readonly CSharpParseOptions Preview = new CSharpParseOptions(LanguageVersion.Preview);

        [Fact]
        public void Generate_RefReturningMembers_ShouldReportNog001AndSkipTheType()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface IRefs
    {
        ref int Slot();
        ref readonly int this[int index] { get; }
    }

    [InterfaceToNullObj]
    public interface IFine
    {
        void Run();
    }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Equal(new[] { "NOG001", "NOG001" }, result.GeneratorDiagnosticIds);
            Assert.DoesNotContain("Sample.IRefs.AsNullObj.g.cs", result.HintNames);
            Assert.Contains("Sample.IFine.AsNullObj.g.cs", result.HintNames);
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_ClassWithRefReturningInterface_ShouldReportNog001()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IRefs
    {
        ref int Slot();
    }

    [InheritsToNullObj]
    public class Refs : IRefs
    {
        private int _slot;
        public ref int Slot() => ref _slot;
    }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Equal(new[] { "NOG001" }, result.GeneratorDiagnosticIds);
            Assert.DoesNotContain("Sample.Refs.AsNullObj.g.cs", result.HintNames);
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_StaticAbstractMember_ShouldReportNog002AndSkipTheType()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
    {
        static abstract TSelf Create();
        static virtual int Count() => 0;
        void Run();
    }
}";
            var result = GeneratorRunner.Run(new[] { source }, GeneratorRunner.DefaultAssemblyName, new LibraryStub[0], Preview);

            Assert.Equal(new[] { "NOG002" }, result.GeneratorDiagnosticIds);
            Assert.DoesNotContain("Sample.IFactory`1.AsNullObj.g.cs", result.HintNames);
            Assert.Contains("'Sample.IFactory<TSelf>.Create()'", result.GeneratorDiagnostics.Single().GetMessage());
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_ClassWithStaticAbstractInterface_ShouldReportNog002()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
    {
        static abstract TSelf Create();
    }

    [InheritsToNullObj]
    public class Factory : IFactory<Factory>
    {
        public static Factory Create() => new Factory();
    }
}";
            var result = GeneratorRunner.Run(new[] { source }, GeneratorRunner.DefaultAssemblyName, new LibraryStub[0], Preview);

            Assert.Equal(new[] { "NOG002" }, result.GeneratorDiagnosticIds);
            Assert.DoesNotContain("Sample.Factory.AsNullObj.g.cs", result.HintNames);
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_PrivateNestedInterface_ShouldReportNog003AtItsName()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public class Outer
    {
        [InterfaceToNullObj]
        private interface ISecret
        {
            void Run();
        }

        [InterfaceToNullObj]
        protected interface IFamily
        {
            void Run();
        }
    }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Equal(new[] { "NOG003", "NOG003" }, result.GeneratorDiagnosticIds);
            var location = result.GeneratorDiagnostics.First().Location.GetLineSpan();
            Assert.Equal("Source0.cs", location.Path);
            Assert.Equal(8, location.StartLinePosition.Line);
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_PrivateNestedClass_ShouldReportNog003()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IRun
    {
        void Run();
    }

    public class Outer
    {
        [InheritsToNullObj]
        private class Hidden : IRun
        {
            public void Run() { }
        }
    }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Equal(new[] { "NOG003" }, result.GeneratorDiagnosticIds);
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_SameGeneratedNameTwice_ShouldReportNog004ForEachAndSkipBoth()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public class First
    {
        [InterfaceToNullObj]
        public interface IFoo { void Run(); }
    }

    public class Second
    {
        [InterfaceToNullObj]
        public interface IFoo { void Stop(); }
    }

    public interface IBar { void Run(); }

    public class Third
    {
        [InheritsToNullObj]
        public class Bar : IBar { public void Run() { } }
    }

    [InheritsToNullObj]
    public class Bar : IBar { public void Run() { } }

    [InterfaceToNullObj]
    public interface IFoo<T> { void Run(); }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Equal(new[] { "NOG004", "NOG004", "NOG004", "NOG004" }, result.GeneratorDiagnosticIds);
            Assert.Contains("'Sample.IFooAsNullObj'", result.GeneratorDiagnostics.First().GetMessage());
            Assert.Equal(
                new[] { "NullObjectGenerator.Attributes.g.cs", "Sample.IFoo`1.AsNullObj.g.cs" },
                result.HintNames.OrderBy(name => name, System.StringComparer.Ordinal).ToArray());
            Assert.Empty(result.CompileErrors);
        }

        [Fact]
        public void Generate_NestedGenericTypesSharingParameterName_ShouldReportNog005()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public class Outer<T>
    {
#pragma warning disable CS0693
        [InterfaceToNullObj]
        public interface IInner<T>
        {
            T Get();
        }
#pragma warning restore CS0693
    }
}";
            var result = GeneratorRunner.Run(source);

            Assert.Equal(new[] { "NOG005" }, result.GeneratorDiagnosticIds);
            Assert.Contains("'T'", result.GeneratorDiagnostics.Single().GetMessage());
            Assert.Empty(result.CompileErrors);
        }
    }
}
