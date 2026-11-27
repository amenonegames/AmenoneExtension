using Xunit;

namespace Amenone.NullObjectGenerator.Tests
{
    public sealed class MetadataReferenceTests
    {
        private static readonly LibraryStub Abstractions = new LibraryStub("Sample.Abstractions", @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Sample.Abstractions
{
    public struct Settings
    {
        public int Level;
    }

    public interface IRemote
    {
        event Action<Settings> Changed;
        Settings Current { get; set; }
        string this[int index] { get; }
        Task SaveAsync(Settings settings, bool force = false);
        Task<IReadOnlyList<Settings>> LoadAsync();
        UniTask<Settings> LoadUniAsync();
        T Convert<T>(Settings settings) where T : struct;
        bool TryFind(string key, out Settings found);
    }

    public interface IRemote<T>
    {
        T Value { get; }
    }
}", LibraryStub.UniTask);

        [Fact]
        public void Generate_ClassImplementingReferencedInterface_ShouldImplementEveryMember()
        {
            const string source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NullObjectGenerator;
using Sample.Abstractions;

namespace Sample.Game
{
    [InheritsToNullObj]
    public class Remote : IRemote, IRemote<int>
    {
        public event Action<Settings> Changed;
        public Settings Current { get; set; }
        public string this[int index] => null;
        public int Value => 0;
        public Task SaveAsync(Settings settings, bool force = false) => Task.CompletedTask;
        public Task<IReadOnlyList<Settings>> LoadAsync() => null;
        public UniTask<Settings> LoadUniAsync() => default;
        public T Convert<T>(Settings settings) where T : struct => default;
        public bool TryFind(string key, out Settings found) { found = default; return false; }
    }

    public static class Usage
    {
        public static IRemote Create() => new RemoteAsNullObj();
    }
}";
            var result = GeneratorRunner.Run(source, Abstractions).AssertCompilesCleanly();

            Assert.Contains(
                "public class RemoteAsNullObj : global::Sample.Abstractions.IRemote, global::Sample.Abstractions.IRemote<int>",
                result.Source("Sample.Game.Remote.AsNullObj.g.cs"));
        }

        [Fact]
        public void Generate_InterfaceDerivingFromReferencedInterface_ShouldImplementInheritedMembers()
        {
            const string source = @"
using NullObjectGenerator;
using Sample.Abstractions;

namespace Sample.Game
{
    [InterfaceToNullObj]
    public interface ILocal : IRemote, IRemote<string>
    {
        void Local();
    }
}";
            var result = GeneratorRunner.Run(source, Abstractions).AssertCompilesCleanly();

            Assert.Contains("TryFind(string key, out global::Sample.Abstractions.Settings found)", result.Source("Sample.Game.ILocal.AsNullObj.g.cs"));
        }
    }
}
