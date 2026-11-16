using System;
using Cysharp.Threading.Tasks;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using RootContainer;
using VContainer;

namespace Messaging.Tests
{
    public sealed class DialogueMessagingExtensionsTest
    {
        private IObjectResolver _root;

        [SetUp]
        public void SetUp() => _root = BuildDialogueMessagingRoot();

        [TearDown]
        public void TearDown() => _root.Dispose();

        [Test]
        public void RegisterDialogueMessaging_BrokersRegisteredInParentScope_ShouldReuseParentBrokers()
        {
            using var gameRoot = BuildGameRootWithDialogueBrokers();
            using var libraryRoot = gameRoot.CreateScope(builder => builder.RegisterDialogueMessaging());

            Assert.That(libraryRoot.Resolve<MessagePipeOptions>(), Is.SameAs(gameRoot.Resolve<MessagePipeOptions>()));
            Assert.That(libraryRoot.Resolve<IAsyncPublisher<NodeStart>>(), Is.SameAs(gameRoot.Resolve<IAsyncPublisher<NodeStart>>()));
        }

        [Test]
        public void RegisterDialogueMessaging_OnlyOptionsRegisteredInParentScope_ShouldThrowWithHowToFix()
        {
            var gameBuilder = new ContainerBuilder();
            gameBuilder.RegisterInstance(new MessagePipeOptions());
            using var gameRoot = gameBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => gameRoot.CreateScope(builder => builder.RegisterDialogueMessaging()));

            Assert.That(exception.Message, Does.Contain("RegisterMessagePipe"));
            Assert.That(exception.Message, Does.Contain("RegisterDialogueMessageBrokers(options)"));
        }

        [Test]
        public void RegisterDialogueMessaging_NothingRegisteredInAnyScope_ShouldRegisterOwnMessagePipeAndBrokers()
        {
            using var bareRoot = new ContainerBuilder().Build();
            using var libraryRoot = bareRoot.CreateScope(builder => builder.RegisterDialogueMessaging());

            Assert.That(libraryRoot.Resolve<MessagePipeOptions>(), Is.Not.Null);
            Assert.That(libraryRoot.Resolve<IAsyncPublisher<NodeStart>>(), Is.Not.Null);
            Assert.That(libraryRoot.Resolve<IAsyncSubscriber<NodeStart>>(), Is.Not.Null);
        }

        [Test]
        public void RegisterDialogueMessageBrokers_ScopedOptions_ShouldThrowWithReason()
        {
            var builder = new ContainerBuilder();
            var options = builder.RegisterMessagePipe(x => x.InstanceLifetime = InstanceLifetime.Scoped);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.RegisterDialogueMessageBrokers(options));

            Assert.That(exception.Message, Does.Contain("Singleton"));
            Assert.That(exception.Message, Does.Contain("Scoped"));
        }

        [Test]
        public void PublishAsync_SubscriberStillRunning_ShouldCompleteOnlyAfterSubscriberFinishes()
        {
            var subscriberFinish = new UniTaskCompletionSource();
            using var subscription = Subscriber<NodeEnd>().Subscribe((_, _) => subscriberFinish.Task);

            var publishing = Publisher<NodeEnd>().PublishAsync(new NodeEnd());
            Assert.That(publishing.Status, Is.EqualTo(UniTaskStatus.Pending));

            subscriberFinish.TrySetResult();
            Assert.That(publishing.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void Publish_SubscriberStillRunning_ShouldReturnBeforeSubscriberFinishes()
        {
            var subscriberFinish = new UniTaskCompletionSource();
            var isSubscriberStarted = false;
            var isSubscriberFinished = false;
            using var subscription = Subscriber<NodeEnd>().Subscribe(async (_, _) =>
            {
                isSubscriberStarted = true;
                await subscriberFinish.Task;
                isSubscriberFinished = true;
            });

            Publisher<NodeEnd>().Publish(new NodeEnd());
            Assert.That(isSubscriberStarted, Is.True);
            Assert.That(isSubscriberFinished, Is.False);

            subscriberFinish.TrySetResult();
            Assert.That(isSubscriberFinished, Is.True);
        }

        [Test]
        public void Publish_AfterSubscriptionDisposed_ShouldNotCallSubscriber()
        {
            var callCount = 0;
            var subscription = Subscriber<NodeEnd>().Subscribe((_, _) =>
            {
                callCount++;
                return UniTask.CompletedTask;
            });

            Publisher<NodeEnd>().Publish(new NodeEnd());
            subscription.Dispose();
            Publisher<NodeEnd>().Publish(new NodeEnd());

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_BrokersRegisteredInGameRootScope_ShouldReachSubscriberInAnotherLibraryScope()
        {
            using var gameRoot = BuildGameRootWithDialogueBrokers();
            using var libraryRoot = gameRoot.CreateScope(builder => builder.RegisterDialogueMessaging());
            using var publisherScope = libraryRoot.CreateScope();
            using var subscriberScope = gameRoot.CreateScope();
            NodeStartContext? received = null;
            using var subscription = subscriberScope.Resolve<IAsyncSubscriber<NodeStart>>().Subscribe((message, _) =>
            {
                received = message.Context;
                return UniTask.CompletedTask;
            });

            publisherScope.Resolve<IAsyncPublisher<NodeStart>>().Publish(new NodeStart(new NodeStartContext("START")));

            Assert.That(received, Is.EqualTo(new NodeStartContext("START")));
        }

        private static IObjectResolver BuildDialogueMessagingRoot()
        {
            var builder = new ContainerBuilder();
            builder.RegisterDialogueMessaging();
            return builder.Build();
        }

        private static IObjectResolver BuildGameRootWithDialogueBrokers()
        {
            var builder = new ContainerBuilder();
            var options = builder.RegisterMessagePipe();
            builder.RegisterDialogueMessageBrokers(options);
            return builder.Build();
        }

        private IAsyncPublisher<T> Publisher<T>() => _root.Resolve<IAsyncPublisher<T>>();

        private IAsyncSubscriber<T> Subscriber<T>() => _root.Resolve<IAsyncSubscriber<T>>();
    }
}
