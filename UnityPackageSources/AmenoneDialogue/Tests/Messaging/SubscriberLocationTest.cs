using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MessagePipe;
using NUnit.Framework;
using VContainer;
using CompilationPipeline = UnityEditor.Compilation.CompilationPipeline;

namespace Messaging.Tests
{
    public sealed class SubscriberLocationTest
    {
        private const string PresenterAssemblyName = "amenone.dialogue.presenter";

        private const BindingFlags DeclaredMembers =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly string MessagePipeNamespace = typeof(IAsyncSubscriber<>).Namespace;

        [Test]
        public void SubscriberHolders_OutsidePresenterAssembly_ShouldBeNone()
        {
            var violations = SubscriberHolders(NonPresenterAssemblies());

            Assert.That(violations, Is.Empty,
                $"Subscribing to MessagePipe messages must happen in the Presenter assembly ({PresenterAssemblyName}). " +
                "Outside it, no type may take a subscriber as a constructor parameter, an [Inject] method parameter, a field or a property. " +
                "Publishing is allowed anywhere. Violations:" + Environment.NewLine +
                string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void SubscriberHolders_InPresenterAssembly_ShouldBeFound() =>
            Assert.That(SubscriberHolders(PresenterAssemblies()), Is.Not.Empty);

        [Test]
        public void LibraryAssemblies_OutsidePresenter_ShouldIncludeLogicAndView() =>
            Assert.That(NonPresenterAssemblies().Select(AssemblyName), Is.SupersetOf(new[] { "amenone.dialogue.logic", "amenone.dialogue.view" }));

        private static string[] SubscriberHolders(IEnumerable<Assembly> assemblies) =>
            assemblies.SelectMany(LoadableTypes).SelectMany(SubscriberHoldings).ToArray();

        private static IEnumerable<string> SubscriberHoldings(Type type) =>
            HeldTypes(type)
                .Where(held => HoldsSubscriber(held.Type))
                .Select(held => $"{type.FullName} ({AssemblyName(type.Assembly)}): {held.Location} of type {held.Type}");

        private static IEnumerable<(string Location, Type Type)> HeldTypes(Type type) =>
            type.GetConstructors(DeclaredMembers)
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => ($"constructor parameter '{parameter.Name}'", parameter.ParameterType))
                .Concat(type.GetMethods(DeclaredMembers).Where(IsInjected).SelectMany(InjectedMethodParameters))
                .Concat(type.GetFields(DeclaredMembers).Where(IsDeclaredInSource).Select(field => ($"field '{field.Name}'", field.FieldType)))
                .Concat(type.GetProperties(DeclaredMembers).Select(property => ($"property '{property.Name}'", property.PropertyType)));

        private static IEnumerable<(string Location, Type Type)> InjectedMethodParameters(MethodInfo method) =>
            method.GetParameters().Select(parameter => ($"[Inject] method '{method.Name}' parameter '{parameter.Name}'", parameter.ParameterType));

        private static bool HoldsSubscriber(Type type) =>
            IsSubscriber(type)
            || (type.HasElementType && HoldsSubscriber(type.GetElementType()))
            || (type.IsGenericType && type.GetGenericArguments().Any(HoldsSubscriber));

        private static bool IsSubscriber(Type type) => IsSubscriberInterface(type) || type.GetInterfaces().Any(IsSubscriberInterface);

        private static bool IsSubscriberInterface(Type type) =>
            type.IsInterface && type.Namespace == MessagePipeNamespace && type.Name.Split('`')[0].EndsWith("Subscriber");

        private static bool IsInjected(MemberInfo member) => member.IsDefined(typeof(InjectAttribute), true);

        private static bool IsDeclaredInSource(FieldInfo field) => !field.IsDefined(typeof(CompilerGeneratedAttribute), false);

        private static IEnumerable<Assembly> PresenterAssemblies() => LibraryAssemblies().Where(IsPresenter);

        private static IEnumerable<Assembly> NonPresenterAssemblies() => LibraryAssemblies().Where(assembly => !IsPresenter(assembly));

        private static bool IsPresenter(Assembly assembly) => AssemblyName(assembly) == PresenterAssemblyName;

        private static string AssemblyName(Assembly assembly) => assembly.GetName().Name;

        private static IEnumerable<Assembly> LibraryAssemblies() =>
            CompilationPipeline.GetAssemblies()
                .Select(assembly => assembly.name)
                .Where(IsLibraryAssemblyName)
                .Select(Assembly.Load);

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
    }
}
