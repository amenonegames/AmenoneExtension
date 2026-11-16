using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class SuppressionAnalyzerTests
    {
        [Fact]
        public void PragmaWarningDisable_ArchitectureGuardId_ShouldReportAG0005()
        {
            const string source = """
                using UnityEngine;

                public sealed class Target
                {
                #pragma warning disable AG0001
                    public float Elapsed() => Time.deltaTime;
                #pragma warning restore AG0001
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaWarningDisable_WithoutId_ShouldReportAG0005()
        {
            const string source = """
                using UnityEngine;

                public sealed class Target
                {
                #pragma warning disable
                    public float Elapsed() => Time.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_ArchitectureGuardCheckId_ShouldReportAG0005()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;
                using UnityEngine;

                public sealed class Target
                {
                    [SuppressMessage("Amenone.ArchitectureGuard", "AG0001:Scaled time usage")]
                    public float Elapsed() => Time.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_NamedArchitectureGuardCheckId_ShouldReportAG0005()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                [assembly: SuppressMessage(checkId: "AG0003", category: "Amenone.ArchitectureGuard")]
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaWarningDisable_AfterDisablingAG0005_ShouldStillReportAG0005()
        {
            const string source = """
                using UnityEngine;

                public sealed class Target
                {
                #pragma warning disable AG0005
                #pragma warning disable AG0001
                    public float Elapsed() => Time.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0005", "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_AG0005OnMember_ShouldBeSuppressedAsKnownLimitation()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                public sealed class Target
                {
                    [SuppressMessage("Amenone.ArchitectureGuard", "AG0005")]
                    public void Run() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_AssemblyAG0005WithOtherArchitectureGuardId_ShouldSuppressBothAsKnownLimitation()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                [assembly: SuppressMessage("Amenone.ArchitectureGuard", "AG0005")]
                [assembly: SuppressMessage("Amenone.ArchitectureGuard", "AG0001")]
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaWarningDisable_MixedIds_ShouldReportOnlyAtArchitectureGuardId()
        {
            const string source = """
                public sealed class Target
                {
                #pragma warning disable AG0001, CS0168
                    public void Run() { int unused; }
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source));

            Assert.Equal("AG0005", diagnostic.Id);
            Assert.Equal("AG0001", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
        }

        [Fact]
        public void PragmaWarningDisable_InInactiveBranch_ShouldNotReport()
        {
            const string source = """
                public sealed class Target
                {
                #if ARCHITECTURE_GUARD_UNDEFINED_SYMBOL
                #pragma warning disable AG0001
                #endif
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_AssemblyWithScopeAndTarget_ShouldReportAG0005()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                [assembly: SuppressMessage("Amenone.ArchitectureGuard", "AG0001:Scaled time usage", Scope = "member", Target = "~M:Target.Run")]

                public sealed class Target
                {
                    public void Run() {}
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_Module_ShouldReportAG0005()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                [module: SuppressMessage("Amenone.ArchitectureGuard", "AG0003")]
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_AliasedAttribute_ShouldReportAG0005()
        {
            const string source = """
                using Quiet = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

                public sealed class Target
                {
                    [Quiet("Amenone.ArchitectureGuard", "AG0001")]
                    public void Run() {}
                }
                """;

            Assert.Equal(new[] { "AG0005" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Theory]
        [InlineData("ag0001")]
        [InlineData("AG1234")]
        public void PragmaWarningDisable_UnknownId_ShouldNotReport(string id)
        {
            var source = $$"""
                public sealed class Target
                {
                #pragma warning disable {{id}}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaWarningDisable_LowercaseId_ShouldNotSuppressAG0001()
        {
            const string source = """
                using UnityEngine;

                public sealed class Target
                {
                #pragma warning disable ag0001
                    public float Elapsed() => Time.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaWarningDisable_OtherId_ShouldNotReport()
        {
            const string source = """
                public sealed class Target
                {
                #pragma warning disable CS0169, IDE0051
                    private int _unused;
                #pragma warning restore CS0169, IDE0051
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PragmaWarningRestore_ArchitectureGuardId_ShouldNotReport()
        {
            const string source = """
                public sealed class Target
                {
                #pragma warning restore AG0001
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void SuppressMessage_OtherCheckId_ShouldNotReport()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                public sealed class Target
                {
                    [SuppressMessage("Style", "IDE0051:Remove unused private members")]
                    private void Unused() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }
    }
}
