# Amenone ArchitectureGuard

Unity プロジェクトで文章として決めている設計の決まりを、Roslyn Analyzer の診断にしてコンパイル時に知らせるパッケージです。

診断を出す範囲は、`amenone.architectureguard` の asmdef を参照したアセンブリだけにする設計です。Unity の文書では、asmdef のフォルダの下にある `RoslynAnalyzer` ラベル付きの DLL は、その asmdef を参照するアセンブリに適用されるとされています。このパッケージでは、この挙動を Unity の実機ではまだ確かめていません。決まりを守らせたいアセンブリの asmdef だけに参照を足してください。

## 入れ方

1. Package Manager の「Add package from git URL...」に次の URL を入れます。

   ```
   https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.architectureguard
   ```

   `Packages/manifest.json` に直接書く場合は次のとおりです。

   ```json
   "com.amenone.architectureguard": "https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.architectureguard"
   ```

2. 決まりを守らせたいアセンブリの asmdef の `references` に `amenone.architectureguard` を足します。

   ```json
   {
       "name": "MyGame.Gameplay",
       "references": [
           "amenone.architectureguard"
       ]
   }
   ```

   `amenone.architectureguard` は `autoReferenced: false` です。そのため Assembly-CSharp など、参照を足していないアセンブリには適用されません（Unity 6000.7.0b1 のバッチモードのコンパイルで確認。他のバージョンでは未確認）。

## 診断の一覧

カテゴリはすべて `Amenone.ArchitectureGuard`、既定の重大度はすべて Warning です。

| ID | 対象 | 許可の方法 |
|---|---|---|
| AG0001 | `UnityEngine.Time` のうち timeScale の影響を受ける時間の読み取り（`deltaTime`、`time`、`timeAsDouble`、`smoothDeltaTime`、`fixedDeltaTime`、`fixedTime`、`fixedTimeAsDouble`、`timeSinceLevelLoad`、`timeSinceLevelLoadAsDouble`）と、`UnityEngine.Time` のプロパティへの代入（`timeScale`、`fixedDeltaTime`、`maximumDeltaTime`、`captureFramerate` などすべて。`=`、複合代入、`++` / `--`、分解代入を含み、複合代入は代入として 1 件だけ出す）。`timeScale` などを読むだけなら対象外 | `[AllowUnityTime("理由")]` |
| AG0002 | `UnityEngine.Time` の実時間の読み取り（`unscaledDeltaTime`、`unscaledTime`、`unscaledTimeAsDouble`、`fixedUnscaledDeltaTime`、`fixedUnscaledTime`、`fixedUnscaledTimeAsDouble`、`realtimeSinceStartup`、`realtimeSinceStartupAsDouble`） | `[AllowUnityTime("理由")]` |
| AG0003 | `UnityEngine.MonoBehaviour` を継ぐクラスが宣言する、引数なしの `Awake` / `Start` / `OnDestroy` / `Update` / `LateUpdate` / `FixedUpdate`（戻り値は問わないので `IEnumerator Start` や `async void Start` も対象。`new` で基底のものを隠した宣言も対象） | そのメソッドに `[AllowUnityMessage("理由")]` |
| AG0004 | 許可の属性の理由が不適切（書かれていない、空、空白だけ、空白を除いて 10 文字未満、文字列定数でない） | なし |
| AG0005 | このパッケージの診断（AG0001〜AG0005）の抑制。`#pragma warning disable AG0001` のようにこのパッケージの ID を書いたもの、ID を書かない `#pragma warning disable`、`[SuppressMessage]`（`[assembly: ...]` と `[module: ...]` を含む）の CheckId がこのパッケージの ID のもの | なし。重大度の変更も抑制もできない |

ID は、実在する ID と大文字小文字まで完全に一致するものだけを対象にします。`#pragma warning disable ag0001` や `AG1234` は AG0005 になりません。なお、Roslyn は pragma の ID を大文字小文字を区別して照合するので、`ag0001` と書いても AG0001 は消えません。

## 許可の属性の書き方

属性は名前空間 `Amenone.ArchitectureGuard` にあります。理由は、なぜ決まりの例外にするのかが後から読んで分かるように、空白を除いて 10 文字以上の文字列定数で書きます。

### AllowUnityTime

メソッド、プロパティ、コンストラクタ、フィールド、イベント、ローカル関数、クラス、構造体に付けられます。付けた範囲の中の AG0001 と AG0002 を許可します。範囲の中にあるラムダ、ローカル関数、プロパティとイベントのアクセサ、フィールド初期化子、入れ子の型も含みます。

```csharp
using Amenone.ArchitectureGuard;
using UnityEngine;

[AllowUnityTime("Unity の時間をプロジェクトの時計へ渡す唯一の実装なので")]
public sealed class UnityTimeSource : ITimeSource
{
    public float DeltaTime => Time.deltaTime;
}

public sealed class FrameProfiler
{
    [AllowUnityTime("処理時間の計測のために実時間を読む")]
    public double Now() => Time.realtimeSinceStartupAsDouble;
}
```

### AllowUnityMessage

メソッドにだけ付けられます。

```csharp
using Amenone.ArchitectureGuard;
using UnityEngine;

public sealed class SceneEntryPoint : MonoBehaviour
{
    [AllowUnityMessage("シーンの起点で、生成と破棄を管理する側を組み立てるため")]
    private void Start() => Compose();
}
```

## 重大度の変え方

既定の重大度は、Analyzer のソースの `ArchitectureGuardDiagnostics.DefaultSeverity` の 1 か所で決めています。パッケージとして既定を変えるときは、そこを書き換えて DLL を作り直し、`AnalyzerReleases.Unshipped.md` に変更を書きます（後述の手順）。

利用する側のプロジェクトで AG0001〜AG0004 の重大度を変えるには、ルールセットファイルを使います。`Assets/Default.ruleset` に書くとプロジェクト全体に、asmdef と同じフォルダに `<アセンブリ名>.ruleset` を置くとそのアセンブリだけに効きます（どちらも Unity 6000.7.0b1 のバッチモードのコンパイルで確認。他のバージョンでは未確認）。例えば警告をエラーにするには次のように書きます。

```xml
<?xml version="1.0" encoding="utf-8"?>
<RuleSet Name="ArchitectureGuard" ToolsVersion="16.0">
  <Rules AnalyzerId="Amenone.ArchitectureGuard.Analyzers" RuleNamespace="Amenone.ArchitectureGuard.Analyzers">
    <Rule Id="AG0001" Action="Error" />
    <Rule Id="AG0002" Action="Error" />
  </Rules>
</RuleSet>
```

AG0005 には `NotConfigurable` を付けているので、ルールセットや `.editorconfig` では重大度を変えられず、`#pragma warning disable` でも消えません。ただし、次の節に書く例外が 1 つあります。

## 制限と補足

- **`[SuppressMessage]` で AG0005 自身を指定すると、AG0005 が消えます。** `NotConfigurable` を付けても塞げませんでした。メンバーに付けた `[SuppressMessage("…", "AG0005")]` はそのメンバーの範囲の AG0005 を、`[assembly: SuppressMessage("…", "AG0005")]` はアセンブリ全体の AG0005 を消します。後者を書くと、同じアセンブリにある他の `[SuppressMessage]` の AG0005 も出なくなります。この挙動はテストで固定しています。コードレビューで `AG0005` の文字列を探してください。
- トップレベルの文（Program.cs 形式）の中の `Time.*` は、許可の属性を付ける場所がないので許可できません。
- `[AllowUnityMessage]` はメソッドにしか付けられません。型にまとめて付けることはできません。
- AG0003 は、static メソッドとジェネリックメソッド（Unity は呼ばないもの）を対象にしません。`override` したメソッド（基底が宣言したもの）、明示的な interface 実装、引数のあるメソッド（`OnDestroy(ref SystemState)` など）、MonoBehaviour を継がないクラスの同名メソッド（VContainer の `IStartable.Start` など）、`OnEnable` / `OnDisable` も対象外です。
- `nameof(Time.deltaTime)` は時間を読まないので対象外です。
- `using static UnityEngine.Time;` や `using Clock = UnityEngine.Time;` を通した参照も、シンボルで判定するので対象になります。自前の `Time` クラスは対象外です。
- 自動生成されたコード（`// <auto-generated/>` の付いたファイル、`[GeneratedCode]` の付いた型など）は見ません。
- `#if` で無効になっている枝の `#pragma` は見ません。
- 許可の属性の理由が不適切（AG0004）でも、AG0001〜AG0003 の許可そのものは効きます。
- `UnityEngine.Time` や `UnityEngine.MonoBehaviour` が見つからないアセンブリ（`noEngineReferences: true` の asmdef など）では、AG0001〜AG0003 は何もしません。AG0004 と AG0005 はそうしたアセンブリでも働きます。

## Analyzer のビルドと DLL の更新

Analyzer のソースは `Source~/` にあります。Unity は名前の末尾が `~` のフォルダを読み込まないので、ここは Unity のコンパイル対象になりません。

- `Source~/Amenone.ArchitectureGuard.sln`
- `Source~/Amenone.ArchitectureGuard.Analyzers/`: Analyzer 本体（netstandard2.0、Microsoft.CodeAnalysis.CSharp 4.1.0）
- `Source~/Amenone.ArchitectureGuard.Analyzers.Tests/`: xUnit のテスト（net8.0）。Unity の DLL は使わず、`UnityEngine` の代用定義と `Runtime/` の属性のソースでコンパイルを作って診断を確かめます

手順は次のとおりです。.NET 8 以降の SDK が必要です。

```sh
cd Packages/com.amenone.architectureguard/Source~
dotnet test
dotnet build -c Release
```

Release でビルドすると、`Runtime/Analyzers/Amenone.ArchitectureGuard.Analyzers.dll` が上書きされます。DLL の `.meta`（`RoslynAnalyzer` ラベル、全プラットフォーム無効）はそのまま使えるので、作り直さないでください。

診断を足すときは、`ArchitectureGuardDiagnostics` に記述子を足し、同じクラスの ID の集合（AG0005 が対象にする ID）にもその記述子を並べます。診断を足したときや、ID・カテゴリ・既定の重大度を変えたときは、`AnalyzerReleases.Unshipped.md` に書きます。リリースするときは、その内容を `AnalyzerReleases.Shipped.md` の新しい版の節へ移します。どちらも更新しないと、ビルドで RS2000 系の警告が出ます。
