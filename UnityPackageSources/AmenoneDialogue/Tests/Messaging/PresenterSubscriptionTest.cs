using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using Presenter;
using Presenter.DialogueState.Base;
using RootContainer;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using ViewAbstract;

namespace Messaging.Tests
{
    public sealed class PresenterSubscriptionTest
    {
        private const Identifier.DialogueState HandledState = Identifier.DialogueState.VisualizingLine;
        private const string ContextlessCall = "HandleAsync without context";

        private IObjectResolver _root;
        private IScopedObjectResolver _presenterScope;
        private TextFeedButtonSpy _textFeedButton;
        private FastForwardButtonSpy _fastForwardButton;

        [SetUp]
        public void SetUp()
        {
            var rootBuilder = new ContainerBuilder();
            rootBuilder.RegisterDialogueMessaging();
            _root = rootBuilder.Build();
            _textFeedButton = new TextFeedButtonSpy();
            _fastForwardButton = new FastForwardButtonSpy();
        }

        [TearDown]
        public void TearDown()
        {
            _presenterScope?.Dispose();
            _root.Dispose();
        }

        [Test]
        public void InitializeDialoguePresenter_InitializeDialoguePublished_ShouldHandleItInQueue()
        {
            BuildPresenterScope(new RecordingStateHandler<DialogueLineContext>());
            LogAssert.Expect(LogType.Log, "InitializeDialogue command received (async)");

            AssertCompleted(Publisher<InitializeDialogue>().PublishAsync(new InitializeDialogue()));
        }

        [Test]
        public void NodeStartPresenter_NodeStartPublished_ShouldHandleItInQueue()
        {
            BuildPresenterScope(new RecordingStateHandler<DialogueLineContext>());
            LogAssert.Expect(LogType.Log, "NodeStart command received (async)");

            AssertCompleted(Publisher<NodeStart>().PublishAsync(new NodeStart(new NodeStartContext("START"))));
        }

        [Test]
        public void NodeEndPresenter_NodeEndPublished_ShouldHandleItInQueue()
        {
            BuildPresenterScope(new RecordingStateHandler<DialogueLineContext>());
            LogAssert.Expect(LogType.Log, "NodeEnd command received (async)");

            AssertCompleted(Publisher<NodeEnd>().PublishAsync(new NodeEnd()));
        }

        [Test]
        public void DialogueEventPresenter_DialogueEventPublished_ShouldCallHandlerOfNextState()
        {
            var handler = new RecordingStateHandler<DialogueLineContext>();
            BuildPresenterScope(handler);

            AssertCompleted(Publisher<DialogueEventMessage>().PublishAsync(new DialogueEventMessage(DialogueEvent.ContinueRequested)));

            Assert.That(handler.Received, Is.EqualTo(new object[] { ContextlessCall }));
        }

        [Test]
        public void DialogueEventPresenter_InterruptEventPublished_ShouldCallInterruptOfCurrentHandler()
        {
            var handler = new RecordingStateHandler<DialogueLineContext>();
            BuildPresenterScope(handler);

            AssertCompleted(Publisher<DialogueInterruptEventMessage>().PublishAsync(
                new DialogueInterruptEventMessage(DialogueInterruptEvent.FastForwardRequested)));

            Assert.That(handler.Received, Is.EqualTo(new object[] { DialogueInterruptEvent.FastForwardRequested }));
        }

        [Test]
        public void DialogueEventPresenter_LineEventPublished_ShouldPassContextToHandler() =>
            AssertContextReachesHandler(
                context => new DialogueLineEventMessage(DialogueEvent.LineVisualizeRequested, context),
                new DialogueLineContext(new LogicLineData("line", Array.Empty<string>())));

        [Test]
        public void DialogueEventPresenter_OptionEventPublished_ShouldPassContextToHandler() =>
            AssertContextReachesHandler(
                context => new DialogueOptionEventMessage(DialogueEvent.OptionsVisualizeRequested, context),
                new DialogueOptionContext(Array.Empty<LogicOptionData>()));

        [Test]
        public void DialogueEventPresenter_SelectOptionEventPublished_ShouldPassContextToHandler() =>
            AssertContextReachesHandler(
                context => new DialogueSelectOptionEventMessage(DialogueEvent.OptionSelected, context),
                new SelectOptionContext(1));

        [Test]
        public void DialogueEventPresenter_CommandEventPublished_ShouldPassContextToHandler() =>
            AssertContextReachesHandler(
                context => new DialogueCommandEventMessage(DialogueEvent.CommandRunRequested, context),
                new DialogueCommandContext("wait 1"));

        [Test]
        public void InputSwitchPresenter_WaitTextFeedPublished_ShouldActivateOnlyTextFeedButton()
        {
            BuildPresenterScope(new RecordingStateHandler<DialogueLineContext>());
            _fastForwardButton.Activate();

            AssertCompleted(Publisher<InputStateMessage>().PublishAsync(new InputStateMessage(InputState.WaitTextFeed)));

            Assert.That(_textFeedButton.IsActive, Is.True);
            Assert.That(_fastForwardButton.IsActive, Is.False);
        }

        [Test]
        public void Presenters_PresenterScopeDisposed_ShouldStopReceivingMessages()
        {
            var handler = new RecordingStateHandler<DialogueLineContext>();
            BuildPresenterScope(handler);
            _presenterScope.Dispose();
            _presenterScope = null;

            AssertCompleted(Publisher<InitializeDialogue>().PublishAsync(new InitializeDialogue()));
            AssertCompleted(Publisher<NodeStart>().PublishAsync(new NodeStart(new NodeStartContext("START"))));
            AssertCompleted(Publisher<NodeEnd>().PublishAsync(new NodeEnd()));
            AssertCompleted(Publisher<DialogueEventMessage>().PublishAsync(new DialogueEventMessage(DialogueEvent.ContinueRequested)));
            AssertCompleted(Publisher<DialogueInterruptEventMessage>().PublishAsync(
                new DialogueInterruptEventMessage(DialogueInterruptEvent.FastForwardRequested)));
            AssertCompleted(Publisher<DialogueLineEventMessage>().PublishAsync(new DialogueLineEventMessage(
                DialogueEvent.LineVisualizeRequested, new DialogueLineContext(new LogicLineData("line", Array.Empty<string>())))));
            AssertCompleted(Publisher<DialogueOptionEventMessage>().PublishAsync(new DialogueOptionEventMessage(
                DialogueEvent.OptionsVisualizeRequested, new DialogueOptionContext(Array.Empty<LogicOptionData>()))));
            AssertCompleted(Publisher<DialogueSelectOptionEventMessage>().PublishAsync(
                new DialogueSelectOptionEventMessage(DialogueEvent.OptionSelected, new SelectOptionContext(1))));
            AssertCompleted(Publisher<DialogueCommandEventMessage>().PublishAsync(
                new DialogueCommandEventMessage(DialogueEvent.CommandRunRequested, new DialogueCommandContext("wait 1"))));
            AssertCompleted(Publisher<InputStateMessage>().PublishAsync(new InputStateMessage(InputState.WaitTextFeed)));

            Assert.That(handler.Received, Is.Empty);
            Assert.That(_textFeedButton.IsActive, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        private void AssertContextReachesHandler<TMessage, TContext>(Func<TContext, TMessage> createMessage, TContext context)
        {
            var handler = new RecordingStateHandler<TContext>();
            BuildPresenterScope(handler);

            AssertCompleted(Publisher<TMessage>().PublishAsync(createMessage(context)));

            Assert.That(handler.Received, Is.EqualTo(new object[] { context }));
        }

        private void BuildPresenterScope(IDialogueStateHandler handler)
        {
            _presenterScope = _root.CreateScope(builder =>
            {
                builder.Register<DialogueStateQueue>(Lifetime.Singleton);
                builder.Register<CancellationTokenHolder>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.RegisterInstance(new NextStateFixedHolder()).As<IDialogueStateHolder>();
                builder.Register(resolver =>
                {
                    var registry = new DialogueEventHandlerRegistry(resolver.Resolve<IDialogueStateHolder>());
                    registry.Register(handler);
                    return registry;
                }, Lifetime.Singleton);
                builder.RegisterInstance(_textFeedButton).As<ITextFeedButtonActivatable>();
                builder.RegisterInstance(_fastForwardButton).As<ITextFastForwardButtonActivatable>();

                builder.RegisterSubscribingPresenters();
            });
        }

        private IAsyncPublisher<T> Publisher<T>() => _root.Resolve<IAsyncPublisher<T>>();

        private static void AssertCompleted(UniTask task) => Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));

        private sealed class NextStateFixedHolder : IDialogueStateHolder
        {
            public Identifier.DialogueState CurrentState => HandledState;

            public bool CanTransition(DialogueEvent trigger, out Identifier.DialogueState nextState)
            {
                nextState = HandledState;
                return true;
            }

            public bool CanHandleInterrupt(DialogueInterruptEvent interruptEvent) => true;

            public void Transition(Identifier.DialogueState state)
            {
            }
        }

        private sealed class RecordingStateHandler<TContext> : IDialogueStateHandler<TContext>
        {
            public List<object> Received { get; } = new();

            public Identifier.DialogueState State => HandledState;

            public UniTask HandleAsync(CancellationToken token) => Record(ContextlessCall);

            public UniTask HandleAsync(TContext context, CancellationToken token) => Record(context);

            public UniTask HandleInterruptAsync(DialogueInterruptEvent interruptEvent, CancellationToken token) => Record(interruptEvent);

            private UniTask Record(object value)
            {
                Received.Add(value);
                return UniTask.CompletedTask;
            }
        }

        private abstract class ToggleButton
        {
            public bool IsActive { get; private set; }

            public void Activate() => IsActive = true;

            public void Deactivate() => IsActive = false;
        }

        private sealed class TextFeedButtonSpy : ToggleButton, ITextFeedButtonActivatable
        {
        }

        private sealed class FastForwardButtonSpy : ToggleButton, ITextFastForwardButtonActivatable
        {
        }
    }
}
