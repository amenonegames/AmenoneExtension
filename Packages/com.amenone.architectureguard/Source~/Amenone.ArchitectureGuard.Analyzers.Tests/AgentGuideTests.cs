using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class AgentGuideTests
    {
        private const int MaxLineCount = 180;
        private const string DiagnosticsChapterHeading = "## 9. ";

        private static readonly Lazy<string[]> GuideLines = new Lazy<string[]>(() =>
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "AgentGuide", "AGENT_GUIDE.md")));

        [Fact]
        public void DescriptorIds_ShouldBeEightDistinctIds()
        {
            var ids = DescriptorIds();

            Assert.NotEmpty(ids);
            Assert.Equal(8, ids.Length);
            Assert.Equal(ids.Length, ids.Distinct().Count());
        }

        [Fact]
        public void DiagnosticsChapter_EveryDescriptorId_ShouldAppearAsHeadingOnce()
        {
            var chapter = string.Join("\n", DiagnosticsChapterLines());

            Assert.All(DescriptorIds(), id => Assert.Single(Regex.Matches(chapter, Regex.Escape($"**{id}**"))));
        }

        [Fact]
        public void Guide_EveryMentionedId_ShouldBeDescriptorId()
        {
            var mentioned = GuideLines.Value
                .SelectMany(line => Regex.Matches(line, @"AG\d{4}").Select(match => match.Value))
                .Distinct()
                .ToArray();

            Assert.NotEmpty(mentioned);
            Assert.All(mentioned, id => Assert.Contains(id, DescriptorIds()));
        }

        [Fact]
        public void Guide_LineCount_ShouldBeWithinLimit() =>
            Assert.InRange(GuideLines.Value.Length, 1, MaxLineCount);

        private static string[] DiagnosticsChapterLines()
        {
            var lines = GuideLines.Value;
            var start = Array.FindIndex(lines, line => line.StartsWith(DiagnosticsChapterHeading, StringComparison.Ordinal));
            Assert.True(start >= 0, "9 章の見出しが見つからない");
            return lines.Skip(start + 1).TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        }

        private static string[] DescriptorIds() =>
            typeof(UnityTimeAnalyzer).Assembly
                .GetType("Amenone.ArchitectureGuard.Analyzers.ArchitectureGuardDiagnostics", throwOnError: true)!
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
                .Select(field => ((DiagnosticDescriptor)field.GetValue(null)!).Id)
                .ToArray();
    }
}
