using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class ImplementationShapeTests
    {
        private static readonly LibraryStub HiddenInterfaces = new LibraryStub("Sample.Hidden", @"
namespace Sample.Hidden
{
    internal interface IInternalOnly
    {
        void Secret();
    }

    public interface IVisible
    {
        void Run();
    }

    public abstract class BaseWithInternal : IInternalOnly, IVisible
    {
        void IInternalOnly.Secret() { }
        public void Run() { }
    }
}");

        [Fact]
        public void Generate_ClassWithPrivateNestedInterface_ShouldLeaveThatInterfaceOut()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IVisible { void Run(); }

    public class Outer
    {
        private interface IPrivate { void Secret(); }

        [InheritsToNullObj]
        public class Inner : IPrivate, IVisible
        {
            void IPrivate.Secret() { }
            public void Run() { }
        }
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var generated = result.Source("Sample.Outer+Inner.AsNullObj.g.cs");
            Assert.Contains("public class InnerAsNullObj : global::Sample.IVisible\n", generated);
            Assert.DoesNotContain("IPrivate", generated);
        }

        [Fact]
        public void Generate_ClassWithInternalInterfaceOfReferencedBase_ShouldLeaveThatInterfaceOut()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample.Game
{
    [InheritsToNullObj]
    public class Derived : Sample.Hidden.BaseWithInternal
    {
    }
}";
            var result = GeneratorRunner.Run(source, HiddenInterfaces).AssertCompilesCleanly();

            var generated = result.Source("Sample.Game.Derived.AsNullObj.g.cs");
            Assert.Contains("public class DerivedAsNullObj : global::Sample.Hidden.IVisible\n", generated);
            Assert.DoesNotContain("IInternalOnly", generated);
        }

        [Fact]
        public void Generate_PublicClassWithInternalTypeArgument_ShouldImplementThoseMembersExplicitly()
        {
            const string source = @"
using System.Collections.Generic;
using NullObjectGenerator;

namespace Sample
{
    internal class Secret { }

    public interface IHolder<T>
    {
        void Put(T item);
        T Item { get; }
        event System.Action<T> Changed;
    }

    public interface IOpen
    {
        void Run();
    }

    [InheritsToNullObj]
    public class Holder : IHolder<Secret>, IHolder<List<Secret[]>>, IHolder<(int Number, Secret Value)>, IOpen
    {
        void IHolder<Secret>.Put(Secret item) { }
        Secret IHolder<Secret>.Item => null;
        event System.Action<Secret> IHolder<Secret>.Changed { add { } remove { } }
        void IHolder<List<Secret[]>>.Put(List<Secret[]> item) { }
        List<Secret[]> IHolder<List<Secret[]>>.Item => null;
        event System.Action<List<Secret[]>> IHolder<List<Secret[]>>.Changed { add { } remove { } }
        void IHolder<(int Number, Secret Value)>.Put((int Number, Secret Value) item) { }
        (int Number, Secret Value) IHolder<(int Number, Secret Value)>.Item => default;
        event System.Action<(int Number, Secret Value)> IHolder<(int Number, Secret Value)>.Changed { add { } remove { } }
        public void Run() { }
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var generated = result.Source("Sample.Holder.AsNullObj.g.cs");
            Assert.StartsWith("public class HolderAsNullObj", generated.Substring(generated.IndexOf("public class")));
            Assert.Contains("void global::Sample.IHolder<global::Sample.Secret>.Put(global::Sample.Secret item)", generated);
            Assert.Contains("public void Run()", generated);
            Assert.DoesNotContain("public void Put", generated);
        }

        [Fact]
        public void Generate_ConstraintWithInternalType_ShouldImplementExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    internal class Secret { }

    public interface IHolder<T>
    {
        void Put<U>(U item) where U : T;
    }

    [InheritsToNullObj]
    public class Holder : IHolder<Secret>
    {
        void IHolder<Secret>.Put<U>(U item) { }
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("void global::Sample.IHolder<global::Sample.Secret>.Put<U>(U item)", result.Source("Sample.Holder.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_ConstraintBecomingSealedOrValueType_ShouldImplementExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IConvert<T>
    {
        U As<U>(T value) where U : T;
    }

    [InterfaceToNullObj]
    public interface INumbers : IConvert<int> { }

    [InterfaceToNullObj]
    public interface ITexts : IConvert<string> { }

    [InterfaceToNullObj]
    public interface IArrays : IConvert<int[]> { }

    [InterfaceToNullObj]
    public interface IObjects : IConvert<object> { }

    [InterfaceToNullObj]
    public interface IErrors : IConvert<System.Exception> { }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("U global::Sample.IConvert<int>.As<U>(int value)", result.Source("Sample.INumbers.AsNullObj.g.cs"));
            Assert.Contains("U global::Sample.IConvert<string>.As<U>(string value)", result.Source("Sample.ITexts.AsNullObj.g.cs"));
            Assert.Contains("U global::Sample.IConvert<int[]>.As<U>(int[] value)", result.Source("Sample.IArrays.AsNullObj.g.cs"));
            Assert.Contains("U global::Sample.IConvert<object>.As<U>(object value)", result.Source("Sample.IObjects.AsNullObj.g.cs"));
            Assert.Contains("public U As<U>(global::System.Exception value)", result.Source("Sample.IErrors.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_MembersShapedLikeObjectMembers_ShouldImplementThemExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface ILooksLikeObject
    {
        string ToString();
        int GetHashCode();
        bool Equals(object other);
        string ToString(string format);
        int GetType { get; }
    }

    [InheritsToNullObj]
    public class Comparer : System.Collections.IEqualityComparer
    {
        public new bool Equals(object x, object y) => false;
        public int GetHashCode(object obj) => 0;
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var looks = result.Source("Sample.ILooksLikeObject.AsNullObj.g.cs");
            Assert.Contains("string global::Sample.ILooksLikeObject.ToString()", looks);
            Assert.Contains("int global::Sample.ILooksLikeObject.GetHashCode()", looks);
            Assert.Contains("bool global::Sample.ILooksLikeObject.Equals(object other)", looks);
            Assert.Contains("public string ToString(string format)", looks);
            Assert.Contains("int global::Sample.ILooksLikeObject.GetType", looks);
            var comparer = result.Source("Sample.Comparer.AsNullObj.g.cs");
            Assert.Contains("bool global::System.Collections.IEqualityComparer.Equals(object x, object y)", comparer);
            Assert.Contains("public int GetHashCode(object obj)", comparer);
        }

        [Fact]
        public void Generate_MethodTypeParameterShadowingInterfaceOne_ShouldNotWarn()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
#pragma warning disable CS0693
    [InterfaceToNullObj]
    public interface IShadow<T>
    {
        void Run<T>(T value);
    }
#pragma warning restore CS0693
}";
            GeneratorRunner.Run(source).AssertCompilesCleanly();
        }

        [Fact]
        public void Generate_InternalTypeArgumentOfContainingType_ShouldImplementExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    internal class Secret { }

    public class Box<T>
    {
        public interface IIn
        {
            void Put(IIn other);
            IIn Self { get; }
        }
    }

    [InheritsToNullObj]
    public class Holder : Box<Secret>.IIn
    {
        void Box<Secret>.IIn.Put(Box<Secret>.IIn other) { }
        Box<Secret>.IIn Box<Secret>.IIn.Self => null;
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("void global::Sample.Box<global::Sample.Secret>.IIn.Put(", result.Source("Sample.Holder.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_AccessorAndIndexerNamesFromOtherInterfaces_ShouldImplementTheLaterOnesExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IItem { int Item { get; } }
    public interface IIndexed { int this[int index] { get; } }
    public interface IValue { int Value { get; set; } }
    public interface IAccessors { int get_Value(); void set_Value(int value); }
    public interface IChanged { event System.Action Changed; }
    public interface IChangedMethods { void add_Changed(System.Action handler); }
    public interface IItemMethods { int get_Item(int index); }

    [InterfaceToNullObj]
    public interface IAll : IItem, IIndexed, IValue, IAccessors, IChanged, IChangedMethods { }

    [InterfaceToNullObj]
    public interface IIndexerFirst : IIndexed, IItem, IItemMethods { }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var all = result.Source("Sample.IAll.AsNullObj.g.cs");
            Assert.Contains("public int Item", all);
            Assert.Contains("int global::Sample.IIndexed.this[int index]", all);
            Assert.Contains("public int Value", all);
            Assert.Contains("int global::Sample.IAccessors.get_Value()", all);
            Assert.Contains("void global::Sample.IChangedMethods.add_Changed(", all);
            var indexerFirst = result.Source("Sample.IIndexerFirst.AsNullObj.g.cs");
            Assert.Contains("public int this[int index]", indexerFirst);
            Assert.Contains("int global::Sample.IItem.Item", indexerFirst);
            Assert.Contains("int global::Sample.IItemMethods.get_Item(int index)", indexerFirst);
        }

        [Fact]
        public void Generate_ClassConstraintNextToSubstitutedClass_ShouldImplementExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IConvert<T>
    {
        U As<U>(T value) where U : class, T;
        V Make<V>() where V : T, new();
    }

    [InterfaceToNullObj]
    public interface IErrors : IConvert<System.Exception> { }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var generated = result.Source("Sample.IErrors.AsNullObj.g.cs");
            Assert.Contains("U global::Sample.IConvert<global::System.Exception>.As<U>(global::System.Exception value)", generated);
            Assert.Contains("public V Make<V>()", generated);
        }

        [Fact]
        public void Generate_ObsoleteInterfaceAndTypes_ShouldNotWarnInGeneratedCode()
        {
            const string source = @"
using System;
using NullObjectGenerator;

namespace Sample
{
    [Obsolete]
    public class Old { }

    [Obsolete(""use something else"")]
    public interface IOld
    {
        Old Get();
    }

#pragma warning disable CS0612, CS0618
    [InterfaceToNullObj]
    public interface IUsesOld : IOld
    {
        void Put(Old value);
    }

    [InheritsToNullObj]
    public class OldUser : IOld
    {
        public Old Get() => null;
    }
#pragma warning restore CS0612, CS0618
}";
            GeneratorRunner.Run(source).AssertCompilesCleanly();
        }

        [Fact]
        public void Generate_WithDocumentationDiagnostics_ShouldNotWarnCs1591InGeneratedCode()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    /// <summary>Runs.</summary>
    [InterfaceToNullObj]
    public interface IRun
    {
        /// <summary>Runs once.</summary>
        void Run();
    }

    public class Undocumented { }
}";
            var parseOptions = GeneratorRunner.ParseOptions.WithDocumentationMode(Microsoft.CodeAnalysis.DocumentationMode.Diagnose);
            var result = GeneratorRunner.Run(new[] { source }, GeneratorRunner.DefaultAssemblyName, new LibraryStub[0], parseOptions).AssertCompilesCleanly();

            Assert.Contains(result.Output.GetDiagnostics(), diagnostic => diagnostic.Id == "CS1591" && diagnostic.Location.SourceTree!.FilePath == "Source0.cs");
            Assert.Contains("Sample.IRun.AsNullObj.g.cs", result.HintNames);
        }

        [Fact]
        public void Generate_PartialInterfaceInSeveralFiles_ShouldImplementMembersOfEveryPart()
        {
            const string attributed = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public partial interface ISplit
    {
        void First();
    }
}";
            const string other = @"
namespace Sample
{
    public partial interface ISplit
    {
        int Second();
    }
}";
            var result = GeneratorRunner.Run(new[] { other, attributed }, GeneratorRunner.DefaultAssemblyName, new LibraryStub[0], GeneratorRunner.ParseOptions)
                .AssertCompilesCleanly();

            var generated = result.Source("Sample.ISplit.AsNullObj.g.cs");
            Assert.Contains("public void First()", generated);
            Assert.Contains("public int Second()", generated);
        }
    }
}
