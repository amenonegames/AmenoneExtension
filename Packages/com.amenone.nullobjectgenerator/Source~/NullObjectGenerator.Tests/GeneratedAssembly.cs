using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Amenone.NullObjectGenerator.Tests
{
    internal sealed class GeneratedAssembly
    {
        private readonly Assembly _assembly;
        private readonly LoadContext _context;

        private GeneratedAssembly(GeneratorResult result)
        {
            var images = result.Libraries.ToDictionary(library => library.AssemblyName, library => library.Image);
            _context = new LoadContext(images);
            _assembly = _context.LoadFromStream(new MemoryStream(GeneratorRunner.Emit(result.Output)));
        }

        public static GeneratedAssembly Load(GeneratorResult result) => new GeneratedAssembly(result);

        public object Create(string typeName) => Activator.CreateInstance(_assembly.GetType(typeName, throwOnError: true)!)!;

        public object? Invoke(object instance, string interfaceName, string methodName, params object?[] arguments)
        {
            var method = _assembly.GetType(interfaceName, throwOnError: true)!.GetMethod(methodName)!;
            try
            {
                return method.Invoke(instance, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }

        public Type ReturnType(string interfaceName, string methodName) =>
            _assembly.GetType(interfaceName, throwOnError: true)!.GetMethod(methodName)!.ReturnType;

        public IReadOnlyList<string> UnityMessages()
        {
            var debug = _context.LoadFromAssemblyName(new AssemblyName("UnityEngine")).GetType("UnityEngine.Debug", throwOnError: true)!;
            return (List<string>)debug.GetField("Messages")!.GetValue(null)!;
        }

        private sealed class LoadContext : AssemblyLoadContext
        {
            private readonly IReadOnlyDictionary<string, byte[]> _images;

            public LoadContext(IReadOnlyDictionary<string, byte[]> images)
                : base(isCollectible: true) =>
                _images = images;

            protected override Assembly? Load(AssemblyName assemblyName) =>
                assemblyName.Name != null && _images.TryGetValue(assemblyName.Name, out var image)
                    ? LoadFromStream(new MemoryStream(image))
                    : null;
        }
    }
}
