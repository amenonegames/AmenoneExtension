# Changelog

## [0.2.0] - 2026-11-18
### Changes
- AG0006: ECS の System の OnUpdate の中での EntityQuery の作成
- AG0007: Presenter 以外のアセンブリでの MessagePipe の購読の interface の受け取り
- AG0007 の Presenter のアセンブリは、アセンブリ名を `.` `-` `_` で区切った 1 つが `presenter` / `presenters` のもの。区切った 1 つが `test` / `tests` のものと、`UnityEngine.TestRunner` を参照するものはテストとして対象外
- AG0003: 診断文に、そのメッセージを避ける理由（非アクティブな間は Awake / Start が呼ばれない、など）を足した。ID・重大度・対象は変えていない
- AG0005: 対象の ID に AG0006 と AG0007 を足した。診断文の直し方の案内を、許可の属性がある規則（AG0001〜AG0003）は属性で、無い規則（AG0006、AG0007）はアセンブリ単位のルールセットで、と分けた

## [0.1.0] - 2026-11-16
### Changes
- Initial release
- AG0001: timeScale の影響を受ける UnityEngine.Time の読み取りと Time.timeScale への代入
- AG0002: UnityEngine.Time の実時間の読み取り
- AG0003: MonoBehaviour が宣言する Awake / Start / OnDestroy / Update / LateUpdate / FixedUpdate
- AG0004: 許可の属性の理由が不適切
- AG0005: ArchitectureGuard の診断の抑制
