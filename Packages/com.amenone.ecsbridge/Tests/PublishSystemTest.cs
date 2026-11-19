using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

namespace Amenone.EcsBridge.Tests
{
    public sealed class PublishSystemTest
    {
        private World _world;
        private RecordingPublishSystem _system;

        [SetUp]
        public void CreateSystem()
        {
            _world = new World(nameof(PublishSystemTest));
            _system = _world.CreateSystemManaged<RecordingPublishSystem>();
        }

        [TearDown]
        public void DisposeWorld() => _world.Dispose();

        [Test]
        public void Update_BeforeConstruct_ShouldNotPublishChanges()
        {
            _system.Update();

            Assert.That(_system.PublishCount, Is.Zero);
        }

        [Test]
        public void Update_AfterConstruct_ShouldPublishChangesEveryUpdate()
        {
            _system.Construct(new TestPublisher());

            _system.Update();
            _system.Update();

            Assert.That(_system.PublishCount, Is.EqualTo(2));
        }

        [Test]
        public void Construct_ShouldCallOnPublisherInjectedAfterSettingPublisher()
        {
            var publisher = new TestPublisher();

            _system.Construct(publisher);

            Assert.That(_system.InjectedPublishers, Is.EqualTo(new[] { publisher }));
        }

        [Test]
        public void Construct_CalledTwice_ShouldReplacePublisherAndCallOnPublisherInjectedEachTime()
        {
            var first = new TestPublisher();
            var second = new TestPublisher();

            _system.Construct(first);
            _system.Construct(second);
            _system.Update();

            Assert.That(_system.InjectedPublishers, Is.EqualTo(new[] { first, second }));
            Assert.That(_system.PublishedWith, Is.SameAs(second));
        }
    }

    public sealed class TestPublisher
    {
    }

    [DisableAutoCreation]
    public partial class RecordingPublishSystem : PublishSystem<TestPublisher>
    {
        public int PublishCount { get; private set; }
        public TestPublisher PublishedWith { get; private set; }
        public List<TestPublisher> InjectedPublishers { get; } = new();

        protected override void OnPublisherInjected() => InjectedPublishers.Add(Publisher);

        protected override void PublishChanges()
        {
            PublishCount++;
            PublishedWith = Publisher;
        }
    }
}
