using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePipe;
using NUnit.Framework;
using RootContainer;
using VContainer;
using CompilationPipeline = UnityEditor.Compilation.CompilationPipeline;

namespace Messaging.Tests
{
    public sealed class InjectedBrokerTest
    {
        private const BindingFlags DeclaredInstanceMembers =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [Test]
        public void InjectedAsyncBrokers_AllInLibrary_ShouldBeResolvableAfterRegisterDialogueMessaging()
        {
            var brokerTypes = InjectedBrokerTypes(typeof(IAsyncPublisher<>), typeof(IAsyncSubscriber<>));
            var builder = new ContainerBuilder();
            builder.RegisterDialogueMessaging();
            using var container = builder.Build();

            var unresolvable = brokerTypes.Where(type => !CanResolve(container, type)).Select(type => type.ToString());

            Assert.That(brokerTypes, Is.Not.Empty);
            Assert.That(unresolvable, Is.Empty);
        }

        [Test]
        public void InjectedAsyncBrokers_AllInLibrary_ShouldBeRegisteredByRegisterDialogueMessageBrokers()
        {
            var brokerTypes = InjectedBrokerTypes(typeof(IAsyncPublisher<>), typeof(IAsyncSubscriber<>));
            var builder = new ContainerBuilder();
            var options = builder.RegisterMessagePipe();
            builder.RegisterDialogueMessageBrokers(options);

            var unregistered = brokerTypes.Where(type => !builder.Exists(type, includeInterfaceTypes: true)).Select(type => type.ToString());

            Assert.That(brokerTypes, Is.Not.Empty);
            Assert.That(unregistered, Is.Empty);
        }

        [Test]
        public void InjectedSyncBrokers_InLibrary_ShouldBeNone()
        {
            var brokerTypes = InjectedBrokerTypes(typeof(IPublisher<>), typeof(ISubscriber<>));

            Assert.That(brokerTypes.Select(type => type.ToString()), Is.Empty);
        }

        private static Type[] InjectedBrokerTypes(params Type[] brokerDefinitions) =>
            LibraryTypes()
                .SelectMany(InjectedTypes)
                .Where(type => type.IsGenericType && !type.ContainsGenericParameters)
                .Where(type => brokerDefinitions.Contains(type.GetGenericTypeDefinition()))
                .Distinct()
                .ToArray();

        private static IEnumerable<Type> LibraryTypes() =>
            CompilationPipeline.GetAssemblies()
                .Select(assembly => assembly.name)
                .Where(IsLibraryAssemblyName)
                .Select(Assembly.Load)
                .SelectMany(LoadableTypes);

        private static bool IsLibraryAssemblyName(string name) =>
            name.StartsWith("amenone.dialogue.") && !name.EndsWith(".tests");

        private static IEnumerable<Type> LoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        private static IEnumerable<Type> InjectedTypes(Type type) =>
            type.GetConstructors(DeclaredInstanceMembers).SelectMany(ParameterTypes)
                .Concat(type.GetMethods(DeclaredInstanceMembers).Where(IsInjected).SelectMany(ParameterTypes))
                .Concat(type.GetFields(DeclaredInstanceMembers).Where(IsInjected).Select(field => field.FieldType))
                .Concat(type.GetProperties(DeclaredInstanceMembers).Where(IsInjected).Select(property => property.PropertyType));

        private static IEnumerable<Type> ParameterTypes(MethodBase method) =>
            method.GetParameters().Select(parameter => parameter.ParameterType);

        private static bool IsInjected(MemberInfo member) => member.IsDefined(typeof(InjectAttribute), true);

        private static bool CanResolve(IObjectResolver container, Type type)
        {
            try
            {
                container.Resolve(type);
                return true;
            }
            catch (VContainerException)
            {
                return false;
            }
        }
    }
}
