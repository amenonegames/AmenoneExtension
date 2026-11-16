using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using NUnit.Framework;
using RootContainer;
using VContainer;
using Yarn;
using Yarn.Unity;
using Object = UnityEngine.Object;

namespace Logic.Tests
{
    public sealed class DialogueFactoryTest
    {
        private const string StartNode = "Start";
        private const string Script = "title: Start\n---\nHello\n<<wait 1>>\n-> First\n-> Second\n===\n";
        private const int StepLimit = 10;

        private readonly List<object> _received = new();
        private YarnProject _yarnProject;
        private IObjectResolver _container;

        [SetUp]
        public void SetUp()
        {
            _received.Clear();
            _yarnProject = CompiledYarnProject.Create(Script);
            var builder = new ContainerBuilder();
            builder.RegisterDialogueMessaging();
            builder.RegisterInstance(_yarnProject);
            builder.RegisterInstance(new MemoryVariableStore()).As<IVariableStorage>();
            builder.Register<DialogueFactory>(Lifetime.Singleton).As<IDialogueFactory>();
            _container = builder.Build();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
            Object.DestroyImmediate(_yarnProject);
        }

        [Test]
        public void Create_ScriptRunToEnd_ShouldPublishNodeStartLineCommandOptionsAndNodeEndInOrder()
        {
            using var subscriptions = DisposableBag.Create(
                Record<NodeStart>(),
                Record<DialogueLineEventMessage>(),
                Record<DialogueCommandEventMessage>(),
                Record<DialogueOptionEventMessage>(),
                Record<NodeEnd>());
            var dialogue = _container.Resolve<IDialogueFactory>().Create();

            RunToEnd(dialogue);

            Assert.That(_received.Select(message => message.GetType()), Is.EqualTo(new[]
            {
                typeof(NodeStart),
                typeof(DialogueLineEventMessage),
                typeof(DialogueCommandEventMessage),
                typeof(DialogueOptionEventMessage),
                typeof(NodeEnd),
            }));
            Assert.That(Single<NodeStart>().Context.NodeName, Is.EqualTo(StartNode));
            Assert.That(Single<DialogueLineEventMessage>().ev, Is.EqualTo(DialogueEvent.LineVisualizeRequested));
            Assert.That(Single<DialogueCommandEventMessage>().ev, Is.EqualTo(DialogueEvent.CommandRunRequested));
            Assert.That(Single<DialogueCommandEventMessage>().Context.CommandText, Is.EqualTo("wait 1"));
            Assert.That(Single<DialogueOptionEventMessage>().ev, Is.EqualTo(DialogueEvent.OptionsVisualizeRequested));
            Assert.That(Single<DialogueOptionEventMessage>().Context.OptionData.Length, Is.EqualTo(2));
        }

        private void RunToEnd(Dialogue dialogue)
        {
            dialogue.SetNode(StartNode);
            var step = 0;
            do
            {
                if (_received.LastOrDefault() is DialogueOptionEventMessage) dialogue.SetSelectedOption(0);
                dialogue.Continue();
            } while (dialogue.IsActive && ++step < StepLimit);
        }

        private IDisposable Record<T>() =>
            _container.Resolve<IAsyncSubscriber<T>>().Subscribe((message, _) =>
            {
                _received.Add(message);
                return UniTask.CompletedTask;
            });

        private T Single<T>() => _received.OfType<T>().Single();
    }
}
