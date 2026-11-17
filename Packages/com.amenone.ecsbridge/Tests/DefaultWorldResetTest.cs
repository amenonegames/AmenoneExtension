using NUnit.Framework;

namespace Amenone.EcsBridge.Tests
{
    public sealed class DefaultWorldResetTest : DefaultWorldFixture
    {
        [Test]
        public void Reset_WhenDefaultWorldExists_ShouldDisposeItAndCreateAnotherDefaultWorld()
        {
            var previousWorld = DefaultWorld;

            new DefaultWorldReset().Reset();
            var recreatedWorld = DefaultWorld;
            DisposeOnTearDown(recreatedWorld);

            Assert.That(previousWorld.IsCreated, Is.False);
            Assert.That(recreatedWorld, Is.Not.Null.And.Not.SameAs(previousWorld));
            Assert.That(recreatedWorld.IsCreated, Is.True);
        }

        [Test]
        public void Read_AfterReset_ShouldFollowRecreatedWorld()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(new TestValue { Value = 1 });
            singleton.Write(new TestValue { Value = 5 });

            new DefaultWorldReset().Reset();
            DisposeOnTearDown(DefaultWorld);

            Assert.That(singleton.Read().Value, Is.EqualTo(1));
        }
    }
}
