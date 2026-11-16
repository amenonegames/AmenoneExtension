using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class UnityTimeAnalyzerTests
    {
        private static string InMethod(string body) => $$"""
            using System;
            using UnityEngine;
            using Amenone.ArchitectureGuard;

            public sealed class Target
            {
                public void Run()
                {
                    {{body}}
                }
            }
            """;

        [Fact]
        public void Time_DeltaTimeRead_ShouldReportAG0001() =>
            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(InMethod("var elapsed = Time.deltaTime;")));

        [Theory]
        [InlineData("time")]
        [InlineData("timeAsDouble")]
        [InlineData("smoothDeltaTime")]
        [InlineData("fixedDeltaTime")]
        [InlineData("fixedTime")]
        [InlineData("fixedTimeAsDouble")]
        [InlineData("timeSinceLevelLoad")]
        [InlineData("timeSinceLevelLoadAsDouble")]
        public void Time_ScaledPropertyRead_ShouldReportAG0001(string propertyName) =>
            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(InMethod($"var value = Time.{propertyName};")));

        [Fact]
        public void Time_TimeScaleRead_ShouldNotReport() =>
            Assert.Empty(AnalyzerRunner.AnalyzeIds(InMethod("var scale = Time.timeScale;")));

        [Fact]
        public void Time_FrameCountRead_ShouldNotReport() =>
            Assert.Empty(AnalyzerRunner.AnalyzeIds(InMethod("var frame = Time.frameCount;")));

        [Theory]
        [InlineData("Time.timeScale = 0f;")]
        [InlineData("Time.timeScale *= 0.5f;")]
        [InlineData("Time.timeScale++;")]
        [InlineData("--Time.timeScale;")]
        [InlineData("(Time.timeScale, var other) = (1f, 2);")]
        public void Time_TimeScaleWrite_ShouldReportAG0001(string statement) =>
            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(InMethod(statement)));

        [Theory]
        [InlineData("Time.fixedDeltaTime = 0.02f;")]
        [InlineData("Time.maximumDeltaTime = 0.1f;")]
        [InlineData("Time.maximumParticleDeltaTime = 0.03f;")]
        [InlineData("Time.captureFramerate = 60;")]
        [InlineData("Time.captureDeltaTime = 1f / 60f;")]
        [InlineData("Time.fixedDeltaTime += 0.01f;")]
        [InlineData("Time.captureFramerate++;")]
        public void Time_SettingWrite_ShouldReportSingleAG0001AsAssignment(string statement)
        {
            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(InMethod(statement)));

            Assert.Equal("AG0001", diagnostic.Id);
            Assert.Contains("への代入", diagnostic.GetMessage());
            Assert.DoesNotContain("読んでいます", diagnostic.GetMessage());
        }

        [Fact]
        public void Time_DeltaTimeRead_ShouldDescribeRead()
        {
            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(InMethod("var elapsed = Time.deltaTime;")));

            Assert.Contains("UnityEngine.Time.deltaTime を直接読んでいます", diagnostic.GetMessage());
        }

        [Fact]
        public void Time_FieldInitializerWithoutAllow_ShouldReportAG0001()
        {
            const string source = """
                using UnityEngine;

                public sealed class Target
                {
                    private readonly float _startedAt = Time.time;
                }
                """;

            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideAllowedFieldInitializerLambda_ShouldNotReport()
        {
            const string source = """
                using System;
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("時計を進める入口へ渡す読み取り関数なので")]
                    private readonly Func<float> _reader = () => Time.deltaTime;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideAllowedEventAccessors_ShouldNotReport()
        {
            const string source = """
                using System;
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("購読した時刻を計測のために記録するため")]
                    public event Action Changed
                    {
                        add => Record(Time.realtimeSinceStartup);
                        remove => Record(Time.unscaledTime);
                    }

                    private static void Record(float value) {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideStaticLocalFunctionOfAllowedMethod_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("時計を進める唯一の入口なので実時間を読む")]
                    public float Tick()
                    {
                        return Read();

                        static float Read() => Time.unscaledDeltaTime;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideAllowedExpressionBodiedMethod_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("Unity の時間を時計へ橋渡しする実装なので")]
                    public float DeltaTime() => Time.deltaTime;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Theory]
        [InlineData("unscaledDeltaTime")]
        [InlineData("unscaledTime")]
        [InlineData("unscaledTimeAsDouble")]
        [InlineData("fixedUnscaledDeltaTime")]
        [InlineData("fixedUnscaledTime")]
        [InlineData("fixedUnscaledTimeAsDouble")]
        [InlineData("realtimeSinceStartup")]
        [InlineData("realtimeSinceStartupAsDouble")]
        public void Time_UnscaledPropertyRead_ShouldReportAG0002(string propertyName) =>
            Assert.Equal(new[] { "AG0002" }, AnalyzerRunner.AnalyzeIds(InMethod($"var value = Time.{propertyName};")));

        [Fact]
        public void Time_UsingStaticDeltaTimeRead_ShouldReportAG0001()
        {
            const string source = """
                using static UnityEngine.Time;

                public sealed class Target
                {
                    public float Elapsed() => deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_AliasedDeltaTimeRead_ShouldReportAG0001()
        {
            const string source = """
                using Clock = UnityEngine.Time;

                public sealed class Target
                {
                    public float Elapsed() => Clock.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_UserDefinedTimeType_ShouldNotReport()
        {
            const string source = """
                namespace Game
                {
                    public static class Time
                    {
                        public static float deltaTime => 0f;
                    }

                    public sealed class Target
                    {
                        public float Elapsed() => Time.deltaTime;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_NameOfDeltaTime_ShouldNotReport() =>
            Assert.Empty(AnalyzerRunner.AnalyzeIds(InMethod("var name = nameof(Time.deltaTime);")));

        [Fact]
        public void Time_ReadInsideAllowedMethod_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("時計を進める唯一の入口なので実時間を読む")]
                    public void Tick()
                    {
                        var elapsed = Time.unscaledDeltaTime;
                        Time.timeScale = 1f;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideAllowedPropertyAndConstructor_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    private readonly double _startedAt;

                    [AllowUnityTime("処理時間の計測の起点として実時間を記録する")]
                    public Target() => _startedAt = Time.realtimeSinceStartupAsDouble;

                    [AllowUnityTime("処理時間の計測のために実時間との差を返す")]
                    public double Elapsed => Time.realtimeSinceStartupAsDouble - _startedAt;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideAllowedType_ShouldNotReport()
        {
            const string source = """
                using System;
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                [AllowUnityTime("Unity の時間を層ごとの時計へ橋渡しする実装なので")]
                public sealed class UnityClock
                {
                    public float DeltaTime => Time.deltaTime;

                    public Func<float> Lambda() => () => Time.deltaTime;

                    public float LocalFunction()
                    {
                        return Read();

                        float Read() => Time.unscaledDeltaTime;
                    }

                    public sealed class Nested
                    {
                        public float Read() => Time.deltaTime;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideLambdaOfAllowedMethod_ShouldNotReport()
        {
            const string source = """
                using System;
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("時計を進める入口が渡すコールバックなので")]
                    public Func<float> CreateReader() => () => Time.deltaTime;
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadInsideAllowedLocalFunction_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    public float Run()
                    {
                        return Measure();

                        [AllowUnityTime("処理時間の計測のために実時間を読む")]
                        static float Measure() => Time.realtimeSinceStartup;
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void Time_ReadOutsideAllowedMember_ShouldReportAG0001()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Target
                {
                    [AllowUnityTime("時計を進める唯一の入口なので実時間を読む")]
                    public float Allowed() => Time.unscaledDeltaTime;

                    public float NotAllowed() => Time.deltaTime;
                }
                """;

            Assert.Equal(new[] { "AG0001" }, AnalyzerRunner.AnalyzeIds(source));
        }
    }
}
