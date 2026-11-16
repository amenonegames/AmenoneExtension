using LogicAbstract;
using NUnit.Framework;
using VContainer;
using Yarn;

namespace Logic.Tests
{
    public sealed class DialogueVariableWriterTest
    {
        private IObjectResolver _root;
        private IScopedObjectResolver _logicScope;
        private IScopedObjectResolver _descendantScope;

        [SetUp]
        public void SetUp()
        {
            _root = new ContainerBuilder().Build();
            _logicScope = _root.CreateScope(RegisterAsLogicContainerBuilderDoes);
            _descendantScope = _logicScope.CreateScope().CreateScope();
        }

        [TearDown]
        public void TearDown()
        {
            _descendantScope.Dispose();
            _logicScope.Dispose();
            _root.Dispose();
        }

        [Test]
        public void Write_NumberFromDescendantScope_ShouldBeReadFromDialogueStorage()
        {
            WriterFromDescendantScope().Write("$count", 3.5f);

            Assert.That(DialogueStorage().TryGetValue("$count", out float value), Is.True);
            Assert.That(value, Is.EqualTo(3.5f));
        }

        [Test]
        public void Write_FlagFromDescendantScope_ShouldBeReadFromDialogueStorage()
        {
            WriterFromDescendantScope().Write("$opened", true);

            Assert.That(DialogueStorage().TryGetValue("$opened", out bool value), Is.True);
            Assert.That(value, Is.True);
        }

        [Test]
        public void Write_WordFromDescendantScope_ShouldBeReadFromDialogueStorage()
        {
            WriterFromDescendantScope().Write("$name", "あめ");

            Assert.That(DialogueStorage().TryGetValue("$name", out string value), Is.True);
            Assert.That(value, Is.EqualTo("あめ"));
        }

        private static void RegisterAsLogicContainerBuilderDoes(IContainerBuilder builder)
        {
            builder.Register<VariableStorage>(Lifetime.Scoped).As<IVariableStorage>();
            builder.Register<DialogueVariableWriter>(Lifetime.Singleton).As<IDialogueVariableWriter>();
        }

        private IDialogueVariableWriter WriterFromDescendantScope() => _descendantScope.Resolve<IDialogueVariableWriter>();

        private IVariableStorage DialogueStorage() => _logicScope.Resolve<IVariableStorage>();
    }
}
