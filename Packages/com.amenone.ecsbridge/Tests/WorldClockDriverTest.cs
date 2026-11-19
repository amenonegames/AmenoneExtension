using NUnit.Framework;
using Unity.Entities;

namespace Amenone.EcsBridge.Tests
{
    public sealed class WorldClockDriverTest : DefaultWorldFixture
    {
        private TestDeltaTimeSource _deltaTimeSource;
        private WorldClockDriver _driver;

        [SetUp]
        public void CreateDriver()
        {
            _deltaTimeSource = new TestDeltaTimeSource { DeltaSeconds = 0.25f };
            _driver = new WorldClockDriver(_deltaTimeSource);
        }

        [Test]
        public void PostTick_AfterStart_ShouldAdvanceWorldTimeBySourceDelta()
        {
            var startElapsed = DefaultWorld.Time.ElapsedTime;
            _driver.Start();

            _driver.PostTick();
            _driver.PostTick();

            Assert.That(DefaultWorld.Time.ElapsedTime, Is.EqualTo(startElapsed + 0.5).Within(1e-6));
            Assert.That(DefaultWorld.Time.DeltaTime, Is.EqualTo(0.25f).Within(1e-6));
        }

        [Test]
        public void PostTick_WhenDeltaExceedsMaximum_ShouldClampToMaximumDeltaTime()
        {
            DefaultWorld.MaximumDeltaTime = 0.1f;
            _driver.Start();
            _deltaTimeSource.DeltaSeconds = 1f;

            _driver.PostTick();

            Assert.That(DefaultWorld.Time.DeltaTime, Is.EqualTo(0.1f).Within(1e-6));
        }

        [Test]
        public void PostTick_BeforeStart_ShouldLeaveWorldTimeUnchanged()
        {
            var startElapsed = DefaultWorld.Time.ElapsedTime;

            _driver.PostTick();

            Assert.That(DefaultWorld.Time.ElapsedTime, Is.EqualTo(startElapsed));
        }

        [Test]
        public void PostTick_AfterDispose_ShouldLeaveWorldTimeUnchanged()
        {
            _driver.Start();
            _driver.Dispose();
            var startElapsed = DefaultWorld.Time.ElapsedTime;

            _driver.PostTick();

            Assert.That(DefaultWorld.Time.ElapsedTime, Is.EqualTo(startElapsed));
        }

        [Test]
        public void PostTick_AfterWorldDisposed_ShouldDoNothing()
        {
            _driver.Start();
            DefaultWorld.Dispose();

            Assert.That(() => _driver.PostTick(), Throws.Nothing);
        }

        [Test]
        public void Dispose_AfterWorldDisposed_ShouldNotThrow()
        {
            _driver.Start();
            DefaultWorld.Dispose();

            Assert.That(() => _driver.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Start_WhenUpdateWorldTimeSystemExists_ShouldDisableIt()
        {
            var updateWorldTimeSystem = DefaultWorld.CreateSystemManaged<UpdateWorldTimeSystem>();

            _driver.Start();

            Assert.That(updateWorldTimeSystem.Enabled, Is.False);
        }

        [Test]
        public void Dispose_AfterStart_ShouldEnableUpdateWorldTimeSystemAgain()
        {
            var updateWorldTimeSystem = DefaultWorld.CreateSystemManaged<UpdateWorldTimeSystem>();
            _driver.Start();

            _driver.Dispose();

            Assert.That(updateWorldTimeSystem.Enabled, Is.True);
        }

        private sealed class TestDeltaTimeSource : IWorldDeltaTimeSource
        {
            public float DeltaSeconds { get; set; }
        }
    }
}
