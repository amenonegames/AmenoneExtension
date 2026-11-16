using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class WithoutUnityEngineTests
    {
        [Fact]
        public void Compilation_WithoutUnityEngine_ShouldReportNothing()
        {
            const string source = """
                namespace Domain
                {
                    public class MonoBehaviour {}

                    public static class Time
                    {
                        public static float deltaTime => 0f;
                        public static float unscaledDeltaTime => 0f;
                        public static float timeScale { get; set; }
                    }

                    public sealed class View : MonoBehaviour
                    {
                        private void Start() => Time.timeScale = Time.deltaTime + Time.unscaledDeltaTime;

                        private void Update() {}
                    }
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIdsWithoutUnityEngine(source));
        }
    }
}
