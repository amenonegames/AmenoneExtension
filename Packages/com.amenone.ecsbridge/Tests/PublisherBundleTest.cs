using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using VContainer;
using VContainer.Unity;

namespace Amenone.EcsBridge.Tests
{
    public sealed class PublisherBundleTest : DefaultWorldFixture
    {
        private RecordingMessagePublisher<int> _numbers;
        private RecordingMessagePublisher<string> _texts;
        private TwoKindsPublishSystem _system;

        [SetUp]
        public void CreateSystemAndPublishers()
        {
            _system = DefaultWorld.CreateSystemManaged<TwoKindsPublishSystem>();
            _numbers = new RecordingMessagePublisher<int>();
            _texts = new RecordingMessagePublisher<string>();
        }

        [Test]
        public void PublishChanges_WithBundleInjectedByContainer_ShouldPublishBothKinds()
        {
            using var container = BuildContainerInjectingSystem(_numbers, _texts);

            _system.Update();

            Assert.That(_numbers.Messages, Is.EqualTo(new[] { 1 }));
            Assert.That(_texts.Messages, Is.EqualTo(new[] { "changed" }));
        }

        [Test]
        public void Construct_ByContainer_ShouldPassBundleBuiltFromRegisteredPublishers()
        {
            using var container = BuildContainerInjectingSystem(_numbers, _texts);

            var injected = _system.InjectedBundles;

            Assert.That(injected, Is.EqualTo(new[] { container.Resolve<TwoKindsPublishers>() }));
            Assert.That(injected[0].Number, Is.SameAs(_numbers));
            Assert.That(injected[0].Text, Is.SameAs(_texts));
        }

        [Test]
        public void Construct_ByRebuiltContainer_ShouldReplaceBundleAndCallOnPublisherInjectedAgain()
        {
            var firstContainer = BuildContainerInjectingSystem(_numbers, _texts);
            var firstBundle = firstContainer.Resolve<TwoKindsPublishers>();
            firstContainer.Dispose();
            var nextNumbers = new RecordingMessagePublisher<int>();
            var nextTexts = new RecordingMessagePublisher<string>();

            using var nextContainer = BuildContainerInjectingSystem(nextNumbers, nextTexts);
            _system.Update();

            Assert.That(_system.InjectedBundles, Is.EqualTo(new[] { firstBundle, nextContainer.Resolve<TwoKindsPublishers>() }));
            Assert.That(_numbers.Messages, Is.Empty);
            Assert.That(nextNumbers.Messages, Is.EqualTo(new[] { 1 }));
            Assert.That(nextTexts.Messages, Is.EqualTo(new[] { "changed" }));
        }

        private static IObjectResolver BuildContainerInjectingSystem(IMessagePublisher<int> numbers, IMessagePublisher<string> texts)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(numbers);
            builder.RegisterInstance(texts);
            builder.Register<TwoKindsPublishers>(Lifetime.Singleton);
            builder.RegisterSystemFromDefaultWorld<TwoKindsPublishSystem>();
            return builder.Build();
        }
    }

    public interface IMessagePublisher<in T>
    {
        void Publish(T message);
    }

    public sealed class RecordingMessagePublisher<T> : IMessagePublisher<T>
    {
        public List<T> Messages { get; } = new();

        public void Publish(T message) => Messages.Add(message);
    }

    public sealed class TwoKindsPublishers
    {
        public readonly IMessagePublisher<int> Number;
        public readonly IMessagePublisher<string> Text;

        public TwoKindsPublishers(IMessagePublisher<int> number, IMessagePublisher<string> text)
        {
            Number = number;
            Text = text;
        }
    }

    [DisableAutoCreation]
    public partial class TwoKindsPublishSystem : PublishSystem<TwoKindsPublishers>
    {
        public List<TwoKindsPublishers> InjectedBundles { get; } = new();

        protected override void OnPublisherInjected() => InjectedBundles.Add(Publisher);

        protected override void PublishChanges()
        {
            Publisher.Number.Publish(1);
            Publisher.Text.Publish("changed");
        }
    }
}
