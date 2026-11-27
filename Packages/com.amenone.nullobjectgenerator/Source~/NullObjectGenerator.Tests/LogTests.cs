using System;
using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class LogTests
    {
        private const string Hint = "Sample.IFoo.AsNullObj.g.cs";

        private static string Source(string attributeArgument) => $@"
using NullObjectGenerator;

namespace Sample
{{
    [InterfaceToNullObj({attributeArgument})]
    public interface IFoo
    {{
        void Execute();
        int Count {{ get; set; }}
    }}
}}";

        [Fact]
        public void Call_AttributeWithoutArgument_ShouldNotLog()
        {
            var result = GeneratorRunner.Run(Source(string.Empty)).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IFooAsNullObj");

            assembly.Invoke(instance, "Sample.IFoo", "Execute");

            Assert.Empty(assembly.UnityMessages());
            Assert.DoesNotContain("UnityEngine", result.Source(Hint));
        }

        [Theory]
        [InlineData("NullObjLog.DebugLog", "Log:Execute is null. do nothing.")]
        [InlineData("NullObjLog.DebugLogErr", "LogError:Execute is null. do nothing.")]
        [InlineData("NullObjLog.DebugLogWarn", "LogWarning:Execute is null. do nothing.")]
        public void Call_AttributeWithUnityLog_ShouldWriteThatLog(string argument, string expected)
        {
            var result = GeneratorRunner.Run(Source(argument)).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IFooAsNullObj");

            assembly.Invoke(instance, "Sample.IFoo", "Execute");

            Assert.Equal(new[] { expected }, assembly.UnityMessages());
        }

        [Fact]
        public void Call_AttributeWithThrowException_ShouldThrowFromMethodsAndAccessors()
        {
            var result = GeneratorRunner.Run(Source("NullObjLog.ThrowException")).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IFooAsNullObj");

            var methodError = Assert.Throws<Exception>(() => assembly.Invoke(instance, "Sample.IFoo", "Execute"));
            var getterError = Assert.Throws<Exception>(() => assembly.Invoke(instance, "Sample.IFoo", "get_Count"));
            var setterError = Assert.Throws<Exception>(() => assembly.Invoke(instance, "Sample.IFoo", "set_Count", 1));

            Assert.Equal("Execute is null. do nothing.", methodError.Message);
            Assert.Equal("Count is null. return default value.", getterError.Message);
            Assert.Equal("Count is null. do nothing.", setterError.Message);
        }

        [Fact]
        public void Call_AttributeWithCombinedFlags_ShouldLogEachInOrder()
        {
            var result = GeneratorRunner.Run(Source("NullObjLog.DebugLog | NullObjLog.DebugLogWarn")).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.IFooAsNullObj");

            assembly.Invoke(instance, "Sample.IFoo", "get_Count");

            Assert.Equal(
                new[] { "Log:Count is null. return default value.", "LogWarning:Count is null. return default value." },
                assembly.UnityMessages());
        }

        [Fact]
        public void Call_InheritsAttributeWithDebugLog_ShouldLog()
        {
            const string source = @"
using NullObjectGenerator;

namespace Sample
{
    public interface IBar
    {
        void Run();
    }

    [InheritsToNullObj(NullObjLog.DebugLog)]
    public class Bar : IBar
    {
        public void Run()
        {
        }
    }
}";
            var result = GeneratorRunner.Run(source).AssertCompilesCleanly();
            var assembly = GeneratedAssembly.Load(result);
            var instance = assembly.Create("Sample.BarAsNullObj");

            assembly.Invoke(instance, "Sample.IBar", "Run");

            Assert.Equal(new[] { "Log:Run is null. do nothing." }, assembly.UnityMessages());
        }
    }
}
