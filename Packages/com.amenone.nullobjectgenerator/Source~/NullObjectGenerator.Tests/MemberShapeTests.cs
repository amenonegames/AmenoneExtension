using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class MemberShapeTests
    {
        [Fact]
        public void Generate_EventsIndexersAndAccessors_ShouldCompile()
        {
            const string source = @"
using System;
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj(NullObjLog.DebugLog)]
    public interface IShapes
    {
        event Action Changed;
        event EventHandler<string> Named;
        int this[int index] { get; set; }
        string this[string key, int offset = 2] { get; }
        int ReadOnly { get; }
        int WriteOnly { set; }
        int ReadWrite { get; set; }
        string InitOnly { get; init; }
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var generated = result.Source("Sample.IShapes.AsNullObj.g.cs");
            Assert.Contains("public event global::System.Action Changed", generated);
            Assert.Contains("public int this[int index]", generated);
            Assert.Contains("public string this[string key, int offset = 2]", generated);
            Assert.Contains("init", generated);
        }

        [Fact]
        public void Call_ParameterModifiersAndDefaults_ShouldAssignEveryOut()
        {
            const string source = @"
using System.Threading;
using NullObjectGenerator;

namespace Sample
{
    public enum Mode { First, Second = 5 }

    [InterfaceToNullObj]
    public interface IParameters
    {
        bool TryGet(int key, out string first, out int second);
        void Swap(ref int left, ref int right);
        void Read(in System.DateTime time);
        void Many(params int[] values);
        void Defaults(int number = 3, string text = ""a\""b"", float ratio = 1.5f, double size = 2, decimal money = 1.25m,
            long big = -4, char letter = 'x', bool flag = true, Mode mode = Mode.Second, int? maybe = null,
            CancellationToken token = default, object anything = null, double nan = double.NaN, uint count = 7);
        void @class(int @event);
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IParametersAsNullObj");
            var arguments = new object?[] { 1, "x", 9 };

            var found = assembly.Invoke(instance, "Sample.IParameters", "TryGet", arguments);

            Assert.Equal(false, found);
            Assert.Null(arguments[1]);
            Assert.Equal(0, arguments[2]);
            Assert.Contains("global::Sample.Mode mode = (global::Sample.Mode)(5)", result.Source("Sample.IParameters.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_GenericMethodsWithConstraints_ShouldCompile()
        {
            const string source = @"
using System;
using System.Collections.Generic;
using NullObjectGenerator;

namespace Sample
{
    public abstract class Base { }

    [InterfaceToNullObj]
    public interface IGenericMethods
    {
        T Create<T>() where T : class, new();
        void Value<T>(T value) where T : struct;
        void Unmanaged<T>(T value) where T : unmanaged;
        TBase Pick<TBase, TItem>(IEnumerable<TItem> items) where TBase : Base where TItem : TBase, IComparable<TItem>;
        void NotNull<T>(T value) where T : notnull;
        T? Maybe<T>(T? value);
        void Kind<T>(T value) where T : Enum;
    }
}";
            GeneratorRunner.Run(source).AssertCompilesCleanly();
        }

        [Fact]
        public void Generate_SameSignatureInSeveralInterfaces_ShouldImplementTheRestExplicitly()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface ILeft
    {
        void Run();
        int Value { get; }
        string Name { get; }
        int this[int index] { get; }
        event System.Action Changed;
    }

    public interface IRight
    {
        void Run();
        string Value { get; }
        int Name();
        string this[int index] { get; }
        event System.Action Changed;
    }

    [InterfaceToNullObj]
    public interface IBoth : ILeft, IRight
    {
        new void Run();
    }

    [InheritsToNullObj]
    public class Both : ILeft, IRight
    {
        public void Run() { }
        public int Value => 0;
        string IRight.Value => null;
        public string Name => null;
        int IRight.Name() => 0;
        public int this[int index] => 0;
        string IRight.this[int index] => null;
        public event System.Action Changed;
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            var generated = result.Source("Sample.IBoth.AsNullObj.g.cs");
            Assert.Contains("public void Run()", generated);
            Assert.Contains("void global::Sample.ILeft.Run()", generated);
            Assert.Contains("void global::Sample.IRight.Run()", generated);
            Assert.Contains("string global::Sample.IRight.Value", generated);
            Assert.Contains("int global::Sample.IRight.Name()", generated);
        }

        [Fact]
        public void Generate_InheritedAndGenericInterfaces_ShouldCompile()
        {
            const string source = @"
using System.Collections.Generic;
using NullObjectGenerator;

namespace Sample
{
    public interface IRoot
    {
        void Root();
    }

    public interface IMiddle : IRoot
    {
        void Middle();
    }

    [InterfaceToNullObj]
    public interface ILeaf : IMiddle
    {
        void Leaf();
    }

    [InterfaceToNullObj]
    public interface IRepository<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
        where TValue : class, new()
    {
        TValue Find(TKey key);
        (TKey key, TValue value) Pair();
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("public class ILeafAsNullObj : global::Sample.ILeaf", result.Source("Sample.ILeaf.AsNullObj.g.cs"));
            Assert.Contains("public class IRepositoryAsNullObj<TKey, TValue>", result.Source("Sample.IRepository`2.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_NestedAndInternalTypes_ShouldKeepNamespaceAndNarrowestAccessibility()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public class Outer<T>
    {
        [InterfaceToNullObj]
        public interface IInner
        {
            T Get();
        }
    }

    internal class Hidden
    {
        [InterfaceToNullObj]
        public interface IHiddenInner
        {
            void Run();
        }
    }

    [InterfaceToNullObj]
    internal interface IInternal
    {
        void Run();
    }
}

[InterfaceToNullObj]
public interface IGlobal
{
    void Run();
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();

            Assert.Contains("public class IInnerAsNullObj<T> : global::Sample.Outer<T>.IInner", result.Source("Sample.Outer`1+IInner.AsNullObj.g.cs"));
            Assert.Contains("internal class IHiddenInnerAsNullObj", result.Source("Sample.Hidden+IHiddenInner.AsNullObj.g.cs"));
            Assert.Contains("internal class IInternalAsNullObj", result.Source("Sample.IInternal.AsNullObj.g.cs"));
            Assert.StartsWith("// <auto-generated/>\n#nullable disable\n#pragma warning disable CS0612, CS0618, CS0693, CS1591\n\npublic class IGlobalAsNullObj", result.Source("IGlobal.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_PointerMembers_ShouldMarkTheClassUnsafe()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public unsafe interface IPointers
    {
        int* Address(byte* data);
    }
}";
            var result = GeneratorRunner.Run(new[] { source }, GeneratorRunner.DefaultAssemblyName, new LibraryStub[0], GeneratorRunner.ParseOptions, allowUnsafe: true)
                .AssertCompilesCleanly();

            Assert.Contains("public unsafe class IPointersAsNullObj", result.Source("Sample.IPointers.AsNullObj.g.cs"));
        }

        [Fact]
        public void Call_DefaultInterfaceMembers_ShouldBeOverriddenAndPrivateOrStaticOnesSkipped()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    [InterfaceToNullObj]
    public interface IWithBodies
    {
        int Abstract();
        int Virtual() => Helper();
        private int Helper() => 1;
        static int Shared() => 2;
        sealed int Fixed() => 3;
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IWithBodiesAsNullObj");

            Assert.Equal(0, assembly.Invoke(instance, "Sample.IWithBodies", "Virtual"));
            Assert.Equal(3, assembly.Invoke(instance, "Sample.IWithBodies", "Fixed"));
        }
    }
}
