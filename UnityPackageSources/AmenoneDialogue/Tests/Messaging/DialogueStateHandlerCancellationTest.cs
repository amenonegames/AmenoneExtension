using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using Presenter.DialogueState;
using Presenter.DialogueState.Base;
using Root.Abstract;
using RootContainer;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace Messaging.Tests
{
    public sealed class DialogueStateHandlerCancellationTest
    {
        private const int WaitLimitMilliseconds = 5000;
        private const Identifier.DialogueState HandledState = Identifier.DialogueState.VisualizingLine;

        private IObjectResolver _root;
        private IScopedObjectResolver _presenterScope;
        private NextStateFixedHolder _stateHolder;
        private WaitingUntilCanceledHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _stateHolder = new NextStateFixedHolder();
            var rootBuilder = new ContainerBuilder();
            rootBuilder.RegisterDialogueMessaging();
            rootBuilder.Register<CancellationTokenHolder>(Lifetime.Singleton).AsImplementedInterfaces();
            _root = rootBuilder.Build();

            _presenterScope = _root.CreateScope(builder =>
            {
                builder.Register<DialogueStateQueue>(Lifetime.Singleton);
                builder.RegisterInstance(_stateHolder).As<IDialogueStateHolder>();
                builder.Register<WaitingUntilCanceledHandler>(Lifetime.Singleton);
                builder.Register(resolver =>
                {
                    var registry = new DialogueEventHandlerRegistry(resolver.Resolve<IDialogueStateHolder>());
                    registry.Register(resolver.Resolve<WaitingUntilCanceledHandler>());
                    return registry;
                }, Lifetime.Singleton);
                builder.RegisterEntryPoint<DialogueEventPresenter>();
            });
            _handler = _presenterScope.Resolve<WaitingUntilCanceledHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            _presenterScope?.Dispose();
            _root?.Dispose();
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_QueueDisposedWhileEnteringState_ShouldCancelEnterStateTokenWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            var publishing = Publisher().PublishAsync(LineVisualizeRequested);
            AssertEnteredWithLiveToken();

            _presenterScope.Resolve<DialogueStateQueue>().Dispose();
            var isCanceled = await publishing.SuppressCancellationThrow();

            Assert.That(_handler.EnteredToken.IsCancellationRequested, Is.True);
            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_PresenterScopeDisposedAfterPublishNotAwaited_ShouldCancelEnterStateTokenWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            Publisher().Publish(LineVisualizeRequested);
            AssertEnteredWithLiveToken();

            DisposePresenterScope();
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_handler.EnteredToken.IsCancellationRequested, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_RootScopeDisposedWhileEnteringState_ShouldCancelEnterStateTokenWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            Publisher().Publish(LineVisualizeRequested);
            AssertEnteredWithLiveToken();

            DisposeRootScope();
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_handler.EnteredToken.IsCancellationRequested, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsync_WaitingWhenRootTokenCanceled_ShouldNotTransitionOrEnterState() => UniTask.ToCoroutine(async () =>
        {
            var runningFinish = new UniTaskCompletionSource();
            var running = Queue().EnqueueAsync(() => runningFinish.Task);
            var handling = _handler.HandleAsync(CancellationToken.None);

            CancelRootToken();
            runningFinish.TrySetResult();
            await UniTask.WhenAll(running, handling);

            Assert.That(_stateHolder.TransitionCount, Is.Zero);
            Assert.That(_handler.EnteredToken.CanBeCanceled, Is.False);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator HandleAsyncWithContext_QueueDisposedWhileEnteringState_ShouldCancelEnterStateTokenWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            var contextHandler = new WaitingUntilCanceledCommandHandler(_stateHolder, Queue());
            var handling = contextHandler.HandleAsync(new DialogueCommandContext("wait 1"), CancellationToken.None);
            Assert.That(contextHandler.EnteredToken.CanBeCanceled, Is.True);
            Assert.That(contextHandler.EnteredToken.IsCancellationRequested, Is.False);

            Queue().Dispose();
            var isCanceled = await handling.SuppressCancellationThrow();

            Assert.That(contextHandler.EnteredToken.IsCancellationRequested, Is.True);
            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        private static DialogueEventMessage LineVisualizeRequested => new(DialogueEvent.LineVisualizeRequested);

        private IAsyncPublisher<DialogueEventMessage> Publisher() => _root.Resolve<IAsyncPublisher<DialogueEventMessage>>();

        private DialogueStateQueue Queue() => _presenterScope.Resolve<DialogueStateQueue>();

        // スコープを破棄せずに Root の token だけをキャンセルするため、登録された CancellationTokenHolder を直接破棄する
        private void CancelRootToken() => ((IDisposable)_root.Resolve<ITokenGettable>()).Dispose();

        private void AssertEnteredWithLiveToken()
        {
            Assert.That(_handler.EnteredToken.CanBeCanceled, Is.True);
            Assert.That(_handler.EnteredToken.IsCancellationRequested, Is.False);
        }

        private void DisposePresenterScope()
        {
            _presenterScope.Dispose();
            _presenterScope = null;
        }

        private void DisposeRootScope()
        {
            _root.Dispose();
            _root = null;
        }

        private sealed class WaitingUntilCanceledHandler : DialogueStateHandlerBase
        {
            public WaitingUntilCanceledHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue) : base(stateHolder, queue)
            {
            }

            public override Identifier.DialogueState State => HandledState;

            public CancellationToken EnteredToken { get; private set; }

            protected override UniTask OnEnterState(CancellationToken token)
            {
                EnteredToken = token;
                return UniTask.Never(token);
            }
        }

        private sealed class WaitingUntilCanceledCommandHandler : DialogueStateHandlerBase<DialogueCommandContext>
        {
            public WaitingUntilCanceledCommandHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue) : base(stateHolder, queue)
            {
            }

            public override Identifier.DialogueState State => Identifier.DialogueState.CommandRunning;

            public CancellationToken EnteredToken { get; private set; }

            protected override UniTask OnEnterState(DialogueCommandContext context, CancellationToken token)
            {
                EnteredToken = token;
                return UniTask.Never(token);
            }
        }

        private sealed class NextStateFixedHolder : IDialogueStateHolder
        {
            public int TransitionCount { get; private set; }

            public Identifier.DialogueState CurrentState => HandledState;

            public bool CanTransition(DialogueEvent trigger, out Identifier.DialogueState nextState)
            {
                nextState = HandledState;
                return true;
            }

            public bool CanHandleInterrupt(DialogueInterruptEvent interruptEvent) => false;

            public void Transition(Identifier.DialogueState state) => TransitionCount++;
        }
    }
}
