using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using Presenter.DialogueState.Base;
using Presenter.DialogueState.Handlers;
using Root.Abstract;
using RootContainer;
using UnityEngine.TestTools;
using VContainer;

namespace Messaging.Tests
{
    public sealed class CommandRunningHandlerCancellationTest
    {
        private const int WaitLimitMilliseconds = 5000;

        // このテストのアセンブリは Logic を参照していないので、実物の CommandDispatcher は名前で作る
        private const string CommandDispatcherTypeName = "Logic.CommandDispatcher, amenone.dialogue.logic";

        private IObjectResolver _root;
        private DialogueStateQueue _queue;
        private ICommandStorage _commands;
        private CommandRunningHandler _handler;
        private IDisposable _finishSubscription;
        private int _finishCount;

        [SetUp]
        public void SetUp()
        {
            _finishCount = 0;
            var rootBuilder = new ContainerBuilder();
            rootBuilder.RegisterDialogueMessaging();
            rootBuilder.Register<CancellationTokenHolder>(Lifetime.Singleton).AsImplementedInterfaces();
            _root = rootBuilder.Build();

            _queue = new DialogueStateQueue(_root.Resolve<ITokenGettable>());
            var dispatcher = Activator.CreateInstance(Type.GetType(CommandDispatcherTypeName, throwOnError: true));
            _commands = (ICommandStorage)dispatcher;
            _handler = new CommandRunningHandler(new NoTransitionHolder(), _queue, (ICommandDispatcher)dispatcher,
                _root.Resolve<IAsyncPublisher<DialogueEventMessage>>());
            _finishSubscription = _root.Resolve<IAsyncSubscriber<DialogueEventMessage>>().Subscribe((message, _) =>
            {
                if (message.ev == DialogueEvent.CommandExecuteFinish) _finishCount++;
                return UniTask.CompletedTask;
            });
        }

        [TearDown]
        public void TearDown()
        {
            _finishSubscription.Dispose();
            _queue.Dispose();
            _root.Dispose();
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_CommandFinished_ShouldPublishCommandExecuteFinish() => UniTask.ToCoroutine(async () =>
        {
            _commands.RegisterCommandHandler("noop", () => { });

            await RunCommandAsync("noop");

            Assert.That(_finishCount, Is.EqualTo(1));
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_QueueDisposedWhileTokenAwareCommandRunning_ShouldStopCommandWithoutPublishingFinish() => UniTask.ToCoroutine(async () =>
        {
            var commandToken = default(CancellationToken);
            _commands.RegisterCommandHandler<CancellationToken>("hold", token =>
            {
                commandToken = token;
                return UniTask.Never(token);
            });
            var handling = RunCommandAsync("hold");

            _queue.Dispose();
            var isCanceled = await handling.SuppressCancellationThrow();

            Assert.That(commandToken.IsCancellationRequested, Is.True);
            Assert.That(isCanceled, Is.True);
            Assert.That(_finishCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_QueueDisposedWhileTokenIgnoringCommandRunning_ShouldNotPublishFinishAfterCommand() => UniTask.ToCoroutine(async () =>
        {
            var commandFinish = new UniTaskCompletionSource();
            var isCommandFinished = false;
            async UniTask HoldUntilFinishedAsync()
            {
                await commandFinish.Task;
                isCommandFinished = true;
            }
            _commands.RegisterCommandHandler("hold", HoldUntilFinishedAsync);
            var handling = RunCommandAsync("hold");

            _queue.Dispose();
            commandFinish.TrySetResult();
            var isCanceled = await handling.SuppressCancellationThrow();

            Assert.That(isCommandFinished, Is.True);
            Assert.That(isCanceled, Is.True);
            Assert.That(_finishCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        });

        private UniTask RunCommandAsync(string commandText) => _handler.HandleAsync(new DialogueCommandContext(commandText), CancellationToken.None);

        private sealed class NoTransitionHolder : IDialogueStateHolder
        {
            public Identifier.DialogueState CurrentState => Identifier.DialogueState.CommandRunning;

            public bool CanTransition(DialogueEvent trigger, out Identifier.DialogueState nextState)
            {
                nextState = CurrentState;
                return false;
            }

            public bool CanHandleInterrupt(DialogueInterruptEvent interruptEvent) => false;

            public void Transition(Identifier.DialogueState state)
            {
            }
        }
    }
}
