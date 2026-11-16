using System.Collections.Generic;
using System.Linq;
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
            var project = ScriptableObject.CreateInstance<YarnProject>();
            project.compiledYarnProgram = Compile(source).Program.ToByteArray();
            return project;
        }

        public static Dictionary<string, string> LineTexts(string source) =>
            Compile(source).StringTable.ToDictionary(entry => entry.Key, entry => entry.Value.text);

        private static CompilationResult Compile(string source) =>
            Yarn.Compiler.Compiler.Compile(CompilationJob.CreateFromString("test.yarn", source));
    }
}
