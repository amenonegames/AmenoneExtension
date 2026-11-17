using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Presenter.DialogueState.Base;
using RootContainer;
using UnityEngine.TestTools;

namespace Messaging.Tests
{
    public sealed class DialogueStateQueueCancellationTest
    {
        private const int WaitLimitMilliseconds = 5000;

        private CancellationTokenHolder _rootTokenHolder;
        private DialogueStateQueue _queue;
        private int _runningActionCount;
        private int _maxRunningActionCount;
        private int _finishedActionCount;

        [SetUp]
        public void SetUp()
        {
            _runningActionCount = 0;
            _maxRunningActionCount = 0;
            _finishedActionCount = 0;
            _rootTokenHolder = new CancellationTokenHolder();
            _queue = new DialogueStateQueue(_rootTokenHolder);
        }

        [TearDown]
        public void TearDown()
        {
            _queue.Dispose();
            _rootTokenHolder.Dispose();
        }

        [Test]
        public void EnqueueAsync_ActionRunning_ShouldPassCancelableTokenNotYetCanceled()
        {
            var actionToken = EnqueueTokenCapturingAction(_queue);

            Assert.That(actionToken.CanBeCanceled, Is.True);
            Assert.That(actionToken.IsCancellationRequested, Is.False);
        }

        [Test]
        public void EnqueueAsync_QueueDisposedWhileActionRunning_ShouldCancelActionToken()
        {
            var actionToken = EnqueueTokenCapturingAction(_queue);

            _queue.Dispose();

            Assert.That(actionToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void EnqueueAsync_RootTokenCanceledWhileActionRunning_ShouldCancelActionToken()
        {
            var actionToken = EnqueueTokenCapturingAction(_queue);

            _rootTokenHolder.Dispose();

            Assert.That(actionToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void EnqueueAsync_CallerTokenCanceledWhileActionRunning_ShouldCancelActionToken()
        {
            using var callerTokenSource = new CancellationTokenSource();
            var actionToken = EnqueueTokenCapturingAction(_queue, callerTokenSource.Token);

            callerTokenSource.Cancel();

            Assert.That(actionToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void EnqueueAsync_QueueWithoutRootTokenDisposedWhileActionRunning_ShouldCancelActionToken()
        {
            using var queueWithoutRootToken = new DialogueStateQueue();
            var actionToken = EnqueueTokenCapturingAction(queueWithoutRootToken);

            queueWithoutRootToken.Dispose();

            Assert.That(actionToken.IsCancellationRequested, Is.True);
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_ActionStoppedByToken_ShouldBeCanceledWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            var running = _queue.EnqueueAsync(token => UniTask.Never(token));

            _queue.Dispose();
            var isCanceled = await running.SuppressCancellationThrow();

            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsyncWithResult_FuncStoppedByToken_ShouldBeCanceledWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            var running = _queue.EnqueueAsync(token => UniTask.Never<int>(token));

            _queue.Dispose();
            var (isCanceled, _) = await running.SuppressCancellationThrow();

            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_ActionStoppedByTokenNotAwaited_ShouldNotLogError() => UniTask.ToCoroutine(async () =>
        {
            _queue.EnqueueAsync(token => UniTask.Never(token)).Forget();

            _queue.Dispose();
            await UniTask.Yield();
            await UniTask.Yield();

            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_CallerTokenOfFinishedAction_ShouldRunNextAction() => UniTask.ToCoroutine(async () =>
        {
            var next = UniTask.CompletedTask;
            var isNextActionRun = false;
            await _queue.EnqueueAsync(token =>
            {
                next = _queue.EnqueueAsync(_ =>
                {
                    isNextActionRun = true;
                    return UniTask.CompletedTask;
                }, token);
                return UniTask.CompletedTask;
            });

            await next;

            Assert.That(isNextActionRun, Is.True);
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_WaitingWhenRootTokenCanceled_ShouldCompleteWithoutRunningAction() => UniTask.ToCoroutine(async () =>
        {
            var runningFinish = new UniTaskCompletionSource();
            var running = _queue.EnqueueAsync(() => runningFinish.Task);
            var isWaitingActionRun = false;
            var waiting = _queue.EnqueueAsync(() =>
            {
                isWaitingActionRun = true;
                return UniTask.CompletedTask;
            });

            _rootTokenHolder.Dispose();
            runningFinish.TrySetResult();
            await UniTask.WhenAll(running, waiting);

            Assert.That(isWaitingActionRun, Is.False);
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsyncWithResult_WaitingWhenRootTokenCanceled_ShouldThrowOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var runningFinish = new UniTaskCompletionSource();
            var running = _queue.EnqueueAsync(() => runningFinish.Task);
            var isWaitingFuncRun = false;
            var waiting = _queue.EnqueueAsync(() =>
            {
                isWaitingFuncRun = true;
                return UniTask.FromResult(1);
            });

            _rootTokenHolder.Dispose();
            runningFinish.TrySetResult();
            await running;
            var (isCanceled, _) = await waiting.SuppressCancellationThrow();

            Assert.That(isCanceled, Is.True);
            Assert.That(isWaitingFuncRun, Is.False);
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_CallerTokenCanceledWhileWaiting_ShouldRunFollowingActionsOneAtATime() => UniTask.ToCoroutine(async () =>
        {
            var runningFinish = new UniTaskCompletionSource();
            var running = _queue.EnqueueAsync(() => runningFinish.Task);
            using var callerTokenSource = new CancellationTokenSource();
            var canceledWaiting = _queue.EnqueueAsync(CountRunningActionAsync, callerTokenSource.Token);
            var firstFollowing = _queue.EnqueueAsync(CountRunningActionAsync);
            var secondFollowing = _queue.EnqueueAsync(CountRunningActionAsync);

            callerTokenSource.Cancel();
            runningFinish.TrySetResult();
            await running;
            // SemaphoreSlim の待ちのキャンセルは後から非同期に届くので、直後に Release されると待ちは席を得て正常に終わる。どちらでも action は走らない
            await canceledWaiting.SuppressCancellationThrow();
            await UniTask.WhenAll(firstFollowing, secondFollowing);

            Assert.That(_finishedActionCount, Is.EqualTo(2));
            Assert.That(_maxRunningActionCount, Is.EqualTo(1));
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_ActionThrew_ShouldRunNextAction() => UniTask.ToCoroutine(async () =>
        {
            var failing = _queue.EnqueueAsync(() => UniTask.FromException(new InvalidOperationException()));
            var isNextActionRun = false;
            var next = _queue.EnqueueAsync(() =>
            {
                isNextActionRun = true;
                return UniTask.CompletedTask;
            });

            var isFailed = false;
            try
            {
                await failing;
            }
            catch (InvalidOperationException)
            {
                isFailed = true;
            }
            await next;

            Assert.That(isFailed, Is.True);
            Assert.That(isNextActionRun, Is.True);
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_DisposedInsideAction_ShouldCancelActionTokenAndCompleteWithoutException() => UniTask.ToCoroutine(async () =>
        {
            var isActionTokenCanceled = false;
            var running = _queue.EnqueueAsync(token =>
            {
                _queue.Dispose();
                isActionTokenCanceled = token.IsCancellationRequested;
                return UniTask.CompletedTask;
            });

            await running;

            Assert.That(isActionTokenCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_EnqueuedInsideActionWithoutAwaiting_ShouldRunAfterAction() => UniTask.ToCoroutine(async () =>
        {
            var runOrder = new List<string>();
            var inner = UniTask.CompletedTask;
            await _queue.EnqueueAsync(() =>
            {
                inner = _queue.EnqueueAsync(() =>
                {
                    runOrder.Add("inner");
                    return UniTask.CompletedTask;
                });
                runOrder.Add("outer");
                return UniTask.CompletedTask;
            });

            await inner;

            Assert.That(runOrder, Is.EqualTo(new[] { "outer", "inner" }));
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_CallerTokenTakenFromFinishedAction_ShouldRunAction() => UniTask.ToCoroutine(async () =>
        {
            var finishedActionToken = default(CancellationToken);
            await _queue.EnqueueAsync(token =>
            {
                finishedActionToken = token;
                return UniTask.CompletedTask;
            });
            var isActionRun = false;

            await _queue.EnqueueAsync(_ =>
            {
                isActionRun = true;
                return UniTask.CompletedTask;
            }, finishedActionToken);

            Assert.That(finishedActionToken.CanBeCanceled, Is.True);
            Assert.That(isActionRun, Is.True);
        });

        private async UniTask CountRunningActionAsync()
        {
            _runningActionCount++;
            _maxRunningActionCount = Math.Max(_maxRunningActionCount, _runningActionCount);
            await UniTask.Yield();
            _runningActionCount--;
            _finishedActionCount++;
        }

        private static CancellationToken EnqueueTokenCapturingAction(DialogueStateQueue queue, CancellationToken callerToken = default)
        {
            var actionToken = default(CancellationToken);
            queue.EnqueueAsync(token =>
            {
                actionToken = token;
                return UniTask.Never(token);
            }, callerToken).Forget();
            return actionToken;
        }
    }
}
