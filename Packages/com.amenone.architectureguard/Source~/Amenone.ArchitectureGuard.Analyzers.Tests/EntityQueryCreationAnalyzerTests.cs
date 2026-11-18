using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class EntityQueryCreationAnalyzerTests
    {
        [Theory]
        [InlineData("GetEntityQuery(ComponentType.ReadOnly<int>())")]
        [InlineData("EntityManager.CreateEntityQuery(ComponentType.ReadOnly<int>())")]
        [InlineData("new EntityQueryBuilder(Allocator.Temp).WithAll<int>().Build(this)")]
        [InlineData("new EntityQueryBuilder(Allocator.Temp).WithAll<int>().Build(EntityManager)")]
        public void SystemBase_QueryCreationInOnUpdate_ShouldReportAG0006(string creation)
        {
            var source = $$"""
                using Unity.Collections;
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var query = {{creation}};
                    }
                }
                """;

            Assert.Equal(new[] { "AG0006" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Theory]
        [InlineData("state.GetEntityQuery(ComponentType.ReadOnly<int>())")]
        [InlineData("state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<int>())")]
        [InlineData("new EntityQueryBuilder(Allocator.Temp).WithAll<int>().Build(ref state)")]
        public void ISystem_QueryCreationInOnUpdate_ShouldReportAG0006(string creation)
        {
            var source = $$"""
                using Unity.Collections;
                using Unity.Entities;

                public partial struct MoveSystem : ISystem
                {
                    public void OnUpdate(ref SystemState state)
                    {
                        var query = {{creation}};
                    }
                }
                """;

            Assert.Equal(new[] { "AG0006" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void ISystem_ExplicitOnUpdate_ShouldReportAG0006()
        {
            const string source = """
                using Unity.Entities;

                public partial struct MoveSystem : ISystem
                {
                    void ISystem.OnUpdate(ref SystemState state)
                    {
                        var query = state.GetEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Equal(new[] { "AG0006" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void SystemBaseGrandChild_QueryCreationInOnUpdate_ShouldReportAG0006()
        {
            const string source = """
                using Unity.Entities;

                public abstract partial class MoveSystemBase : SystemBase {}

                public partial class MoveSystem : MoveSystemBase
                {
                    protected override void OnUpdate()
                    {
                        var query = GetEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Equal(new[] { "AG0006" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void SystemBase_QueryCreationInLambdaAndLocalFunctionInOnUpdate_ShouldReportAG0006()
        {
            const string source = """
                using System;
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        Func<EntityQuery> create = () => GetEntityQuery(ComponentType.ReadOnly<int>());
                        var query = CreateQuery();

                        EntityQuery CreateQuery() => EntityManager.CreateEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Equal(new[] { "AG0006", "AG0006" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void QueryCreationInUpdate_ShouldNameSystemAndCall()
        {
            const string source = """
                using Unity.Entities;

                public partial struct MoveSystem : ISystem
                {
                    public void OnUpdate(ref SystemState state)
                    {
                        var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source, libraries: new[] { LibraryStub.Entities }));

            Assert.StartsWith("MoveSystem の OnUpdate の中で EntityManager.CreateEntityQuery により EntityQuery を作っています。", diagnostic.GetMessage());
        }

        [Fact]
        public void Systems_QueryCreationInOnCreate_ShouldNotReport()
        {
            const string source = """
                using Unity.Collections;
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    private EntityQuery _query;

                    protected override void OnCreate() => _query = GetEntityQuery(ComponentType.ReadOnly<int>());

                    protected override void OnUpdate()
                    {
                        var isEmpty = _query.IsEmpty;
                    }
                }

                public partial struct TurnSystem : ISystem
                {
                    private EntityQuery _query;

                    public void OnCreate(ref SystemState state) =>
                        _query = new EntityQueryBuilder(Allocator.Temp).WithAll<int>().Build(ref state);

                    public void OnUpdate(ref SystemState state)
                    {
                        var isEmpty = _query.IsEmpty;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void ISystem_SystemApiInOnUpdate_ShouldNotReport()
        {
            const string source = """
                using Unity.Entities;

                public partial struct MoveSystem : ISystem
                {
                    public void OnUpdate(ref SystemState state)
                    {
                        foreach (var speed in SystemAPI.Query<int>())
                        {
                        }

                        var settings = SystemAPI.GetSingleton<long>();
                        var query = SystemAPI.QueryBuilder().WithAll<int>().Build();
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void Systems_UniversalQueryAndCachedQueryUseInOnUpdate_ShouldNotReport()
        {
            const string source = """
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    private EntityQuery _query;

                    protected override void OnCreate() => _query = GetEntityQuery(ComponentType.ReadOnly<int>());

                    protected override void OnUpdate()
                    {
                        var everything = EntityManager.UniversalQuery;
                        var cached = _query;
                        var count = _query.CalculateEntityCount();
                    }
                }

                public partial struct TurnSystem : ISystem
                {
                    private EntityQuery _query;

                    public void OnCreate(ref SystemState state) => _query = state.GetEntityQuery(ComponentType.ReadOnly<int>());

                    public void OnUpdate(ref SystemState state)
                    {
                        var everything = state.EntityManager.UniversalQuery;
                        var count = _query.CalculateEntityCount();
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void Systems_OnUpdateOverloadAndStaticOnUpdate_ShouldNotReport()
        {
            const string source = """
                using Unity.Entities;

                public partial struct MoveSystem : ISystem
                {
                    public void OnUpdate(ref SystemState state) {}

                    public void OnUpdate(EntityManager entityManager)
                    {
                        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<int>());
                    }

                    public static void OnUpdate(ref SystemState state, int frame)
                    {
                        var query = state.GetEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }

                public partial class TurnSystem : SystemBase
                {
                    protected override void OnUpdate() {}

                    public static void OnUpdate(EntityManager entityManager)
                    {
                        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void NestedSystems_QueryCreationInOnUpdate_ShouldReportAG0006()
        {
            const string source = """
                using Unity.Entities;

                public static class MoveSystems
                {
                    public partial class MoveSystem : SystemBase
                    {
                        protected override void OnUpdate()
                        {
                            var query = GetEntityQuery(ComponentType.ReadOnly<int>());
                        }
                    }

                    public partial struct TurnSystem : ISystem
                    {
                        public void OnUpdate(ref SystemState state)
                        {
                            var query = state.GetEntityQuery(ComponentType.ReadOnly<int>());
                        }
                    }
                }
                """;

            Assert.Equal(new[] { "AG0006", "AG0006" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void GeneratedFile_QueryCreationInOnUpdate_ShouldNotReport()
        {
            const string source = """
                // <auto-generated/>
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var query = GetEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void GeneratedType_QueryCreationInOnUpdate_ShouldNotReport()
        {
            const string source = """
                using System.CodeDom.Compiler;
                using Unity.Entities;

                [GeneratedCode("ArchitectureGuardTests", "1.0")]
                public partial struct MoveSystem : ISystem
                {
                    public void OnUpdate(ref SystemState state)
                    {
                        var query = state.GetEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void SystemBase_QueryCreationInHelperCalledFromOnUpdate_ShouldNotReport()
        {
            const string source = """
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var query = CreateQuery();
                    }

                    private EntityQuery CreateQuery() => GetEntityQuery(ComponentType.ReadOnly<int>());
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void NonSystem_OnUpdateCreatingQuery_ShouldNotReport()
        {
            const string source = """
                using Unity.Entities;

                public sealed class QueryCache
                {
                    private readonly EntityManager _entityManager;

                    public QueryCache(EntityManager entityManager) => _entityManager = entityManager;

                    public void OnUpdate()
                    {
                        var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<int>());
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }

        [Fact]
        public void Compilation_WithoutEntities_OwnSystemBaseLookalike_ShouldReportNothing()
        {
            const string source = """
                namespace Game
                {
                    public struct EntityQuery {}

                    public abstract class SystemBase
                    {
                        protected EntityQuery GetEntityQuery() => default;

                        protected abstract void OnUpdate();
                    }

                    public interface ISystem
                    {
                        void OnUpdate();
                    }

                    public sealed class MoveSystem : SystemBase, ISystem
                    {
                        protected override void OnUpdate()
                        {
                            var query = GetEntityQuery();
                        }

                        void ISystem.OnUpdate() => GetEntityQuery();
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaDisableAG0006_ShouldReportAG0005()
        {
            const string source = """
                using Unity.Entities;

                public partial class MoveSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                #pragma warning disable AG0006
                        var query = GetEntityQuery(ComponentType.ReadOnly<int>());
                #pragma warning restore AG0006
                    }
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIdsWith(source, LibraryStub.Entities));
        }
    }
}
