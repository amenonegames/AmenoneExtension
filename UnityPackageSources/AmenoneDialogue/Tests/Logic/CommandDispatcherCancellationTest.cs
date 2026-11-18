using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Logic.Tests
{
    public sealed class CommandDispatcherCancellationTest
    {
        private const int WaitLimitMilliseconds = 5000;
        private const float OmittedSeconds = -1f;

        private CommandDispatcher _dispatcher;
        private TokenReceivingCommands _commands;
        private CancellationTokenSource _tokenSource;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = new CommandDispatcher();
            _commands = new TokenReceivingCommands();
            _dispatcher.RegisterCommandsFromInstance(_commands);
            _tokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown() => _tokenSource.Dispose();

        [Test]
        public void Dispatch_YarnCommandWithOnlyToken_ShouldPassDispatchToken()
        {
            Dispatch("token_only");

            Assert.That(_commands.Calls, Is.EqualTo(1));
            Assert.That(_commands.ReceivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_YarnCommandWithArgumentThenToken_ShouldPassArgumentAndToken()
        {
            Dispatch("name_then_token door");

            Assert.That(_commands.ReceivedName, Is.EqualTo("door"));
            Assert.That(_commands.ReceivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_YarnCommandWithTokenFirst_ShouldPassArgumentAndToken()
        {
            Dispatch("token_then_name door");

            Assert.That(_commands.ReceivedName, Is.EqualTo("door"));
            Assert.That(_commands.ReceivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_YarnCommandOptionalArgumentOmittedBeforeToken_ShouldFillDefaultAndPassToken()
        {
            Dispatch("optional_seconds_then_token");

            Assert.That(_commands.ReceivedSeconds, Is.EqualTo(OmittedSeconds));
            Assert.That(_commands.ReceivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_YarnCommandOptionalArgumentWrittenBeforeToken_ShouldUseWrittenValueAndPassToken()
        {
            Dispatch("optional_seconds_then_token 2");

            Assert.That(_commands.ReceivedSeconds, Is.EqualTo(2f));
            Assert.That(_commands.ReceivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_LambdaWithOnlyToken_ShouldPassDispatchToken()
        {
            var receivedToken = default(CancellationToken);
            _dispatcher.RegisterCommandHandler<CancellationToken>("lambda_token", token =>
            {
                receivedToken = token;
                return UniTask.CompletedTask;
            });

            Dispatch("lambda_token");

            Assert.That(receivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_LambdaWithArgumentAndToken_ShouldPassArgumentAndToken()
        {
            var receivedSeconds = 0f;
            var receivedToken = default(CancellationToken);
            _dispatcher.RegisterCommandHandler<float, CancellationToken>("lambda_wait", (seconds, token) =>
            {
                receivedSeconds = seconds;
                receivedToken = token;
                return UniTask.CompletedTask;
            });

            Dispatch("lambda_wait 3");

            Assert.That(receivedSeconds, Is.EqualTo(3f));
            Assert.That(receivedToken, Is.EqualTo(_tokenSource.Token));
        }

        [Test]
        public void Dispatch_CommandWithoutToken_ShouldRunAsBefore()
        {
            var receivedName = string.Empty;
            _dispatcher.RegisterCommandHandler<string>("say", name => receivedName = name);

            Dispatch("say hello");

            Assert.That(receivedName, Is.EqualTo("hello"));
        }

        [Test]
        public void Dispatch_MoreArgumentsThanScriptParameters_ShouldReportCountsWithoutToken()
        {
            LogAssert.Expect(LogType.Error, new Regex("expects 1 to 1 arguments, but 2 were provided"));

            Dispatch("name_then_token door window");

            Assert.That(_commands.Calls, Is.Zero);
        }

        [Test]
        public void Dispatch_RequiredArgumentMissingBesideToken_ShouldReportCountsWithoutToken()
        {
            LogAssert.Expect(LogType.Error, new Regex("expects 1 to 1 arguments, but 0 were provided"));

            Dispatch("token_then_name");

            Assert.That(_commands.Calls, Is.Zero);
        }

        [Test]
        public void Dispatch_TooManyArgumentsForOptionalArgumentAndToken_ShouldReportCountsWithoutToken()
        {
            LogAssert.Expect(LogType.Error, new Regex("expects 0 to 1 arguments, but 2 were provided"));

            Dispatch("optional_seconds_then_token 1 2");

            Assert.That(_commands.Calls, Is.Zero);
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator Dispatch_TokenCanceledWhileTokenAwareCommandRunning_ShouldStopCommandAndCancelWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            var dispatching = _dispatcher.DispatchCommandAsync("hold", _tokenSource.Token);

            _tokenSource.Cancel();
            var isCanceled = await dispatching.SuppressCancellationThrow();

            Assert.That(_commands.ReceivedToken.IsCancellationRequested, Is.True);
            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator Dispatch_TokenCanceledWhileTokenIgnoringCommandRunning_ShouldFinishCommandThenCancel() => UniTask.ToCoroutine(async () =>
        {
            var commandFinish = new UniTaskCompletionSource();
            var isCommandFinished = false;
            async UniTask HoldUntilFinishedAsync()
            {
                await commandFinish.Task;
                isCommandFinished = true;
            }
            _dispatcher.RegisterCommandHandler("ignore_token", HoldUntilFinishedAsync);
            var dispatching = _dispatcher.DispatchCommandAsync("ignore_token", _tokenSource.Token);

            _tokenSource.Cancel();
            await UniTask.Yield();
            var statusAfterCancel = dispatching.Status;
            commandFinish.TrySetResult();
            var isCanceled = await dispatching.SuppressCancellationThrow();

            Assert.That(statusAfterCancel, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(isCommandFinished, Is.True);
            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator Dispatch_SyncCommandThrowsCanceledExceptionAfterTokenCanceled_ShouldCancelWithoutErrorLog() => UniTask.ToCoroutine(async () =>
        {
            _dispatcher.RegisterCommandHandler<CancellationToken>("check_token", token => token.ThrowIfCancellationRequested());
            _tokenSource.Cancel();

            var isCanceled = await _dispatcher.DispatchCommandAsync("check_token", _tokenSource.Token).SuppressCancellationThrow();

            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [Test]
        public void Dispatch_CommandThrowsWhileTokenNotCanceled_ShouldLogErrorAndComplete()
        {
            Action failing = () => throw new InvalidOperationException("command failed");
            _dispatcher.RegisterCommandHandler("fail", failing);
            LogAssert.Expect(LogType.Error, new Regex("Error executing command 'fail'"));

            Assert.DoesNotThrow(() => Dispatch("fail"));
        }

        [Test]
        public void Dispatch_CommandThrowsCanceledExceptionWhileTokenNotCanceled_ShouldLogErrorAndComplete()
        {
            Func<UniTask> cancelingItself = () => UniTask.FromCanceled();
            _dispatcher.RegisterCommandHandler("cancel_itself", cancelingItself);
            LogAssert.Expect(LogType.Error, new Regex("Error executing command 'cancel_itself'"));

            Assert.DoesNotThrow(() => Dispatch("cancel_itself"));
        }

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator Dispatch_WaitCommandTokenCanceled_ShouldStopWaiting() => UniTask.ToCoroutine(async () =>
        {
            new DefaultYarnCommands().RegisterTo(_dispatcher);
            var dispatching = _dispatcher.DispatchCommandAsync("wait 60", _tokenSource.Token);

            _tokenSource.Cancel();
            var isCanceled = await dispatching.SuppressCancellationThrow();

            Assert.That(isCanceled, Is.True);
            LogAssert.NoUnexpectedReceived();
        });

        [UnityTest, Timeout(WaitLimitMilliseconds)]
        public IEnumerator Wait_CalledWithSecondsOnly_ShouldComplete() => UniTask.ToCoroutine(async () =>
        {
            await new DefaultYarnCommands().Wait(0f);
        });

        private void Dispatch(string command) => _dispatcher.DispatchCommandAsync(command, _tokenSource.Token).GetAwaiter().GetResult();

        // Yarn Spinner の解析器は、[YarnCommand] の付いたメソッドを public の型に置く事を求める
        public sealed class TokenReceivingCommands
        {
            public int Calls { get; private set; }
            public CancellationToken ReceivedToken { get; private set; }
            public string ReceivedName { get; private set; }
            public float ReceivedSeconds { get; private set; }

            [YarnCommand("token_only")]
            public void TokenOnly(CancellationToken token) => Receive(token);

            [YarnCommand("name_then_token")]
            public UniTask NameThenTokenAsync(string name, CancellationToken token)
            {
                ReceivedName = name;
                Receive(token);
                return UniTask.CompletedTask;
            }

            [YarnCommand("token_then_name")]
            public void TokenThenName(CancellationToken token, string name)
            {
                ReceivedName = name;
                Receive(token);
            }

            [YarnCommand("optional_seconds_then_token")]
            public void OptionalSecondsThenToken(float seconds = OmittedSeconds, CancellationToken token = default)
            {
                ReceivedSeconds = seconds;
                Receive(token);
            }

            [YarnCommand("hold")]
            public UniTask HoldAsync(CancellationToken token)
            {
                Receive(token);
                return UniTask.Never(token);
            }

            private void Receive(CancellationToken token)
            {
                Calls++;
                ReceivedToken = token;
            }
        }
    }
}
