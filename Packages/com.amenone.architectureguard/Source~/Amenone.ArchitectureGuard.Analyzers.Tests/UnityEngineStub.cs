namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal static class UnityEngineStub
    {
        public const string Source = @"
namespace UnityEngine
{
    public class Object {}
    public class Component : Object {}
    public class Behaviour : Component {}
    public class MonoBehaviour : Behaviour {}

    public static class Time
    {
        public static float deltaTime => 0f;
        public static float time => 0f;
        public static double timeAsDouble => 0d;
        public static float smoothDeltaTime => 0f;
        public static float fixedDeltaTime { get; set; }
        public static float fixedTime => 0f;
        public static double fixedTimeAsDouble => 0d;
        public static float timeSinceLevelLoad => 0f;
        public static double timeSinceLevelLoadAsDouble => 0d;
        public static float unscaledDeltaTime => 0f;
        public static float unscaledTime => 0f;
        public static double unscaledTimeAsDouble => 0d;
        public static float fixedUnscaledDeltaTime => 0f;
        public static float fixedUnscaledTime => 0f;
        public static double fixedUnscaledTimeAsDouble => 0d;
        public static float realtimeSinceStartup => 0f;
        public static double realtimeSinceStartupAsDouble => 0d;
        public static float timeScale { get; set; }
        public static float maximumDeltaTime { get; set; }
        public static float maximumParticleDeltaTime { get; set; }
        public static int captureFramerate { get; set; }
        public static float captureDeltaTime { get; set; }
        public static int frameCount => 0;
    }
}
";
    }
}
