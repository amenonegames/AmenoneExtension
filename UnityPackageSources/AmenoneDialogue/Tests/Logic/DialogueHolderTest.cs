using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using Root.Abstract;
using RootContainer;
using VContainer;
using Yarn;
using Yarn.Unity;
using Object = UnityEngine.Object;

namespace Logic.Tests
{
    public sealed class DialogueHolderTest
    {
        private const string Script = "title: Start\n---\nHello\n===\n";

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

        private sealed class FixedTokenGettable : ITokenGettable
        {
            private readonly CancellationToken _token;

            public FixedTokenGettable(CancellationToken token) => _token = token;

            public CancellationToken Get() => _token;
        }
    }
}
