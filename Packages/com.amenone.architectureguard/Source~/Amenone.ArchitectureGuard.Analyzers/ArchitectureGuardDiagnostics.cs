using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class ArchitectureGuardDiagnostics
    {
        public const string Category = "Amenone.ArchitectureGuard";
        public const DiagnosticSeverity DefaultSeverity = DiagnosticSeverity.Warning;

        public const string ScaledTimeReadGuidance = "時間は、プロジェクトで決めた時間の供給元（時計）から取ってください。";
        public const string TimeSettingWriteGuidance = "全体の時間の設定はコードから書き換えないでください。止める、遅くするといった時間の制御は、プロジェクトで決めた時計の側で行ってください。";

        public static readonly DiagnosticDescriptor ScaledTimeUsage = new DiagnosticDescriptor(
            id: "AG0001",
            title: "UnityEngine.Time の時間を直接読んでいる、または時間の設定を書き換えている",
            messageFormat: "{0}。{1}どうしても直接扱う必要がある場合は、そのメンバーか型に [AllowUnityTime(\"理由\")] を付けて理由を書いてください。",
            category: Category,
            defaultSeverity: DefaultSeverity,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnscaledTimeUsage = new DiagnosticDescriptor(
            id: "AG0002",
            title: "UnityEngine.Time の実時間を直接読んでいる",
            messageFormat: "{0} で実時間を直接読んでいます。実時間を使ってよいのは、時計を進める入口と処理時間の計測だけです。それ以外では、プロジェクトで決めた時間の供給元（時計）から時間を取ってください。時計の入口か計測に当たる場合は、そのメンバーか型に [AllowUnityTime(\"理由\")] を付けて、実時間が必要な理由を書いてください。",
            category: Category,
            defaultSeverity: DefaultSeverity,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnityMessageDeclaration = new DiagnosticDescriptor(
            id: "AG0003",
            title: "MonoBehaviour が Unity のメッセージを宣言している",
            messageFormat: "MonoBehaviour を継ぐ {0} が Unity のメッセージ {1}() を宣言しています。Unity が呼ぶ時機と順序に頼らないでください。初期化と破棄は明示的なメソッドにして、生成と破棄を管理する側から呼んでください。順序が意味を持つ毎フレームの処理は、呼ぶ側が順に呼んでください。Unity のメッセージでなければ成り立たない場合は、このメソッドに [AllowUnityMessage(\"理由\")] を付けて理由を書いてください。",
            category: Category,
            defaultSeverity: DefaultSeverity,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidAllowReason = new DiagnosticDescriptor(
            id: "AG0004",
            title: "許可の属性の理由が不適切",
            messageFormat: "[{0}] の理由が{1}。決まりの例外にする理由が後から読んで分かるように、空白を除いて {2} 文字以上の文字列定数で理由を書いてください。",
            category: Category,
            defaultSeverity: DefaultSeverity,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DiagnosticSuppression = new DiagnosticDescriptor(
            id: "AG0005",
            title: "ArchitectureGuard の診断を抑制している",
            messageFormat: "{0} は ArchitectureGuard の診断を抑制しています。この診断は抑制も重大度の変更もできません。抑制をやめて、診断が指す箇所を直してください。決まりの例外にするなら、[AllowUnityTime(\"理由\")] か [AllowUnityMessage(\"理由\")] を付けて理由を書いてください。",
            category: Category,
            defaultSeverity: DefaultSeverity,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        private static readonly ImmutableHashSet<string> Ids = new[]
            {
                ScaledTimeUsage,
                UnscaledTimeUsage,
                UnityMessageDeclaration,
                InvalidAllowReason,
                DiagnosticSuppression,
            }
            .Select(descriptor => descriptor.Id)
            .ToImmutableHashSet(StringComparer.Ordinal);

        public static bool IsArchitectureGuardId(string id) => Ids.Contains(id);
    }
}
