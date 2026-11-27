using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class InheritsToNullObjTests
    {
        [Fact]
        public void Generate_ClassWithInterfaces_ShouldImplementAllWithClassNamespaceAndAccessibility()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IBase { void Base(); }
    public interface IHoge : IBase { string TestStr { get; } }
    internal interface IFuga { uint Value { get; set; } }

    public abstract class Parent : IFuga
    {
        public uint Value { get; set; }
    }

    [InheritsToNullObj]
    public class Child : Parent, IHoge
    {
        public string TestStr => null;
        public void Base() { }
    }

    [InheritsToNullObj]
    internal class Hidden : IHoge
    {
        public string TestStr => null;
        public void Base() { }
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var child = result.Source("Sample.Child.AsNullObj.g.cs");
            Assert.Contains("public class ChildAsNullObj : global::Sample.IFuga, global::Sample.IHoge, global::Sample.IBase", child);
            Assert.Contains("uint global::Sample.IFuga.Value", child);
            Assert.Contains("internal class HiddenAsNullObj", result.Source("Sample.Hidden.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_GenericClass_ShouldCarryTypeParametersAndConstraints()
        {
            const string source = @"
using System.Collections.Generic;
using NullObjectGenerator;

namespace Sample
{
    public interface IStore<T> where T : class
    {
        T Get(int id);
    }

    [InheritsToNullObj]
    public class Store<T> : IStore<T>, IEnumerable<T> where T : class
    {
        public T Get(int id) => null;
        public IEnumerator<T> GetEnumerator() => null;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
    }

    public static class Usage
    {
        public static IStore<string> Create() => new StoreAsNullObj<string>();
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("where T : class", result.Source("Sample.Store`1.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_ClassWithoutInterfaces_ShouldBeEmptyClass()
        {
            const string source = @"
using NullObjectGenerator;

[InheritsToNullObj]
public class Plain
{
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("public class PlainAsNullObj\n", result.Source("Plain.AsNullObj.g.cs"));
        }
    }
}
