using System;
using System.Threading.Tasks;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class ReturnValueTests
    {
        private const string Source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NullObjectGenerator;

namespace Sample
{
    public struct Point
    {
        public int X;
    }

    [InterfaceToNullObj]
    public interface IReturns
    {
        void Nothing();
        int Number();
        Point Value();
        string Text();
        int? MaybeNumber();
        Task Work();
        Task<string> TextLater();
        ValueTask WorkValue();
        ValueTask<int> NumberValue();
        UniTask WorkUni();
        UniTask<int> NumberUni();
        IEnumerable<int> Numbers();
        int[] Array();
        Task TaskProperty { get; }
    }
}";

        [Fact]
        public void Generate_ReturnTypesOfEveryKind_ShouldCompile() =>
            GeneratorRunner.Run(Source, LibraryStub.UniTask).AssertCompilesCleanly();

        [Fact]
        public async Task Call_ReturnTypesOfEveryKind_ShouldReturnDefaultsOrCompletedTasks()
        {
            var result = GeneratorRunner.Run(Source, LibraryStub.UniTask).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IReturnsAsNullObj");
            object? Call(string name) => assembly.Invoke(instance, "Sample.IReturns", name);

            Assert.Null(Call("Nothing"));
            Assert.Equal(0, Call("Number"));
            Assert.Null(Call("Text"));
            Assert.Null(Call("MaybeNumber"));
            Assert.True(((Task)Call("Work")!).IsCompletedSuccessfully);
            var textLater = (Task<string>)Call("TextLater")!;
            Assert.True(textLater.IsCompletedSuccessfully);
            Assert.Null(await textLater);
            Assert.True(((ValueTask)Call("WorkValue")!).IsCompletedSuccessfully);
            Assert.Equal(0, await (ValueTask<int>)Call("NumberValue")!);
            AssertDefault(assembly, "Value", Call("Value"));
            AssertDefault(assembly, "WorkUni", Call("WorkUni"));
            AssertDefault(assembly, "NumberUni", Call("NumberUni"));
            Assert.Null(Call("Numbers"));
            Assert.Null(Call("Array"));
            Assert.True(((Task)Call("get_TaskProperty")!).IsCompletedSuccessfully);
        }

        private static void AssertDefault(GeneratedAssembly assembly, string methodName, object? returned)
        {
            var returnType = assembly.ReturnType("Sample.IReturns", methodName);
            Assert.True(returnType.IsValueType);
            Assert.Equal(Activator.CreateInstance(returnType), returned);
        }

        [Fact]
        public void Generate_UniTaskReturn_ShouldUseDefault()
        {
            var result = GeneratorRunner.Run(Source, LibraryStub.UniTask).AssertCompilesCleanly();

            var source = result.Source("Sample.IReturns.AsNullObj.g.cs");

            Assert.Contains("public global::Cysharp.Threading.Tasks.UniTask WorkUni()", source);
            Assert.Contains("public global::Cysharp.Threading.Tasks.UniTask<int> NumberUni()", source);
            Assert.Contains("return global::System.Threading.Tasks.Task.FromResult<string>(default);", source);
        }
    }
}
