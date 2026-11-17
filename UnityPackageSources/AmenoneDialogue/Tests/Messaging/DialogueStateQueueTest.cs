using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Presenter.DialogueState.Base;
using UnityEngine.TestTools;

namespace Messaging.Tests
{
    public sealed class DialogueStateQueueTest
    {
        private const int WaitLimitMilliseconds = 5000;

        private DialogueStateQueue _queue;

        [SetUp]
        public void SetUp() => _queue = new DialogueStateQueue();

        [TearDown]
        public void TearDown() => _queue.Dispose();

        [Test]
        public void EnqueueAsync_DisposedWhileActionRunning_ShouldCompleteWithoutException()
        {
            var actionFinish = new UniTaskCompletionSource();
            var running = _queue.EnqueueAsync(() => actionFinish.Task);

            _queue.Dispose();
            actionFinish.TrySetResult();

            Assert.That(running.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void EnqueueAsyncWithResult_DisposedWhileFuncRunning_ShouldReturnResult()
        {
            var funcFinish = new UniTaskCompletionSource<int>();
            var running = _queue.EnqueueAsync(() => funcFinish.Task);

            _queue.Dispose();
            funcFinish.TrySetResult(1);

            Assert.That(running.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(running.GetAwaiter().GetResult(), Is.EqualTo(1));
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_WaitingWhenDisposed_ShouldCompleteWithoutRunningAction() => UniTask.ToCoroutine(async () =>
        {
            var runningFinish = new UniTaskCompletionSource();
            var running = _queue.EnqueueAsync(() => runningFinish.Task);
            var isWaitingActionRun = false;
            var waiting = _queue.EnqueueAsync(() =>
            {
                isWaitingActionRun = true;
                return UniTask.CompletedTask;
            });

            _queue.Dispose();
            runningFinish.TrySetResult();
            await UniTask.WhenAll(running, waiting);

            Assert.That(isWaitingActionRun, Is.False);
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsyncWithResult_WaitingWhenDisposed_ShouldThrowOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var runningFinish = new UniTaskCompletionSource();
            var running = _queue.EnqueueAsync(() => runningFinish.Task);
            var isWaitingFuncRun = false;
            var waiting = _queue.EnqueueAsync(() =>
            {
                isWaitingFuncRun = true;
                return UniTask.FromResult(1);
            });

            _queue.Dispose();
            runningFinish.TrySetResult();
            await running;
            var (isCanceled, _) = await waiting.SuppressCancellationThrow();

            Assert.That(isCanceled, Is.True);
            Assert.That(isWaitingFuncRun, Is.False);
        });

        [Test]
        public void EnqueueAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            _queue.Dispose();

            var enqueued = _queue.EnqueueAsync(() => UniTask.CompletedTask);

            Assert.Throws<ObjectDisposedException>(() => enqueued.GetAwaiter().GetResult());
        }

        [Test]
        public void EnqueueAsyncWithResult_AfterDispose_ShouldThrowObjectDisposedException()
        {
            _queue.Dispose();

            var enqueued = _queue.EnqueueAsync(() => UniTask.FromResult(1));

            Assert.Throws<ObjectDisposedException>(() => enqueued.GetAwaiter().GetResult());
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsync_NotDisposed_ShouldRunActionsOneAtATimeInOrder() => UniTask.ToCoroutine(async () =>
        {
            var executionLog = new List<string>();
            var firstFinish = new UniTaskCompletionSource();
            var first = _queue.EnqueueAsync(async () =>
            {
                executionLog.Add("first started");
                await firstFinish.Task;
                executionLog.Add("first finished");
            });
            var second = _queue.EnqueueAsync(() =>
            {
                executionLog.Add("second started");
                return UniTask.CompletedTask;
            });

            await UniTask.Yield();
            Assert.That(executionLog, Is.EqualTo(new[] { "first started" }));

            firstFinish.TrySetResult();
            await UniTask.WhenAll(first, second);

            Assert.That(executionLog, Is.EqualTo(new[] { "first started", "first finished", "second started" }));
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator EnqueueAsyncWithResult_NotDisposed_ShouldReturnEachFuncResult() => UniTask.ToCoroutine(async () =>
        {
            var firstFinish = new UniTaskCompletionSource<int>();
            var first = _queue.EnqueueAsync(() => firstFinish.Task);
            var second = _queue.EnqueueAsync(() => UniTask.FromResult(2));

            firstFinish.TrySetResult(1);
            var results = await UniTask.WhenAll(first, second);

            Assert.That(results, Is.EqualTo((1, 2)));
        });
    }
}
