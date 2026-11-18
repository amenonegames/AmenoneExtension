namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal static class EntitiesStub
    {
        public const string Source = @"
namespace Unity.Collections
{
    public enum Allocator { Temp }
}

namespace Unity.Entities
{
    using Unity.Collections;

    public struct EntityQuery
    {
        public bool IsEmpty => true;
        public int CalculateEntityCount() => 0;
    }

    public struct ComponentType
    {
        public static ComponentType ReadOnly<T>() => default;
    }

    public struct EntityManager
    {
        public EntityQuery CreateEntityQuery(params ComponentType[] componentTypes) => default;
        public EntityQuery UniversalQuery => default;
    }

    public struct SystemState
    {
        public EntityManager EntityManager => default;
        public EntityQuery GetEntityQuery(params ComponentType[] componentTypes) => default;
    }

    public abstract class ComponentSystemBase
    {
        public EntityManager EntityManager => default;
        protected internal EntityQuery GetEntityQuery(params ComponentType[] componentTypes) => default;
        protected virtual void OnCreate() {}
    }

    public abstract class SystemBase : ComponentSystemBase
    {
        protected abstract void OnUpdate();
    }

    public interface ISystem
    {
        void OnCreate(ref SystemState state) {}
        void OnUpdate(ref SystemState state) {}
        void OnDestroy(ref SystemState state) {}
    }

    public struct EntityQueryBuilder
    {
        public EntityQueryBuilder(Allocator allocator) {}
        public EntityQueryBuilder WithAll<T>() => this;
        public EntityQuery Build(ref SystemState systemState) => default;
        public EntityQuery Build(SystemBase systemBase) => default;
        public EntityQuery Build(EntityManager entityManager) => default;
    }

    public struct SystemAPIQueryBuilder
    {
        public SystemAPIQueryBuilder WithAll<T>() => this;
        public EntityQuery Build() => default;
    }

    public struct QueryEnumerable<T>
    {
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() => null;
    }

    public static class SystemAPI
    {
        public static SystemAPIQueryBuilder QueryBuilder() => default;
        public static QueryEnumerable<T> Query<T>() => default;
        public static T GetSingleton<T>() => default;
    }
}
";
    }
}
