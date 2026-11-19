using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class PublishingSystemAnalyzerTests
    {
        private static readonly LibraryStub[] AllLibraries =
        {
            LibraryStub.Entities,
            LibraryStub.MessagePipe,
            LibraryStub.VContainer,
            LibraryStub.EcsBridge,
        };

        [Theory]
        [InlineData("IPublisher<Moved>")]
        [InlineData("IAsyncPublisher<Moved>")]
        [InlineData("IBufferedPublisher<Moved>")]
        [InlineData("IBufferedAsyncPublisher<Moved>")]
        [InlineData("IPublisher<int, Moved>")]
        [InlineData("IAsyncPublisher<int, Moved>")]
        [InlineData("IDisposablePublisher<Moved>")]
        public void SystemBase_PublisherField_ShouldReportAG0008(string publisherType)
        {
            var source = $$"""
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private {{publisherType}} _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SystemBase_PublisherInjectMethodParameter_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;
                using VContainer;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    [Inject]
                    public void Construct(IPublisher<Moved> publisher) {}

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SystemBase_PublisherProperty_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    public IPublisher<Moved> Publisher { get; set; }

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SystemBase_PublisherConstructorParameter_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    public MoveSystem(IAsyncPublisher<Moved> publisher) {}

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SystemBase_SeveralPublishers_ShouldReportAG0008OnceAtTypeName()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;
                using VContainer;

                public sealed class Moved {}

                public sealed class Stopped {}

                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _moved;

                    public IPublisher<Stopped> Stopped { get; set; }

                    [Inject]
                    public void Construct(IPublisher<Moved> moved, IPublisher<Stopped> stopped) {}

                    protected override void OnUpdate() {}
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source, libraries: AllLibraries));

            Assert.Equal("AG0008", diagnostic.Id);
            Assert.Equal("MoveSystem", SourceTextAt(diagnostic));
            Assert.StartsWith(
                "System の MoveSystem が IPublisher<Moved>、IPublisher<Stopped> を受け取っていますが、Amenone.EcsBridge.PublishSystem<TPublisher> を継いでいません。",
                diagnostic.GetMessage());
            Assert.Contains("PublishSystem<TPublisher> を継いでください。", diagnostic.GetMessage());
        }

        [Fact]
        public void ISystemStruct_PublisherField_ShouldReportAG0008WithClassSystemGuidance()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial struct MoveSystem : ISystem
                {
                    private IPublisher<Moved> _publisher;
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source, libraries: AllLibraries));

            Assert.Equal("AG0008", diagnostic.Id);
            Assert.Contains(
                "ISystem の struct は基底クラスを継げないので、発行は PublishSystem<TPublisher> を継ぐクラスの System で行ってください。",
                diagnostic.GetMessage());
        }

        [Fact]
        public void NestedSystem_Publisher_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public static class MoveSystems
                {
                    public partial class MoveSystem : SystemBase
                    {
                        private IPublisher<Moved> _publisher;

                        protected override void OnUpdate() {}
                    }
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void PartialSystemWithPublishersInEachPart_ShouldReportAG0008Once()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _moved;

                    protected override void OnUpdate() {}
                }

                public partial class MoveSystem
                {
                    private IAsyncPublisher<Moved> _asyncMoved;
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void AbstractSystemBaseDerived_Publisher_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public abstract partial class MoveSystemBase : SystemBase
                {
                    protected IPublisher<Moved> Publisher { get; set; }
                }

                public partial class MoveSystem : MoveSystemBase
                {
                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void PublishSystemOfPublisher_ShouldNotReport()
        {
            const string source = """
                using Amenone.EcsBridge;
                using MessagePipe;

                public sealed class Moved {}

                public partial class MoveSystem : PublishSystem<IPublisher<Moved>>
                {
                    protected override void PublishChanges() => Publisher.Publish(new Moved());
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void PublishSystemOfBundle_AndBundleItself_ShouldNotReport()
        {
            const string source = """
                using Amenone.EcsBridge;
                using MessagePipe;

                public sealed class Moved {}

                public sealed class Stopped {}

                public sealed class MovePublishers
                {
                    public MovePublishers(IPublisher<Moved> moved, IPublisher<Stopped> stopped)
                    {
                        Moved = moved;
                        Stopped = stopped;
                    }

                    public IPublisher<Moved> Moved { get; }

                    public IPublisher<Stopped> Stopped { get; }
                }

                public partial class MoveSystem : PublishSystem<MovePublishers>
                {
                    protected override void PublishChanges() => Publisher.Moved.Publish(new Moved());
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void PublishSystemDerivedThroughAbstractClass_HoldingPublisher_ShouldNotReport()
        {
            const string source = """
                using Amenone.EcsBridge;
                using MessagePipe;

                public sealed class Moved {}

                public abstract partial class MovePublishSystem : PublishSystem<IPublisher<Moved>>
                {
                    private IPublisher<Moved> _extra;
                }

                public partial class MoveSystem : MovePublishSystem
                {
                    public IAsyncPublisher<Moved> AsyncPublisher { get; set; }

                    protected override void PublishChanges() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void NonSystemClass_Publisher_ShouldNotReport()
        {
            const string source = """
                using MessagePipe;

                public sealed class Moved {}

                public sealed class Mover
                {
                    private readonly IPublisher<Moved> _publisher;

                    public Mover(IPublisher<Moved> publisher) => _publisher = publisher;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SystemBase_SubscriberOnly_ShouldReportOnlyAG0007()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private ISubscriber<Moved> _subscriber;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0007" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SystemBase_SubscriberAndPublisher_ShouldReportAG0007AndAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private ISubscriber<Moved> _subscriber;

                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0007", "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void Compilation_WithoutEntities_ShouldNotReport()
        {
            const string source = """
                using MessagePipe;

                namespace Game
                {
                    public abstract class SystemBase {}

                    public sealed class Moved {}

                    public sealed class MoveSystem : SystemBase
                    {
                        private IPublisher<Moved> _publisher;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.MessagePipe, LibraryStub.EcsBridge));
        }

        [Fact]
        public void Compilation_WithoutMessagePipe_ShouldNotReport()
        {
            const string source = """
                using Unity.Entities;

                namespace Game
                {
                    public interface IPublisher<TMessage> {}

                    public sealed class Moved {}

                    public partial class MoveSystem : SystemBase
                    {
                        private IPublisher<Moved> _publisher;

                        protected override void OnUpdate() {}
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities, LibraryStub.EcsBridge));
        }

        [Fact]
        public void Compilation_WithoutEcsBridge_ShouldNotReport()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities, LibraryStub.MessagePipe));
        }

        [Fact]
        public void OwnSystemBaseAndPublisherLookalikes_ShouldNotReport()
        {
            const string source = """
                namespace Game
                {
                    public abstract class SystemBase {}

                    public interface IPublisher<TMessage> {}

                    public sealed class Moved {}

                    public sealed class OwnBaseSystem : SystemBase
                    {
                        private MessagePipe.IPublisher<Moved> _publisher;
                    }

                    public partial class OwnPublisherSystem : Unity.Entities.SystemBase
                    {
                        private IPublisher<Moved> _publisher;

                        protected override void OnUpdate() {}
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void OwnPublishSystemLookalike_Publisher_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                namespace Game
                {
                    public abstract partial class PublishSystem<TPublisher> : SystemBase where TPublisher : class {}

                    public sealed class Moved {}

                    public partial class MoveSystem : PublishSystem<IPublisher<Moved>>
                    {
                        private IPublisher<Moved> _publisher;

                        protected override void OnUpdate() {}
                    }
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Theory]
        [InlineData("Game.Logic.Tests")]
        [InlineData("Game_Ecs-TEST")]
        public void TestAssembly_PublishingSystem_ShouldNotReport(string assemblyName)
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn(assemblyName, source, AllLibraries));
        }

        [Fact]
        public void AssemblyReferencingTestRunner_PublishingSystem_ShouldNotReport()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsIn("Game.Ecs.Spec", source, AllLibraries.Append(LibraryStub.TestRunner).ToArray()));
        }

        [Fact]
        public void GeneratedFile_PublishingSystem_ShouldNotReport()
        {
            const string source = """
                // <auto-generated/>
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void GeneratedType_PublishingSystem_ShouldNotReport()
        {
            const string source = """
                using System.CodeDom.Compiler;
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                [GeneratedCode("ArchitectureGuardTests", "1.0")]
                public partial class MoveSystem : SystemBase
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void PragmaDisableAG0008_ShouldReportAG0005()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                #pragma warning disable AG0008
                public partial class MoveSystem : SystemBase
                #pragma warning restore AG0008
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void SuppressionDiagnostic_ShouldListAG0008AsRuleWithoutAttribute()
        {
            const string source = """
                public sealed class Mover
                {
                #pragma warning disable AG0008
                #pragma warning restore AG0008
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source));

            Assert.Contains("属性の無い AG0006〜AG0008 はアセンブリ単位のルールセットで外して", diagnostic.GetMessage());
        }

        [Theory]
        [InlineData("MoveSystem.Generated.cs", "")]
        [InlineData("MoveSystem.g.cs", "")]
        [InlineData("", "// <auto-generated/>")]
        public void PartialSystemFirstDeclaredInGeneratedFile_ShouldReportAG0008AtHandWrittenDeclaration(string generatedPath, string generatedHeader)
        {
            var generated = new SourceFile(generatedPath, $$"""
                {{generatedHeader}}
                public partial class MoveSystem : Unity.Entities.SystemBase
                {
                    protected override void OnCreate() {}
                }
                """);
            var handWritten = new SourceFile("MoveSystem.cs", """
                using MessagePipe;

                public sealed class Moved {}

                public partial class MoveSystem
                {
                    private IPublisher<Moved> _publisher;

                    protected override void OnUpdate() {}
                }
                """);

            var diagnostic = Assert.Single(AnalyzerRunner.AnalyzeFiles(new[] { generated, handWritten }, AllLibraries));

            Assert.Equal("AG0008", diagnostic.Id);
            Assert.Equal("MoveSystem.cs", diagnostic.Location.SourceTree!.FilePath);
            Assert.Equal("MoveSystem", SourceTextAt(diagnostic));
        }

        [Fact]
        public void GenericSystem_PublisherOfTypeParameter_ShouldReportAG0008()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public partial class ChangePublishSystem<TMessage> : SystemBase
                {
                    private IPublisher<TMessage> _publisher;

                    protected override void OnUpdate() {}
                }
                """;

            Assert.Equal(new[] { "AG0008" }, AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void GenericPublishSystemDerived_ShouldNotReport()
        {
            const string source = """
                using Amenone.EcsBridge;
                using MessagePipe;

                public abstract partial class ChangePublishSystem<TMessage> : PublishSystem<IPublisher<TMessage>> where TMessage : class
                {
                    private IAsyncPublisher<TMessage> _asyncPublisher;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, AllLibraries));
        }

        [Fact]
        public void ClassImplementingISystem_Publisher_ShouldReportAG0008WithBaseClassGuidance()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public sealed class MoveSystem : ISystem
                {
                    private IPublisher<Moved> _publisher;
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source, libraries: AllLibraries));

            Assert.Equal("AG0008", diagnostic.Id);
            Assert.Contains("PublishSystem<TPublisher> を継いでください。", diagnostic.GetMessage());
            Assert.DoesNotContain("ISystem の struct", diagnostic.GetMessage());
        }

        [Fact]
        public void SystemBase_PublisherInExplicitInterfaceImplementation_ShouldReportAG0008Once()
        {
            const string source = """
                using MessagePipe;
                using Unity.Entities;

                public sealed class Moved {}

                public interface IPublisherBinder
                {
                    void Bind(IPublisher<Moved> publisher);
                }

                public partial class MoveSystem : SystemBase, IPublisherBinder
                {
                    void IPublisherBinder.Bind(IPublisher<Moved> publisher) {}

                    protected override void OnUpdate() {}
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source, libraries: AllLibraries));

            Assert.Equal("AG0008", diagnostic.Id);
            Assert.Equal("MoveSystem", SourceTextAt(diagnostic));
        }

        private static string SourceTextAt(Diagnostic diagnostic) =>
            diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
    }
}
