# 設計の指示（ArchitectureGuard）

`com.amenone.architectureguard` の警告は違反を止めるが、新しい型をどこに置きどう組むかは教えない。この文書はそれを補う。メニューの `Tools/Amenone/ArchitectureGuard/Copy Agent Guide To Project` でプロジェクトの `docs/architectureguard/` に写す。パッケージを入れた・上げた直後は、1 度コンパイルを通してから写す。写しは直接書き換えない。プロジェクト固有の名前は、10 章の項目としてプロジェクトの `AGENTS.md` に書く。

`AGENTS.md` には「設計の決まりは `docs/architectureguard/AGENT_GUIDE.md` を読む」と文で書く。`@パス` の取り込みを解くのは Claude Code の `CLAUDE.md` だけで、`AGENTS.md` を読む他のエージェント（Codex など）は解かない。

決まりの末尾の `（AG0007）` は Analyzer が検査する決まり、`（検査なし）` は検査の無い決まりを表す。検査の無い決まりは、この文書を読んで守るしかない。名前の例（`MyGame.Shop` など）は作り物。

## 1. この文書の使い方

- 型（クラス、interface、メッセージ、System）を足す前に 3 章を見て、置くアセンブリを決める。
- AG の警告が出たら 9 章を見て直す。10 章の「当面残す警告」の表に挙げた物を除き、警告を残したまま作業を終えない。（検査なし）
- 決まりから外れる時は、9 章の許可の方法で理由を残し、足した許可を作業の報告（PR の本文など）に一覧で書く。（検査なし）

## 2. 全体の形

VContainer（DI）と MessagePipe（通知）で組む MVP のレイヤード構成に、必要な所だけ Unity Entities（ECS）を足す。

- 抽象（interface、MessagePipe のメッセージ、外へ見せる値）と実装を別のアセンブリに分ける。（検査なし）
- 実装のアセンブリは他の実装のアセンブリを参照しない。参照してよいのは、抽象と、値と規則だけの層。ただし合成の根（いちばん親のスコープを置くアセンブリ）だけは、親のスコープに登録する実装を参照してよい。Editor の拡張のアセンブリも、拡張の対象の実装を参照してよい。実行時のアセンブリは Editor のアセンブリを参照しない。（検査なし）
- 値と規則だけの層は、参照を持たない純粋な C#（`noEngineReferences`）にする。どこからでも参照してよい。（検査なし）
- 実装どうしをつなぐのは、各層の登録（VContainer の `LifetimeScope` / ContainerBuilder）だけ。実装のコードは相手の実装の型を知らない。（検査なし）
- 機能のまとまりごとにこの形を繰り返す（`MyGame.Shop.Abstract`、`MyGame.Shop.Logic`、`MyGame.Shop.Presenter` …）。まとまりどうしも抽象越しにだけやり取りする。（検査なし）

## 3. どこに何を置くか

表の「参照してよい物」は層どうしの参照だけを書く。このほかに、managed の実装のアセンブリ（Logic、View、Presenter、部品、合成の根、境目）は、メッセージの発行と購読のために MessagePipe を、自分の登録を書くために VContainer を参照してよい。

| 役割 | 置くアセンブリ（例） | 参照してよい物 |
|---|---|---|
| 値と規則（ID、量、計算の規則） | 値の層（`MyGame.Domain`） | なし |
| interface、メッセージ、外へ見せる値 | 抽象（`MyGame.Shop.Abstract`） | 値の層、他の抽象 |
| 判断と状態 | Logic（`MyGame.Shop.Logic`） | 値の層、抽象 |
| View の interface | View の抽象（`MyGame.Shop.ViewAbstract`） | 値の層 |
| 表示と入力の受け取り（MonoBehaviour） | View（`MyGame.Shop.View`） | 値の層、View の抽象、メッセージの型を持つ抽象、部品のアセンブリ |
| View と Logic をつなぐ。メッセージの購読 | Presenter（`MyGame.Shop.Presenter`） | 値の層、View の抽象、抽象 |
| 複数の View から使う表示の部品 | 部品のアセンブリ（部品だけを置き、抽象は分けない） | 値の層、UI のライブラリ |
| いちばん親のスコープ | 合成の根（`MyGame.Root`） | 抽象、親のスコープに登録する実装（時計など） |
| managed と ECS が同じ値で使う識別子 | 識別子の層（`MyGame.Battle.Identifier`、参照なし） | なし |
| `IComponentData`、Authoring、Baker | `MyGame.Battle.Ecs.Components` | Entities ほか DOTS |
| `ISystem`、`IJobEntity` | `MyGame.Battle.Ecs.Systems` | Components、DOTS |
| managed から ECS への書き込み | 境目（`MyGame.Battle.Bridge`） | Components、抽象、Entities、`amenone.ecsbridge` |
| ECS から managed へ発行する System（`PublishSystem<TPublisher>` の派生）と publisher の束のクラス | 境目（`MyGame.Battle.Bridge`） | Components、メッセージの型を持つ抽象、Entities、`amenone.ecsbridge` |
| Inspector やメニューなどの Editor の拡張（実行時のアセンブリからは参照されない） | Editor のアセンブリ（`MyGame.Shop.Editor`、Editor だけ） | 拡張の対象の実装、値の層、抽象 |

他の層の登録が `SerializeField` で受け取る ScriptableObject は、受け取る側が参照できるよう抽象に置く。

新しい機能を足す時の手順:

1. 足す型の役割を上の表で引き、置くアセンブリを決める。
2. 相手の層に要る interface やメッセージが無ければ、抽象の側に足す。asmdef の参照を足して層を飛び越えない。（検査なし）
3. 既存のアセンブリに収まらない時や、参照の向きが崩れる時は、細かいアセンブリを新しく切る。新しいアセンブリにも抽象と実装を分ける決まりを当てはめる。（検査なし）
4. Presenter のアセンブリは、名前を `.` `-` `_` で区切った 1 つを `Presenter` にする。テストのアセンブリは 1 つを `Tests` にする。どちらの判定も大文字小文字を区別せず、区切った 1 つが `presenter` / `presenters`、`test` / `tests` のどれでも通る。AG0007 は Presenter を名前で、テストを名前か `UnityEngine.TestRunner` の参照で見分ける。AG0007 と AG0008 はテストのアセンブリを見ない。（AG0007、AG0008）
5. 新しい asmdef には `amenone.architectureguard` の参照を足す。テスト、Editor、値だけの層は除く。検査したいアセンブリは、間接の参照に頼らず参照を足す。（検査なし）
6. 参照を足さないアセンブリ（テストや Editor を含む）も、参照を足したアセンブリを参照していれば規則が当たる。当てたくない時は 9 章の末尾に従う。（検査なし）
7. 実装を、そのアセンブリの登録の 1 か所に登録する（6 章）。層をまたぐ通知が要る時は 5 章に従う。

## 4. MVP の役割分担

この章は検査なし。

- View は表示と入力の受け取りだけを持つ。判断も状態も持たない。
- View は Logic を直接呼ばない。入力はメッセージで発行する。
- Presenter は View の抽象と Logic の抽象を interface 越しにつなぐだけで、判断を持たない。受けた物を相手の interface へそのまま渡す。
- 判断が View や Presenter に要りそうに見えたら、Logic に置き、抽象に interface を足す。

## 5. メッセージ

- 層をまたぐ通知は MessagePipe で行う。メッセージの型は抽象の側に置く。（検査なし）
- 購読の interface（`ISubscriber<T>`、`IAsyncSubscriber<T>`、`IBufferedSubscriber<T>` など）を受け取るのは Presenter のアセンブリだけ。発行（`IPublisher<T>` など）は AG0007 の対象外。ECS の System から発行する時は 8 章に従う。受け手を Presenter に集めると、メッセージの届き先を Presenter だけ読めば追える。（AG0007）
- 他の層で反応が要る時は、Presenter が購読して、その層の抽象の interface を呼ぶ。interface の実装は、その層が自分の登録で登録する。（AG0007。interface を挟む形は検査なし）
- `Func<ISubscriber<T>>` やリストに包む、`IObjectResolver.Resolve` で取り出すなど、AG0007 が見ない書き方で購読を Presenter の外へ持ち出さない。（検査なし）

```csharp
// MyGame.Shop.Presenter。ItemPurchased と IInventory は MyGame.Shop.Abstract、IInventory の実装は Logic にある
public sealed class ItemPurchasedRelay : IInitializable, IDisposable
{
    private readonly ISubscriber<ItemPurchased> _purchased;
    private readonly IInventory _inventory;
    private IDisposable _subscription;

    public ItemPurchasedRelay(ISubscriber<ItemPurchased> purchased, IInventory inventory)
    {
        _purchased = purchased;
        _inventory = inventory;
    }

    public void Initialize() => _subscription = _purchased.Subscribe(message => _inventory.Add(message.ItemId));
    public void Dispose() => _subscription?.Dispose();
}
```

- `RegisterMessagePipe` は、親子でつながるスコープの中で 1 か所（いちばん親に近い所）だけで呼ぶ。子でも呼ぶとスコープごとに別のブローカーになり、親の側で発行したメッセージが子に届かない。（検査なし）
- options の `InstanceLifetime` は `Singleton`（既定）のままにする。`Scoped` にすると、やはりスコープごとに別のブローカーになる。（検査なし）
- 同期の `ISubscriber<T>` と非同期の `IAsyncSubscriber<T>` は別のブローカー。`IAsyncPublisher<T>` で出したメッセージは `ISubscriber<T>` に届かないので、発行と同じ側で購読する。（検査なし）

## 6. DI とスコープ

この章は検査なし。

- 実装のアセンブリは、自分の登録をまとめる `LifetimeScope`（ContainerBuilder）を 1 つ持つ。そのアセンブリの登録はそこにだけ書く。
- スコープの親子の順を決めて、プロジェクトの `AGENTS.md` に書く。引かれる側ほど親に置く。子のスコープは親の登録を引けるが、親は子の登録を見られない。
- 組み立ての後に要る処理（登録口への登録など）は、`RegisterBuildCallback` かエントリーポイント（`IInitializable` など）で行う。

## 7. 時間

時計の型はこのパッケージに無い。プロジェクトが用意し、型と取り方をプロジェクトの `AGENTS.md` に書く。

- ゲーム内の時間は `UnityEngine.Time` から取らず、プロジェクトの時計から取る。（AG0001）
- `Time.timeScale` などの `Time` のプロパティに代入しない。止める・遅くするは時計の側で行う。`Time.timeScale` はゲーム全体に効くので、一部だけを止められない。（AG0001）
- 止めたい単位ごとに時計を分ける（例: ゲーム内の時計と UI の時計）。止める側は、自分が止めた分を自分で戻す。（検査なし）
- 実時間（`Time.unscaledDeltaTime` など）を読むのは、時計を進める入口と処理時間の計測だけ。そこに `[AllowUnityTime("理由")]` を付ける。（AG0002）
- 時計を進める入口は 1 つにする。（検査なし）
- 演出（トゥイーン）や待ちには、時計か、時計が出すスケジューラーや時間の元を渡す。外のライブラリが既定で `Time` を読む所は検査されない。（検査なし）
- ECS の System の中では `SystemAPI.Time.DeltaTime` を使う。World の時間は、ecsbridge の `WorldClockDriver` に、プロジェクトの時計を返す `IWorldDeltaTimeSource` を渡して進める。（System の中でも `UnityEngine.Time` の読み取りは AG0001 / AG0002。駆動の形は検査なし）

## 8. MonoBehaviour と ECS

### MonoBehaviour

- 引数なしの `Awake` / `Start` / `OnDestroy` / `Update` / `LateUpdate` / `FixedUpdate` を宣言しない。`Awake` / `Start` は非アクティブな GameObject では呼ばれず、1 度もアクティブにならなければ `OnDestroy` も呼ばれない。`Update` 系は呼ばれる順がコードから読めない。（AG0003）
- 代わりに、抽象の側に interface を置き、生成と破棄を管理する側（Presenter や、VContainer の `IStartable` / `ITickable` / `IDisposable` を実装したクラス）からまとめて呼ぶ。（検査なし）
- `OnEnable` / `OnDisable` は使ってよい。ただし初期化はそこに頼らず、要る時に明示的に呼ぶ。（検査なし）

### ECS

- 計算をする System は DI も interface 越しの呼び出しも持たない。データは `IComponentData`、処理は System に置き、Burst と Job に乗る形にする。managed への発行は `PublishSystem<TPublisher>` を継ぐ専用の System に分け、publisher を受け取るのはそこだけにする。（検査なし）
- managed と ECS の境目は 1 つのアセンブリに狭く保つ。境目の外側は 2〜6 章の構成に従う。（検査なし）
- managed → ECS は、境目のクラスがシングルトンの component に書く（ecsbridge の `DefaultWorldSingleton<T>`）。各 Entity に配るのは ECS の System。（検査なし）
- ECS → managed は、`PublishSystem<TPublisher>` を継ぐ System が値の変わった時だけ発行し、Presenter が受ける。複数の種類を出す時は、publisher をフィールドに持つ束のクラスを型引数にする。`ISystem` の struct は基底を継げないので、発行はクラスの System で行う。発行の System の書き方を 1 つに揃え、注入と前回値の扱いは基底に任せる。（AG0008。変わった時だけ出すのは検査なし）
- 発行する System は `RegisterSystemFromDefaultWorld` で、publisher（と束）を解決できるスコープに登録する。（検査なし）
- query は `OnCreate` で作ってフィールドに持つ。`OnUpdate` で作ると、作る（同じ query を探す）処理が毎フレーム走る。`SystemAPI.Query` / `SystemAPI.QueryBuilder` はソース生成が `OnCreate` へ移すので対象外。（AG0006。`OnUpdate` から呼ぶ別のメソッドの中は検査なし）
- 既定の World をフィールドに持ち続けない。Play に入り直すと World が替わるので、ecsbridge の `DefaultWorldSingleton<T>` / `DefaultWorldQuery` を通して読み書きする。（検査なし）
- World を作り直したら、System を登録したスコープと `WorldClockDriver` を持つスコープも作り直す。古いスコープは古い System と World を指したままで、新しい System には publisher が入らない。（検査なし）

## 9. 診断の一覧と直し方

- **AG0001**: timeScale の影響を受ける `Time` の読み取りと、`Time` のプロパティへの代入。時間はプロジェクトの時計から取り、止めるのも時計で行う。時計を進める入口だけは `[AllowUnityTime("理由")]`。
- **AG0002**: 実時間の `Time` の読み取り。時計の入口と計測以外では時計に替える。入口と計測には `[AllowUnityTime("理由")]`。
- **AG0003**: MonoBehaviour の引数なしの `Awake` / `Start` / `OnDestroy` / `Update` 系。抽象の interface にし、生成と破棄を管理する側から呼ぶ。シーンの起点などどうしても要る所だけ `[AllowUnityMessage("理由")]`。
- **AG0004**: 許可の属性の理由が無い、空、空白を除いて 10 文字未満、文字列定数でない。理由が無いと、後から例外を外してよいか判断できない。なぜ例外なのかが分かる文に書き直す。
- **AG0005**: このパッケージの ID を書いた `#pragma warning disable`、ID を書かない `#pragma warning disable`、CheckId がこのパッケージの ID の `[SuppressMessage]` による抑制。抑制すると逸脱がコードから見えなくなる。抑制をやめて直す。例外にするなら、許可の属性のある規則（AG0001〜AG0003）は属性と理由を書き、属性の無い規則（AG0006〜AG0008）はアセンブリ単位の `<アセンブリ名>.ruleset` で `Action="None"` にして理由をコメントに書く。
- **AG0006**: System の `OnUpdate` の中での `EntityQuery` の作成。`OnCreate` で作ってフィールドに持つ。
- **AG0007**: Presenter 以外のアセンブリでの購読の interface の受け取り。購読を Presenter に移し、反応する層の抽象の interface を呼ぶ（5 章）。
- **AG0008**: MessagePipe の publisher を受け取る ECS の System が `Amenone.EcsBridge.PublishSystem<TPublisher>` を継いでいない。`PublishSystem<TPublisher>` を継ぎ、複数の種類は publisher の束のクラスを型引数にする（8 章）。

規則が対象にするライブラリ（UnityEngine、Unity.Entities、MessagePipe、`com.amenone.ecsbridge`）が見つからないアセンブリでは、その規則は何もしない。AG0004 と AG0005 は UnityEngine の無いアセンブリ（`noEngineReferences`）でも働く。`com.amenone.ecsbridge` が入っていないか、そのアセンブリがコンパイルの条件（Entities と VContainer が要る）で外れているプロジェクトでは、AG0008 は動かない。

規則が当たるのは、`amenone.architectureguard` を参照するアセンブリと、それを直接か間接に参照するアセンブリ。参照を足したアセンブリを参照するテストや Editor、`autoReferenced` の asmdef を通して参照する Assembly-CSharp にも当たる。アセンブリ丸ごと当てない時は、`<アセンブリ名>.ruleset` で AG0001〜AG0004 と AG0006〜AG0008 を `Action="None"` にし、理由をコメントに書く。メンバーごとの例外は ruleset でなく許可の属性で書く（属性があるのは AG0001〜AG0003）。AG0005 は ruleset でも外れない。当てたくないアセンブリでも、ID を書かない `#pragma warning disable` は AG0005 になるので、pragma には抑制する ID を書く。`CS0618` など、このパッケージ以外の ID を書いた pragma は AG0005 にならない。直接にも間接にも参照しないアセンブリは何も検査されない。

## 10. プロジェクトの側で決める事

次の項目はこの文書に無い。プロジェクトの `AGENTS.md` に、この章の 2 つの表を写して埋める。

| 項目 | このプロジェクトでの決め |
|---|---|
| 値と規則の層のアセンブリ名 | |
| 機能のまとまりの一覧と、各層のアセンブリ名の付け方 | |
| いちばん親のスコープと、その置き場所 | |
| シーンごとのスコープの親子の順 | |
| `RegisterMessagePipe` を呼ぶスコープ | |
| 時計の型、取り方、時計の分け方 | |
| 時計を進める入口（`[AllowUnityTime]` を付ける所） | |
| managed と ECS の境目のアセンブリ | |
| World を作り直す所と、作り直すスコープ | |
| `amenone.architectureguard` の参照を足さないアセンブリと理由（間接の参照で規則が当たるかも書く） | |
| ruleset で外した規則、そのアセンブリ、理由 | |

当面残す警告は、次の表に 1 行ずつ書く。

| 規則 | 件数 | 所 | 残す期限 |
|---|---|---|---|
| | | | |
