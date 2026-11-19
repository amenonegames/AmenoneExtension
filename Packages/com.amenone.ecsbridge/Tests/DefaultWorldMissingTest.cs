using System;
using NUnit.Framework;
using Unity.Entities;

namespace Amenone.EcsBridge.Tests
{
    public sealed class DefaultWorldMissingTest : DefaultWorldFixture
    {
        [SetUp]
        public void RemoveDefaultWorld() => World.DefaultGameObjectInjectionWorld = null;

        [Test]
        public void SingletonRead_WhenDefaultWorldIsNull_ShouldThrowInvalidOperation()
            => AssertThrowsBecauseOfMissingWorld(() => new DefaultWorldSingleton<TestValue>().Read());

        [Test]
        public void SingletonBuffer_WhenDefaultWorldIsNull_ShouldThrowInvalidOperation()
            => AssertThrowsBecauseOfMissingWorld(() => new DefaultWorldSingletonBuffer<TestElement>().Buffer());

        [Test]
        public void QueryCurrent_WhenDefaultWorldIsNull_ShouldThrowInvalidOperation()
            => AssertThrowsBecauseOfMissingWorld(() => new DefaultWorldQuery(ComponentType.ReadOnly<TestValue>()).Current());

        [Test]
        public void SingletonRead_WhenDefaultWorldIsDisposed_ShouldThrowInvalidOperation()
        {
            var world = new World(nameof(DefaultWorldMissingTest));
            World.DefaultGameObjectInjectionWorld = world;
            world.Dispose();
            World.DefaultGameObjectInjectionWorld = world;

            AssertThrowsBecauseOfMissingWorld(() => new DefaultWorldSingleton<TestValue>().Read());
        }

        private static void AssertThrowsBecauseOfMissingWorld(TestDelegate code)
            => Assert.That(code, Throws.InvalidOperationException.With.Message.Contains("DefaultGameObjectInjectionWorld"));
    }
}
