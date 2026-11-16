using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class AllowReasonAnalyzerTests
    {
        private static string WithAllowUnityTime(string reasonExpression) => $$"""
            using UnityEngine;
            using Amenone.ArchitectureGuard;

            public sealed class Target
            {
                private static readonly string DynamicReason = "実行時に決まる理由の文字列です";
                private const string ConstantReason = "定数に置いた十分な長さの理由";

                [AllowUnityTime({{reasonExpression}})]
                public float Elapsed() => Time.deltaTime;
            }
            """;

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"   \"")]
        [InlineData("\" 短 い 理 由 \"")]
        [InlineData("\"123456789\"")]
        [InlineData("null")]
        public void AllowUnityTime_InvalidReason_ShouldReportAG0004(string reasonExpression) =>
            Assert.Equal(new[] { "AG0004" }, AnalyzerRunner.AnalyzeIds(WithAllowUnityTime(reasonExpression)));

        [Fact]
        public void AllowUnityTime_NonConstantReason_ShouldReportAG0004() =>
            Assert.Equal(new[] { "AG0004" }, AnalyzerRunner.AnalyzeIdsAllowingCompileErrors(WithAllowUnityTime("DynamicReason")));

        [Theory]
        [InlineData("\"1234567890\"")]
        [InlineData("ConstantReason")]
        [InlineData("reason: \"名前付き引数で書いた十分な理由\"")]
        public void AllowUnityTime_ValidReason_ShouldNotReport(string reasonExpression) =>
            Assert.Empty(AnalyzerRunner.AnalyzeIds(WithAllowUnityTime(reasonExpression)));

        [Fact]
        public void AllowUnityMessage_ShortReason_ShouldReportAG0004()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class View : MonoBehaviour
                {
                    [AllowUnityMessage("必要")]
                    private void Start() {}
                }
                """;

            Assert.Equal(new[] { "AG0004" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void AliasedAllowUnityTime_ShortReason_ShouldReportAG0004()
        {
            const string source = """
                using UnityEngine;
                using Permit = Amenone.ArchitectureGuard.AllowUnityTimeAttribute;

                public sealed class Target
                {
                    [Permit("短い")]
                    public float Elapsed() => Time.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0004" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void AllowUnityTime_ShortReason_ShouldExplainCurrentLength()
        {
            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(WithAllowUnityTime("\"短い\"")));

            Assert.Contains("[AllowUnityTime] の理由が短すぎます（空白を除いて 2 文字）", diagnostic.GetMessage());
        }
    }
}
