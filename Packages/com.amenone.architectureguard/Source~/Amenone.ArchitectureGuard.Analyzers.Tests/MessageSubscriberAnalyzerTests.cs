using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class MessageSubscriberAnalyzerTests
    {
        private const string LogicAssembly = "Game.Logic";

        [Theory]
        [InlineData("ISubscriber<Moved>")]
        [InlineData("IAsyncSubscriber<Moved>")]
        [InlineData("IBufferedSubscriber<Moved>")]
        [InlineData("IBufferedAsyncSubscriber<Moved>")]
        [InlineData("ISubscriber<int, Moved>")]
        [InlineData("IAsyncSubscriber<int, Moved>")]
        [InlineData("IScopedSubscriber<Moved>")]
        public void LogicAssembly_SubscriberConstructorParameter_ShouldReportAG0007(string subscriberType)
        {
            var source = $$"""
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                    public MoveLog({{subscriberType}} subscriber) {}
                }
                """;

            Assert.Equal(new[] { "AG0007" }, AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void LogicAssembly_SubscriberFieldPropertyAndMethodParameter_ShouldReportAG0007Each()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                    private ISubscriber<Moved> _subscriber;

                    public IAsyncSubscriber<Moved> AsyncSubscriber { get; set; }

                    public void Construct(IBufferedSubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Equal(
                new[] { "AG0007", "AG0007", "AG0007" },
                AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void SubscriberOutsidePresenter_ShouldNameAssemblyDeclarationAndType()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                    public MoveLog(ISubscriber<Moved> subscriber) {}
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(
                source,
                assemblyName: LogicAssembly,
                libraries: new[] { LibraryStub.MessagePipe }));

            Assert.StartsWith(
                "アセンブリ Game.Logic の MoveLog のコンストラクタの引数 subscriber が ISubscriber<Moved> を受け取っています。",
                diagnostic.GetMessage());
        }

        [Fact]
        public void LogicAssembly_RecordPrimaryConstructorSubscriber_ShouldReportAG0007Once()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed record MoveLog(ISubscriber<Moved> Subscriber);
                """;

            Assert.Equal(new[] { "AG0007" }, AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void LogicAssembly_InterfaceImplementationAndOverride_ShouldReportAG0007PerDeclaration()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public interface IMoveLog
                {
                    void Bind(ISubscriber<Moved> subscriber);
                }

                public abstract class MoveLogBase : IMoveLog
                {
                    void IMoveLog.Bind(ISubscriber<Moved> subscriber) {}

                    public abstract void Watch(ISubscriber<Moved> subscriber);
                }

                public sealed class MoveLog : MoveLogBase
                {
                    public override void Watch(ISubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Equal(
                new[] { "AG0007", "AG0007", "AG0007", "AG0007" },
                AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void LogicAssembly_PartialMethodSubscriber_ShouldReportAG0007PerPart()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed partial class MoveLog
                {
                    public partial void Bind(ISubscriber<Moved> subscriber);
                }

                public sealed partial class MoveLog
                {
                    public partial void Bind(ISubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Equal(new[] { "AG0007", "AG0007" }, AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void LogicAssembly_NestedTypeSubscriber_ShouldReportAG0007()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public static class MoveLogs
                {
                    public sealed class MoveLog
                    {
                        public MoveLog(ISubscriber<Moved> subscriber) {}
                    }
                }
                """;

            Assert.Equal(new[] { "AG0007" }, AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void GeneratedFile_Subscriber_ShouldNotReport()
        {
            const string source = """
                // <auto-generated/>
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                    private readonly ISubscriber<Moved> _subscriber;

                    public MoveLog(ISubscriber<Moved> subscriber) => _subscriber = subscriber;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void GeneratedType_Subscriber_ShouldNotReport()
        {
            const string source = """
                using System.CodeDom.Compiler;
                using MessagePipe;

                public sealed class Moved {}

                [GeneratedCode("ArchitectureGuardTests", "1.0")]
                public sealed class MoveLog
                {
                    private readonly ISubscriber<Moved> _subscriber;

                    public MoveLog(ISubscriber<Moved> subscriber) => _subscriber = subscriber;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Theory]
        [InlineData("Game.NonPresenter")]
        [InlineData("Game.PresenterKit")]
        [InlineData("Game.Contests")]
        [InlineData("Game.Testing")]
        public void AssemblyWithMarkerInsideSegment_Subscriber_ShouldReportAG0007(string assemblyName)
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                    public MoveLog(ISubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Equal(new[] { "AG0007" }, AnalyzerRunner.AnalyzeIdsIn(assemblyName, source, LibraryStub.MessagePipe));
        }

        [Theory]
        [InlineData("Game.Presenter")]
        [InlineData("game.adv.presenter")]
        [InlineData("Game.PRESENTERS")]
        [InlineData("Game-Presenter_View")]
        public void PresenterAssembly_Subscriber_ShouldNotReport(string assemblyName)
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MovePresenter
                {
                    private readonly ISubscriber<Moved> _subscriber;

                    public MovePresenter(ISubscriber<Moved> subscriber) => _subscriber = subscriber;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(assemblyName, source, LibraryStub.MessagePipe));
        }

        [Theory]
        [InlineData("Game.Logic.Tests")]
        [InlineData("Game.Logic.test")]
        [InlineData("Game_Logic-TESTS")]
        public void TestAssemblyByName_Subscriber_ShouldNotReport(string assemblyName)
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLogTest
                {
                    public void Run(ISubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(assemblyName, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void AssemblyReferencingTestRunner_Subscriber_ShouldNotReport()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLogSpec
                {
                    [UnityEngine.TestTools.UnityTest]
                    public void Run(ISubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn("Game.Logic.Spec", source, LibraryStub.MessagePipe, LibraryStub.TestRunner));
        }

        [Fact]
        public void AssemblyReferencingOnlyNUnit_Subscriber_ShouldReportAG0007()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                    public MoveLog(ISubscriber<Moved> subscriber) {}
                }
                """;

            Assert.Equal(
                new[] { "AG0007" },
                AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe, LibraryStub.NUnit));
        }

        [Fact]
        public void LogicAssembly_PublisherAndRequestHandlerOnly_ShouldNotReport()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class Mover
                {
                    private readonly IPublisher<Moved> _publisher;

                    public Mover(IPublisher<Moved> publisher, IAsyncPublisher<Moved> asyncPublisher, IRequestHandler<int, Moved> handler) =>
                        _publisher = publisher;

                    public IPublisher<Moved> Publisher => _publisher;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void LogicAssembly_OwnSubscriberLookalikeWithMessagePipe_ShouldNotReport()
        {
            const string source = """
                namespace Game
                {
                    public interface ISubscriber<TMessage> {}

                    public sealed class Moved {}

                    public sealed class MoveLog
                    {
                        public MoveLog(ISubscriber<Moved> subscriber) {}
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }

        [Fact]
        public void Compilation_WithoutMessagePipe_OwnSubscriberLookalike_ShouldReportNothing()
        {
            const string source = """
                namespace Game
                {
                    public interface ISubscriber<TMessage> {}

                    public interface IAsyncSubscriber<TMessage> {}

                    public sealed class Moved {}

                    public sealed class MoveLog
                    {
                        private ISubscriber<Moved> _subscriber;

                        public MoveLog(IAsyncSubscriber<Moved> subscriber) {}
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source));
        }

        [Fact]
        public void PragmaDisableAG0007_ShouldReportAG0005()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class MoveLog
                {
                #pragma warning disable AG0007
                    public MoveLog(ISubscriber<Moved> subscriber) {}
                #pragma warning restore AG0007
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIdsIn(LogicAssembly, source, LibraryStub.MessagePipe));
        }
    }
}
