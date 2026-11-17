using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using Root.Abstract;
using RootContainer;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Yarn;
using Yarn.Unity;
using Object = UnityEngine.Object;

namespace Logic.Tests
{
    public sealed class DialogueHolderTest
    {
        private const string Script =
            "title: START\n---\nDefault first\nDefault second\n===\n" +
            "title: Start\n---\nHello\nWorld\n===\n" +
            "title: Broken\n---\n<<jump {\"Missing\"}>>\n===\n";

        private YarnProject _yarnProject;
        private CancellationTokenSource _dialogueLifetime;
        private IObjectResolver _container;

        [SetUp]
        public void SetUp()
        {
            _yarnProject = CompiledYarnProject.Create(Script);
            _dialogueLifetime = new CancellationTokenSource();
            var builder = new ContainerBuilder();
            builder.RegisterDialogueMessaging();
            builder.RegisterInstance(_yarnProject);
            builder.RegisterInstance(new MemoryVariableStore()).As<IVariableStorage>();
            builder.RegisterInstance(new FixedTokenGettable(_dialogueLifetime.Token)).As<ITokenGettable>();
            builder.Register<DialogueFactory>(Lifetime.Singleton).As<IDialogueFactory>();
            builder.Register<DialogueHolder>(Lifetime.Singleton).AsSelf();
            _container = builder.Build();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
            _dialogueLifetime.Dispose();
            Object.DestroyImmediate(_yarnProject);
        }

        [Test]
        public void Start_TokenGettableHasToken_ShouldPublishStartDialogueWithThatToken()
        {
            var received = new List<(DialogueEventMessage message, CancellationToken token)>();
            using var subscription = _container.Resolve<IAsyncSubscriber<DialogueEventMessage>>().Subscribe((message, token) =>
            {
                received.Add((message, token));
                return UniTask.CompletedTask;
            });

            _container.Resolve<DialogueHolder>().Start("Start");

            Assert.That(received, Is.EqualTo(new[] { (new DialogueEventMessage(DialogueEvent.StartDialogue), _dialogueLifetime.Token) }));
        }

        [Test]
        public void Start_SubscriberContinuesOnStartDialogue_ShouldDeliverOnlyFirstLineWithoutException()
        {
            var holder = _container.Resolve<DialogueHolder>();
            var thrown = new List<Exception>();
            var deliveredLines = new List<string>();
            using var continuation = ContinueOnStartDialogue(holder, thrown);
            using var lines = CollectLineTexts(deliveredLines);

            holder.Start("Start");

            Assert.That(thrown, Is.Empty);
            Assert.That(deliveredLines, Is.EqualTo(new[] { "Hello" }));
        }

        [Test]
        public void StartWithoutNodeName_SubscriberContinuesOnStartDialogue_ShouldDeliverOnlyFirstLineOfStartNode()
        {
            var holder = _container.Resolve<DialogueHolder>();
            var thrown = new List<Exception>();
            var deliveredLines = new List<string>();
            using var continuation = ContinueOnStartDialogue(holder, thrown);
            using var lines = CollectLineTexts(deliveredLines);

            holder.Start();

            Assert.That(thrown, Is.Empty);
            Assert.That(deliveredLines, Is.EqualTo(new[] { "Default first" }));
        }

        [Test]
        public void Continue_VMThrows_ShouldLogTheExceptionAndPublishDialogueFailedOnce()
        {
            var holder = _container.Resolve<DialogueHolder>();
            var failures = new List<DialogueFailed>();
            using var failure = CollectFailures(failures);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("."));

            holder.Continue();

            Assert.That(failures.Count, Is.EqualTo(1), "ノードを選ばずに Continue すると VM が投げる");
            Assert.That(failures[0].Exception, Is.Not.Null);
        }

        [Test]
        public void Continue_VMRunsNormally_ShouldNotPublishDialogueFailed()
        {
            var holder = _container.Resolve<DialogueHolder>();
            var failures = new List<DialogueFailed>();
            using var failure = CollectFailures(failures);
            using var continuation = ContinueOnStartDialogue(holder, new List<Exception>());

            holder.Start("Start");
            holder.Continue();

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void Start_AfterTheVMThrewAndStop_ShouldDeliverTheFirstLineOfTheOtherNode()
        {
            var holder = _container.Resolve<DialogueHolder>();
            var thrown = new List<Exception>();
            var deliveredLines = new List<string>();
            var failures = new List<DialogueFailed>();
            using var continuation = ContinueOnStartDialogue(holder, thrown);
            using var lines = CollectLineTexts(deliveredLines);
            using var failure = CollectFailures(failures);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("Missing"));
            holder.Start("Broken");
            Assert.That(failures.Count, Is.EqualTo(1), "無いノードへの jump で VM が投げる");

            holder.Stop();
            holder.Start("Start");

            Assert.That(thrown, Is.Empty);
            Assert.That(failures.Count, Is.EqualTo(1));
            Assert.That(deliveredLines, Is.EqualTo(new[] { "Hello" }));
        }

        private IDisposable CollectFailures(List<DialogueFailed> failures) =>
            _container.Resolve<IAsyncSubscriber<DialogueFailed>>().Subscribe((message, _) =>
            {
                failures.Add(message);
                return UniTask.CompletedTask;
            });

        private IDisposable ContinueOnStartDialogue(IDialogueContinuable dialogue, List<Exception> thrown) =>
            _container.Resolve<IAsyncSubscriber<DialogueEventMessage>>().Subscribe((message, _) =>
            {
                if (message.ev == DialogueEvent.StartDialogue) ContinueRecordingException(dialogue, thrown);
                return UniTask.CompletedTask;
            });

        private static void ContinueRecordingException(IDialogueContinuable dialogue, List<Exception> thrown)
        {
            try
            {
                dialogue.Continue();
            }
            catch (Exception exception)
            {
                thrown.Add(exception);
            }
        }

        private IDisposable CollectLineTexts(List<string> deliveredLines)
        {
            var lineTexts = CompiledYarnProject.LineTexts(Script);
            return _container.Resolve<IAsyncSubscriber<DialogueLineEventMessage>>().Subscribe((message, _) =>
            {
                deliveredLines.Add(lineTexts[message.Context.LineData.LineId]);
                return UniTask.CompletedTask;
            });
        }

        private sealed class FixedTokenGettable : ITokenGettable
        {
            private readonly CancellationToken _token;

            public FixedTokenGettable(CancellationToken token) => _token = token;

            public CancellationToken Get() => _token;
        }
    }
}
