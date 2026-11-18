using Xunit;

namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    public sealed class UnityMessageAnalyzerTests
    {
        [Theory]
        [InlineData("Awake")]
        [InlineData("Start")]
        [InlineData("OnDestroy")]
        [InlineData("Update")]
        [InlineData("LateUpdate")]
        [InlineData("FixedUpdate")]
        public void MonoBehaviour_MessageDeclaration_ShouldReportAG0003(string messageName)
        {
            var source = $$"""
                using UnityEngine;

                public sealed class View : MonoBehaviour
                {
                    private void {{messageName}}() {}
                }
                """;

            Assert.Equal(new[] { "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_CoroutineStart_ShouldReportAG0003()
        {
            const string source = """
                using System.Collections;
                using UnityEngine;

                public sealed class View : MonoBehaviour
                {
                    private IEnumerator Start() { yield break; }
                }
                """;

            Assert.Equal(new[] { "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviourGrandChild_Update_ShouldReportAG0003()
        {
            const string source = """
                using UnityEngine;

                public abstract class ViewBase : MonoBehaviour {}

                public sealed class View : ViewBase
                {
                    private void Update() {}
                }
                """;

            Assert.Equal(new[] { "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void NonMonoBehaviour_Start_ShouldNotReport()
        {
            const string source = """
                public interface IStartable
                {
                    void Start();
                }

                public sealed class EntryPoint : IStartable
                {
                    public void Start() {}

                    public void Update() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_OverrideStart_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public abstract class ViewBase : MonoBehaviour
                {
                    [AllowUnityMessage("外部ライブラリの基底クラスを模した宣言なので")]
                    protected virtual void Start() {}
                }

                public sealed class View : ViewBase
                {
                    protected override void Start() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_OnEnableAndOnDisable_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;

                public sealed class View : MonoBehaviour
                {
                    private void OnEnable() {}

                    private void OnDisable() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_OnDestroyWithParameter_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;

                public struct SystemState {}

                public sealed class View : MonoBehaviour
                {
                    public void OnDestroy(ref SystemState state) {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_AllowedStart_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public sealed class Bootstrap : MonoBehaviour
                {
                    [AllowUnityMessage("シーンの起点で合成ルートを組み立てるため")]
                    private void Start() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void PartialMonoBehaviour_BaseInOtherPart_ShouldReportAG0003()
        {
            const string source = """
                using UnityEngine;

                public partial class View : MonoBehaviour {}

                public partial class View
                {
                    private void Start() {}
                }
                """;

            Assert.Equal(new[] { "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void GenericMonoBehaviourBase_Messages_ShouldReportAG0003()
        {
            const string source = """
                using UnityEngine;

                public abstract class ViewBase<TModel> : MonoBehaviour
                {
                    private void Awake() {}
                }

                public sealed class View : ViewBase<int>
                {
                    private void Update() {}
                }
                """;

            Assert.Equal(new[] { "AG0003", "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_AsyncVoidStart_ShouldReportAG0003()
        {
            const string source = """
                using System.Threading.Tasks;
                using UnityEngine;

                public sealed class View : MonoBehaviour
                {
                    private async void Start() => await Task.Yield();
                }
                """;

            Assert.Equal(new[] { "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_ExplicitInterfaceStart_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;

                public interface IStartable
                {
                    void Start();
                }

                public sealed class View : MonoBehaviour, IStartable
                {
                    void IStartable.Start() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }

        [Fact]
        public void MonoBehaviour_NewHiddenStart_ShouldReportAG0003()
        {
            const string source = """
                using UnityEngine;
                using Amenone.ArchitectureGuard;

                public class ViewBase : MonoBehaviour
                {
                    [AllowUnityMessage("外部ライブラリの基底クラスを模した宣言なので")]
                    protected void Start() {}
                }

                public sealed class View : ViewBase
                {
                    private new void Start() {}
                }
                """;

            Assert.Equal(new[] { "AG0003" }, AnalyzerRunner.AnalyzeIds(source));
        }

        [Theory]
        [InlineData("Awake", "非アクティブな GameObject では、アクティブになるまで Awake() が呼ばれません")]
        [InlineData("Start", "GameObject が非アクティブか、コンポーネントが無効の間は Start() が呼ばれません")]
        [InlineData("OnDestroy", "1 度もアクティブにならなかった GameObject では OnDestroy() が呼ばれません")]
        [InlineData("Update", "Update() はどれが先に呼ばれるかがコードから読めません")]
        [InlineData("FixedUpdate", "FixedUpdate() はどれが先に呼ばれるかがコードから読めません")]
        public void MonoBehaviour_MessageDeclaration_ShouldExplainWhyToAvoid(string messageName, string reason)
        {
            var source = $$"""
                using UnityEngine;

                public sealed class View : MonoBehaviour
                {
                    private void {{messageName}}() {}
                }
                """;

            var diagnostic = Assert.Single(AnalyzerRunner.Analyze(source));

            Assert.Contains($"View が Unity のメッセージ {messageName}() を宣言しています。{reason}。", diagnostic.GetMessage());
        }

        [Fact]
        public void MonoBehaviour_StaticAndGenericMessages_ShouldNotReport()
        {
            const string source = """
                using UnityEngine;

                public sealed class View : MonoBehaviour
                {
                    private static void Awake() {}

                    private void Start<T>() {}
                }
                """;

            Assert.Empty(AnalyzerRunner.AnalyzeIds(source));
        }
    }
}
