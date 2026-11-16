using Google.Protobuf;
using UnityEngine;
using Yarn.Compiler;
using Yarn.Unity;

namespace Logic.Tests
{
    internal static class CompiledYarnProject
    {
        public static YarnProject Create(string source)
        {
            var result = Yarn.Compiler.Compiler.Compile(CompilationJob.CreateFromString("test.yarn", source));
            var project = ScriptableObject.CreateInstance<YarnProject>();
            project.compiledYarnProgram = result.Program.ToByteArray();
            return project;
        }
    }
}
