using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator.Tests
{
    internal sealed class LibraryStub
    {
        public static readonly LibraryStub UnityEngine = new LibraryStub("UnityEngine", @"
namespace UnityEngine
{
    public static class Debug
    {
        public static readonly System.Collections.Generic.List<string> Messages = new System.Collections.Generic.List<string>();

        public static void Log(object message) => Messages.Add(""Log:"" + message);

        public static void LogError(object message) => Messages.Add(""LogError:"" + message);

        public static void LogWarning(object message) => Messages.Add(""LogWarning:"" + message);
    }
}");

        public static readonly LibraryStub UniTask = new LibraryStub("UniTask", @"
namespace Cysharp.Threading.Tasks
{
    public readonly struct UniTask
    {
        private readonly object _source;

        public UniTask(object source) => _source = source;

        public bool IsCompleted => _source == null;
    }

    public readonly struct UniTask<T>
    {
        private readonly object _source;
        private readonly T _result;

        public UniTask(T result)
        {
            _source = new object();
            _result = result;
        }

        public bool IsCompleted => _source == null;
    }
}");

        private readonly Lazy<byte[]> _image;

        public LibraryStub(string assemblyName, string source, params LibraryStub[] dependencies)
            : this(assemblyName, new Lazy<byte[]>(() => GeneratorRunner.EmitLibrary(assemblyName, source, dependencies)), dependencies)
        {
        }

        private LibraryStub(string assemblyName, Lazy<byte[]> image, IReadOnlyList<LibraryStub> dependencies)
        {
            AssemblyName = assemblyName;
            Dependencies = dependencies;
            _image = image;
        }

        public string AssemblyName { get; }

        public IReadOnlyList<LibraryStub> Dependencies { get; }

        public byte[] Image => _image.Value;

        public MetadataReference Reference => MetadataReference.CreateFromImage(Image);

        public static LibraryStub FromImage(string assemblyName, byte[] image) =>
            new LibraryStub(assemblyName, new Lazy<byte[]>(() => image), Array.Empty<LibraryStub>());

        public IEnumerable<LibraryStub> WithDependencies() => Dependencies.SelectMany(dependency => dependency.WithDependencies()).Append(this);
    }
}
