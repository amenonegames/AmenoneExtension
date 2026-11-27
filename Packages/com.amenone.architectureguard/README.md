# Amenone ArchitectureGuard

Unity プロジェクトで文章として決めている設計の決まりを、Roslyn Analyzer の診断にしてコンパイル時に知らせるパッケージです。

診断が出るのは、`amenone.architectureguard` の asmdef を参照したアセンブリと、そのアセンブリを直接か間接に参照するアセンブリです。Unity は、asmdef のフォルダの下にある `RoslynAnalyzer` ラベル付きの DLL を、その asmdef を参照するアセンブリに適用します。この参照は間接でも数えるので、参照を足していないアセンブリでも、参照を足したアセンブリを参照していれば診断が出ます（Unity 6000.7.0b1 のバッチモードのコンパイルで確認。後述の「確かめた事」）。決まりを守らせたいアセンブリの asmdef に参照を足し、当てたくないアセンブリは、後述の「重大度の変え方」のアセンブリ単位のルールセットで規則を `Action="None"` にしてください（AG0005 はルールセットでも外せません）。

規則が対象にするライブラリ（UnityEngine、Unity.Entities、MessagePipe、com.amenone.ecsbridge）が入っていないプロジェクトやアセンブリでは、その規則は何もしません。型は名前の一致ではなくシンボルで判定するので、自前の `SystemBase` や `ISubscriber<T>`、`PublishSystem<T>` のような同じ名前の型には反応しません。

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

   `amenone.architectureguard` は `autoReferenced: false` です。そのため Assembly-CSharp など、参照を足していないアセンブリには、それだけでは適用されません。ただし、参照を足したアセンブリを参照するアセンブリには適用されます。テストや Editor のアセンブリが実装のアセンブリを参照する場合も、参照を足したアセンブリの asmdef が `autoReferenced: true`（既定）で Assembly-CSharp から参照される場合も同じです（どれも Unity 6000.7.0b1 のバッチモードのコンパイルで確認。他のバージョンでは未確認）。

## 診断の一覧

カテゴリはすべて `Amenone.ArchitectureGuard`、既定の重大度はすべて Warning です。

| ID | 対象 | 許可の方法 |
|---|---|---|
| AG0001 | `UnityEngine.Time` のうち timeScale の影響を受ける時間の読み取り（`deltaTime`、`time`、`timeAsDouble`、`smoothDeltaTime`、`fixedDeltaTime`、`fixedTime`、`fixedTimeAsDouble`、`timeSinceLevelLoad`、`timeSinceLevelLoadAsDouble`）と、`UnityEngine.Time` のプロパティへの代入（`timeScale`、`fixedDeltaTime`、`maximumDeltaTime`、`captureFramerate` などすべて。`=`、複合代入、`++` / `--`、分解代入を含み、複合代入は代入として 1 件だけ出す）。`timeScale` などを読むだけなら対象外 | `[AllowUnityTime("理由")]` |
| AG0002 | `UnityEngine.Time` の実時間の読み取り（`unscaledDeltaTime`、`unscaledTime`、`unscaledTimeAsDouble`、`fixedUnscaledDeltaTime`、`fixedUnscaledTime`、`fixedUnscaledTimeAsDouble`、`realtimeSinceStartup`、`realtimeSinceStartupAsDouble`） | `[AllowUnityTime("理由")]` |
| AG0003 | `UnityEngine.MonoBehaviour` を継ぐクラスが宣言する、引数なしの `Awake` / `Start` / `OnDestroy` / `Update` / `LateUpdate` / `FixedUpdate`（戻り値は問わないので `IEnumerator Start` や `async void Start` も対象。`new` で基底のものを隠した宣言も対象） | そのメソッドに `[AllowUnityMessage("理由")]` |
| AG0004 | 許可の属性の理由が不適切（書かれていない、空、空白だけ、空白を除いて 10 文字未満、文字列定数でない） | なし |
| AG0005 | このパッケージの診断（AG0001〜AG0008）の抑制。`#pragma warning disable AG0001` のようにこのパッケージの ID を書いたもの、ID を書かない `#pragma warning disable`、`[SuppressMessage]`（`[assembly: ...]` と `[module: ...]` を含む）の CheckId がこのパッケージの ID のもの | なし。重大度の変更も抑制もできない |
| AG0006 | Unity.Entities の `SystemBase` を継ぐクラスの `OnUpdate()` と、`ISystem` を実装する型の `OnUpdate(ref SystemState)` の中で、EntityQuery を作る呼び出し（`GetEntityQuery(...)`、`SystemState.GetEntityQuery(...)`、`EntityManager.CreateEntityQuery(...)`、`new EntityQueryBuilder(...)....Build(...)` など、Unity.Entities のメソッドで戻り値が `EntityQuery` のもの）。`SystemAPI.Query<…>()`、`SystemAPI.QueryBuilder()....Build()`、`SystemAPI.GetSingleton<T>()` は対象外 | なし（後述の重大度の変え方を使う） |
| AG0007 | Presenter のアセンブリ以外で、MessagePipe の購読の interface（`ISubscriber<T>`、`IAsyncSubscriber<T>`、`IBufferedSubscriber<T>`、`IBufferedAsyncSubscriber<T>`、キー付きの `ISubscriber<TKey, T>` / `IAsyncSubscriber<TKey, T>`、およびそれらを継ぐ型）を受け取る宣言（コンストラクタとメソッドの引数、フィールド、プロパティの型）。`IPublisher<T>` など発行の側と `IRequestHandler` は対象外 | なし（後述の重大度の変え方を使う） |
| AG0008 | Unity.Entities の `SystemBase` を継ぐクラスと `ISystem` を実装する型のうち、MessagePipe の発行の interface（`IPublisher<T>`、`IAsyncPublisher<T>`、`IBufferedPublisher<T>`、`IBufferedAsyncPublisher<T>`、キー付きの `IPublisher<TKey, T>` / `IAsyncPublisher<TKey, T>`、およびそれらを継ぐ型）を受け取る宣言（コンストラクタとメソッドの引数、フィールド、プロパティの型）を持つのに、`com.amenone.ecsbridge` の `Amenone.EcsBridge.PublishSystem<TPublisher>` を継いでいないもの。診断は型に 1 件（クラス名の位置）。`PublishSystem<TPublisher>` を継ぐ型（間に別の基底を挟んでもよい）と、System でないクラス（publisher の束のクラスなど）は対象外 | なし（後述の重大度の変え方を使う） |

## 規則ごとの理由

- **AG0001 / AG0002**: `Time.timeScale` はゲーム全体に効くので、止めたい所だけを止める（ゲームの進行を止めても UI のアニメーションは動かし続ける、など）ことができません。時間はプロジェクトで決めた時計から取り、止める・遅くするは時計の側で行います。実時間は時計を進める入口と処理時間の計測だけに使います。
- **AG0003**: `Awake` は GameObject がアクティブになるまで、`Start` は GameObject が非アクティブか、コンポーネントが無効の間は呼ばれません。1 度もアクティブにならなかった GameObject では `OnDestroy` も呼ばれません。`Update` 系は、どれが先に呼ばれるかがコードから読めません。初期化・破棄・毎フレームの処理は、生成と破棄を管理する側から明示的に呼びます。
- **AG0004**: 例外にした理由が残っていないと、後から読んだ人が例外を外してよいか判断できません。
- **AG0005**: 抑制すると、決まりの逸脱がコードから見えなくなります。例外は理由つきの許可の属性で書きます。
- **AG0006**: `OnUpdate` は毎フレーム呼ばれるので、query を作る（同じ query を探す）処理も毎フレーム走ります。query は `OnCreate` で作ってフィールドに持ちます。`SystemAPI.Query` と `SystemAPI.QueryBuilder` は、Entities のソース生成が query を `OnCreate` へ移すので対象外です。
- **AG0007**: 発行されたメッセージの受け手を Presenter に集めると、メッセージがどこへ届くかを Presenter だけ読めば追えます。他の層で反応が要る場合は、Presenter が購読して、その層の interface を呼びます。
- **AG0008**: ECS から MVP へ発行する System の書き方を `PublishSystem<TPublisher>` の 1 つに揃え、publisher の注入と前回値の扱いは基底に任せます。基底が何をするかは `com.amenone.ecsbridge` の README に書いてあります。複数の種類を発行する System は、publisher をフィールドに持つ束のクラスを作り、`PublishSystem<束のクラス>` と書きます。`ISystem` の struct は基底クラスを継げないので、発行は `PublishSystem<TPublisher>` を継ぐクラスの System で行います。

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

## エージェント向けの指示の文書

パッケージの直下の `AGENT_GUIDE.md` は、コードを書くエージェント（Claude Code や Codex など）に向けた設計の指示です。Analyzer の警告は違反を止めますが、新しい型をどこに置きどう組むかは教えません。この文書は、アセンブリの分け方、MVP の役割、メッセージ、DI、時間、MonoBehaviour と ECS の決まりと、各診断の直し方をまとめたものです。プロジェクトごとに決める事は 10 章の 2 つの表にあり、プロジェクトの側で埋めます。

git で入れたパッケージはフォルダの名前に版やハッシュが付くので、文書はプロジェクトへ写してから読み込ませます。写し先はプロジェクトの直下の `docs/architectureguard/AGENT_GUIDE.md`（`Assets` の外）です。

- **メニューで写す:** `Tools/Amenone/ArchitectureGuard/Copy Agent Guide To Project`。
- **コマンドラインで写す:** 同じ処理を `Amenone.ArchitectureGuard.Editor.AgentGuide.CopyToProject` で呼べます。

  ```sh
  Unity -batchmode -quit -projectPath <プロジェクト> -executeMethod Amenone.ArchitectureGuard.Editor.AgentGuide.CopyToProject
  ```

  パッケージを入れた・上げた直後は、Editor のアセンブリがコンパイルされて読み込まれるまで、メニューも `AgentGuide.CopyToProject` の型もありません。1 度コンパイルを通してから（コマンドラインなら、1 度 Unity でプロジェクトを開いてから）呼びます。

- **Editor を使わずに写す:** `Library/PackageCache/com.amenone.architectureguard@*/AGENT_GUIDE.md`（`Packages/` に埋め込んでいる場合は `Packages/com.amenone.architectureguard/AGENT_GUIDE.md`）を `docs/architectureguard/` へコピーするだけでも構いません。

メニューかメソッドで写した物には、写した版を書いた 1 行の HTML コメントが先頭に付きます。2 行目からはパッケージの文書とバイト単位で同じです（パッケージの文書に BOM が付いていれば、BOM だけは除きます）。写しは直接書き換えず、プロジェクト固有の事は `AGENTS.md` などに書きます。

Editor を開くと（スクリプトを読み込むと）、写しがあれば本文をパッケージの文書と比べ、違っていれば警告を 1 回出します。1 回は Editor を起動している間で 1 回です。先頭のコメントの行がある写しはその行を除いて比べ、無い写し（手でコピーした物）は全体を比べます。比べる前に、両方から先頭の BOM を除き、改行を LF に揃えます。git の改行の設定などで BOM や改行だけが変わった写しは、古いと見なしません。写しが無いプロジェクトでは何もしません。比べるだけで、ファイルを書き換えることはありません。パッケージを上げたら、同じメニューかメソッドで写し直します。写しの中身が同じなら、ファイルは書き換えず、書き換えなかった事をログに出します。

写した文書は、エージェントの指示のファイルから読ませます。`AGENTS.md` には、文書を読むよう文で書きます。例えば次のとおりです。その下に、文書の 10 章の 2 つの表を写して埋めます。

```markdown
設計の決まりは `docs/architectureguard/AGENT_GUIDE.md` を読む。プロジェクト固有の事はこの下に書く。
```

`@パス` で別のファイルを取り込む書き方を解くのは、Claude Code の `CLAUDE.md` だけです。`AGENTS.md` を読む他のエージェント（Codex など）は `@パス` を取り込みとして扱わないので、`AGENTS.md` には `@パス` だけを書かないでください。`CLAUDE.md` なら、次の 1 行で取り込めます。

```
@docs/architectureguard/AGENT_GUIDE.md
```

写す口は、Editor だけの別のアセンブリ `amenone.architectureguard.editor`（`autoReferenced: false`）にあります。Analyzer の DLL は `amenone.architectureguard` の asmdef のフォルダにあるので、Editor のアセンブリを足しても、Analyzer が適用される範囲は変わりません。

## 重大度の変え方

既定の重大度は、Analyzer のソースの `ArchitectureGuardDiagnostics.DefaultSeverity` の 1 か所で決めています。パッケージとして既定を変えるときは、そこを書き換えて DLL を作り直し、`AnalyzerReleases.Unshipped.md` に変更を書きます（後述の手順）。

利用する側のプロジェクトで AG0001〜AG0004、AG0006〜AG0008 の重大度を変えるには、ルールセットファイルを使います。`Assets/Default.ruleset` に書くとプロジェクト全体に、asmdef と同じフォルダに `<アセンブリ名>.ruleset` を置くとそのアセンブリだけに効きます（どちらも Unity 6000.7.0b1 のバッチモードのコンパイルで確認。他のバージョンでは未確認）。例えば警告をエラーにするには次のように書きます。

```xml
<?xml version="1.0" encoding="utf-8"?>
<RuleSet Name="ArchitectureGuard" ToolsVersion="16.0">
  <Rules AnalyzerId="Amenone.ArchitectureGuard.Analyzers" RuleNamespace="Amenone.ArchitectureGuard.Analyzers">
    <Rule Id="AG0001" Action="Error" />
    <Rule Id="AG0002" Action="Error" />
  </Rules>
</RuleSet>
```

AG0006〜AG0008 には許可の属性がありません。AG0007 は層（アセンブリ）の単位で縛る規則なので、メンバーごとの例外にはあまり意味がありません。AG0006 は、OnUpdate で query を作ってよい場面が思い当たらなかったので、属性を足して公開する API を増やすより、同じ扱いにしました。AG0008 も、発行する System を `PublishSystem<TPublisher>` に揃えない理由が思い当たらなかったので、同じ扱いにしました。例外にしたいアセンブリがある場合は、そのアセンブリの `<アセンブリ名>.ruleset` で `Action="None"` にし、理由をルールセットのコメントに書きます（`Action="None"` で AG0007 が消えることは、Unity 6000.7.0b1 のバッチモードのコンパイルで確認）。`#pragma warning disable AG0006` や `#pragma warning disable AG0008` などは AG0005 になります。

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
- AG0006 は、`Unity.Entities.EntityQuery` と、`Unity.Entities.SystemBase` か `Unity.Entities.ISystem` が見つからないアセンブリでは何もしません。System かどうかは Unity.Entities の型を継ぐ・実装するかで、`OnUpdate` かどうかは基底の `OnUpdate` を override しているか、`ISystem.OnUpdate` の実装かで判定します。同じ名前の自前の型やメソッドは対象外です。
- AG0006 が見るのは、`OnUpdate` の本体に書かれた呼び出し（その中のラムダとローカル関数を含む）だけです。`OnUpdate` から呼ぶ別のメソッドの中までは追いません。同じメソッドを `OnCreate` からも呼ぶ場合に誤って出さないためです。`OnUpdate` の中で 1 度だけ作ってフィールドに持つ書き方（遅延の初期化）も対象になります。
- AG0007 は、MessagePipe の購読の interface が 1 つも見つからないアセンブリでは何もしません。
- AG0007 では、アセンブリ名を `.`、`-`、`_` で区切り、区切った 1 つが `presenter` か `presenters`（大文字小文字を区別しない）なら Presenter のアセンブリとして扱います。区切った 1 つが `test` か `tests` のアセンブリと、`UnityEngine.TestRunner` を参照するアセンブリは、テストのアセンブリとして対象外にします。区切りの一部に含むだけのもの（`Game.NonPresenter`、`Game.Contests` など）は当たりません。設定で変える方法はありません。`nunit.framework` の参照はテストの目印にしません。Unity のエディタでのコンパイルでは、テストでないアセンブリにも `nunit.framework` の参照が付くためです。
- AG0007 が見るのは、フィールド・プロパティの型と、メソッドとコンストラクタ（record の主コンストラクタを含む）の引数の型そのものです。次は対象外です: 別の型に包んだ型（`ISubscriber<T>[]`、`Func<ISubscriber<T>>`、`List<ISubscriber<T>>` などの型引数）、インデクサの引数、delegate の引数、event の型、ローカル関数とラムダの引数、ローカル変数、`IObjectResolver.Resolve<ISubscriber<T>>()` のような取り出し、`IDistributedSubscriber`。
- AG0007 は宣言ごとに出します。partial メソッドの宣言と実装、interface の宣言とその明示的な実装、abstract と override では、それぞれに 1 件ずつ出ます。record の主コンストラクタは、引数とそこから作られるプロパティで 1 件です。
- AG0008 は、`Unity.Entities.SystemBase` と `Unity.Entities.ISystem` のどちらも見つからないアセンブリ、MessagePipe の発行の interface が 1 つも見つからないアセンブリ、`Amenone.EcsBridge.PublishSystem<TPublisher>` が見つからないアセンブリ（`com.amenone.ecsbridge` を入れていない、または参照していないもの）では何もしません。テストのアセンブリの判定は AG0007 と同じで、テストのアセンブリは対象外です。
- AG0008 が見る宣言の範囲は AG0007 と同じで、その型が自分で宣言したメンバーだけです。基底クラスが宣言した publisher は基底クラスの側で 1 件になり、派生クラスには出ません。`PublishSystem<TPublisher>` を継いでいる型は、publisher を別に持っていても出しません。System が購読の interface だけを受け取っている場合は AG0008 ではなく AG0007 の対象です。
- AG0008 は partial で宣言が分かれた型にも 1 件だけ出します。出す位置は、生成コードのファイル（`// <auto-generated/>` で始まるもの、名前が `.g.cs` や `.generated.cs` などで終わるもの、`.editorconfig` の `generated_code = true` のもの）にない最初の宣言のクラス名です。生成コードの位置に出した診断は Roslyn が捨てるので、Entities のソース生成などが足した partial の宣言は選びません。

## 確かめた事

- AG0001〜AG0005: Unity 6000.7.0b1 のバッチモードのコンパイルで、読み込みと警告、許可の属性、ルールセットを確かめました。
- AG0006 と AG0007: Unity 6000.7.0b1 のバッチモードのコンパイルで、Unity.Entities と MessagePipe の代用の定義（同じ名前空間・同じアセンブリ名で、必要な型だけを書いたもの）に対して警告が出ること、Presenter のアセンブリと、このパッケージを参照しないアセンブリでは出ないことを確かめました。本物の Entities と MessagePipe を入れた Unity のプロジェクトでは確かめていません。本物を使うプロジェクトのソースには、リポジトリの外のハーネス（Roslyn でコンパイルを作って Analyzer を当てるもの）で当てて、件数を測っただけです。
- AG0008: Unity 6000.7.0b1 のバッチモードのコンパイルで、Unity.Entities、MessagePipe、com.amenone.ecsbridge の代用の定義に対して警告が出ることを確かめました。本物の Entities、MessagePipe、com.amenone.ecsbridge を入れたプロジェクトのソースには、上と同じハーネスで当てて、`SystemBase` を直接継いで publisher を受け取る System の数と件数が合うことを確かめました。あわせて、このパッケージを入れた別のプロジェクトで、本物の Entities、MessagePipe、com.amenone.ecsbridge を入れた Unity のコンパイルで AG0008 が期待どおりの数だけ出ることを確かめました。
- 適用の範囲: Unity 6000.7.0b1 のバッチモードのコンパイルで、`amenone.architectureguard` を参照するアセンブリ A、A だけを参照するアセンブリ B、B だけを参照するアセンブリ C を作り、B と C に AG0001 が出ること、どれも参照しないアセンブリには出ないことを確かめました。A を参照する Editor だけのアセンブリと、テストのアセンブリ（`UnityEngine.TestRunner` を参照するもの）にも AG0001 が出ました。C の asmdef を `autoReferenced: true` にすると、Assembly-CSharp にも AG0001〜AG0003 が出ました。A を参照し、`<アセンブリ名>.ruleset` で AG0001 と AG0005 を `Action="None"` にしたアセンブリでは、AG0001 は出ず、AG0005 は出ました。
- 文書を写す口: パッケージを `Packages/` に埋め込んだプロジェクトで確かめました。Unity 6000.7.0b1 のバッチモードの `-executeMethod` で、次のことを確かめています。
  - 写しが作られ、2 行目からがパッケージの文書と同じになる。もう一度写してもファイルは書き換わらない。
  - パッケージの文書を変えると警告が 1 回出る。手で写した（先頭の行が無い）写しは、同じなら出ず、違えば出る。写しが無ければ出ず、`docs/` も作らない。
  - Editor のアセンブリには Analyzer が適用されない。
  - Unity 2022.3.62f3 でも、Editor のアセンブリがコンパイルでき、写せる。
  - git で入れた（`Library/PackageCache` にある）パッケージから写せることを、このパッケージを入れた別のプロジェクトで確かめました。

## Analyzer のビルドと DLL の更新

Analyzer のソースは `Source~/` にあります。Unity は名前の末尾が `~` のフォルダを読み込まないので、ここは Unity のコンパイル対象になりません。

- `Source~/Amenone.ArchitectureGuard.sln`
- `Source~/Amenone.ArchitectureGuard.Analyzers/`: Analyzer 本体（netstandard2.0、Microsoft.CodeAnalysis.CSharp 4.1.0）
- `Source~/Amenone.ArchitectureGuard.Analyzers.Tests/`: xUnit のテスト（net8.0）。Unity の DLL は使わず、`UnityEngine` の代用定義と `Runtime/` の属性のソースでコンパイルを作って診断を確かめます。`AGENT_GUIDE.md` が 9 章で全部の診断の ID を 1 回ずつ挙げていること、実在しない ID を書いていないこと、180 行以内であることも確かめます

手順は次のとおりです。.NET 8 以降の SDK が必要です。

```sh
cd Packages/com.amenone.architectureguard/Source~
dotnet test
dotnet build -c Release
```

Release でビルドすると、`Runtime/Analyzers/Amenone.ArchitectureGuard.Analyzers.dll` が上書きされます。DLL の `.meta`（`RoslynAnalyzer` ラベル、全プラットフォーム無効）はそのまま使えるので、作り直さないでください。

診断を足すときは、`ArchitectureGuardDiagnostics` に記述子を足し、同じクラスの ID の集合（AG0005 が対象にする ID）にもその記述子を並べます。診断を足したときや、ID・カテゴリ・既定の重大度を変えたときは、`AnalyzerReleases.Unshipped.md` に書きます。リリースするときは、その内容を `AnalyzerReleases.Shipped.md` の新しい版の節へ移します。どちらも更新しないと、ビルドで RS2000 系の警告が出ます。

## ライセンス

MIT です。[LICENSE.md](LICENSE.md) を見てください。
