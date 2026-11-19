# Amenone ECS Bridge

VContainer + MessagePipe で組んだ MVP の構成と、Unity Entities（ECS）をつなぐ小さな部品の集まりです。

- MVP の側から既定の World のシングルトン・バッファ・query を読み書きする（Play のたびに作り直される World に追従する）
- ECS の側で値が変わった時だけ MVP の側へメッセージを発行する System の基底クラスと、前回値の記録
- 既定の World を作り直す、World の時間を外の時計で進める

## 要る物と確かめた版

| 要る物 | 確かめた版 |
|---|---|
| Unity | 6000.7.0b1 |
| Unity Entities（`com.unity.entities`） | 6.7.0 |
| VContainer（`jp.hadashikick.vcontainer`） | 1.19.0 |
| MessagePipe / MessagePipe.VContainer（サンプルだけ） | https://github.com/Cysharp/MessagePipe のコミット 3adc01deb1a283cb2faeb36c00178b84884a7fc3 |
| UniTask（サンプルだけ） | 2.5.11（https://github.com/Cysharp/UniTask のコミット f74aace058f4ee7d3aa5bcc89cc98dbfb2a02892） |

asmdef の下限（Entities 1.0.0、VContainer 1.13.0）と `package.json` の `"unity": "2022.3"` は、コンパイルの条件としての宣言で、動作を確かめた版ではありません。

このパッケージは `package.json` の `dependencies` に何も書いていません。Entities と VContainer は自分のプロジェクトに入れてください。
どちらかが入っていない間は、アセンブリ（`amenone.ecsbridge`）ごとコンパイルされません（asmdef の `versionDefines` と `defineConstraints`）。
Unity 2022.3（2022.3.62f3、Entities 無し）では、コンパイルが止まらない事だけを確かめました。

## 入れ方

Package Manager の「Add package from git URL」に次を入れます。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.ecsbridge
```

## 使い方

### DefaultWorldSingleton / DefaultWorldSingletonBuffer / DefaultWorldQuery

`World.DefaultGameObjectInjectionWorld` のシングルトンを、MVP の側のクラスから読み書きします。
使うたびに今の既定の World か確かめ、World が替わっていれば query を作り直します。ドメインもシーンも再読み込みしない設定で Play に入り直しても、前の World を見続けません。
既定の World が無い（null か破棄済み）時は `InvalidOperationException` を投げます。

```csharp
public sealed class MoveInputSender
{
    private readonly DefaultWorldSingleton<MoveInput> _input = new();

    public void Send(float2 direction) => _input.Write(new MoveInput { Direction = direction });
}
```

- `new DefaultWorldSingleton<T>(initialValue, createsWhenMissing: true)`: その World を初めて触った時に、シングルトンが無ければ `initialValue` で作ります。既にあれば上書きしません
  - `Remove()` や他所で消えた後は作り直さないので、`Read()` は投げます。`Write()` は作り直します
- `createsWhenMissing: false`: `Prepare()` や `Read()` では作りません（`Write()` は作ります）。`Exists()` で有無を確かめてください
- `DefaultWorldSingletonBuffer<T>`: `Buffer()` で今の World のシングルトンのバッファを返します（無ければ空で作る）。返る `DynamicBuffer` は構造の変更（Entity の作成・破棄など）で無効になるので、持ち続けずに毎回 `Buffer()` で取ってください
- `DefaultWorldQuery`: `Current()` で今の World の `EntityQuery` を返します

### PublishSystem&lt;TPublisher&gt; と LastPublishedValue / LastPublished

ECS の値を読んで、変わった時だけ MVP の側へ発行する System の基底クラスです。
`TPublisher` は `IPublisher<T>` でも `IBufferedPublisher<T>` でも構いません（この型自体は MessagePipe に依存しません）。
VContainer が `[Inject] Construct` で publisher を渡すまでは `PublishChanges` を呼びません。

```csharp
public partial class ScorePublishSystem : PublishSystem<IPublisher<ScoreChangedMessage>>
{
    private readonly LastPublishedValue<int> _lastScore = new();

    protected override void OnPublisherInjected() => _lastScore.Clear();

    protected override void PublishChanges()
    {
        var score = SystemAPI.GetSingleton<Score>().Value;
        if (_lastScore.TryUpdate(score)) Publisher.Publish(new ScoreChangedMessage(score));
    }
}
```

```csharp
builder.RegisterSystemFromDefaultWorld<ScorePublishSystem>();
```

- `OnPublisherInjected()`: World を作り直さずに LifetimeScope だけを作り直すと、World に残った同じ System へ publisher が注入し直されます。前回値をどう扱うかはここに書きます（下の「最初の値を発行するかどうか」）
- `LastPublishedValue<T>`: `TryUpdate(value)` は初回と値が変わった時に true。`Clear()` で初回に戻ります。`TreatAsPublished(value)` はその値を発行済みとして扱います（発行はしない）。`TryGetPublished(out value)` で前回値を読みます。比較は `EqualityComparer<T>.Default`
- `LastPublished<TKey, TValue>`: キーごとの前回値。`TryUpdate(key, value)`・`TreatAsPublished(key, value)`・`TryGetPublished(key, out value)`・`Clear()` は同じです。`TreatEveryKeyAsPublished(value)` は、覚えているキーを忘れ、まだ見ていないキーも含めて全部のキーがその値を発行済みとして扱います（`TryGetPublished` もまだ見ていないキーにこの値を返す）。`Clear()` はこの値も忘れます

前回値の部品は使わなくても構いません。基底の `PublishSystem<TPublisher>` は前回値の持ち方を縛らないので、部品に合わない判定は System が自分のフィールドで持ってください。

### 複数の種類のメッセージを出す System

publisher を名前付きのフィールドに持つ小さなクラス（publisher の束）を作り、`PublishSystem<束のクラス>` を継ぎます。
VContainer が束を組み立てて基底の `[Inject] Construct` に渡すので、基底の「注入までは何もしない」「`OnPublisherInjected()`」はそのまま使えます。

```csharp
public sealed class ScorePublishers
{
    public readonly IPublisher<ScoreChangedMessage> Score;
    public readonly IBufferedPublisher<ComboChangedMessage> Combo;

    public ScorePublishers(IPublisher<ScoreChangedMessage> score, IBufferedPublisher<ComboChangedMessage> combo)
    {
        Score = score;
        Combo = combo;
    }
}

public partial class ScorePublishSystem : PublishSystem<ScorePublishers>
{
    private readonly LastPublishedValue<int> _lastScore = new();
    private readonly LastPublishedValue<int> _lastCombo = new();

    protected override void OnPublisherInjected()
    {
        _lastScore.Clear();
        _lastCombo.Clear();
    }

    protected override void PublishChanges()
    {
        var score = SystemAPI.GetSingleton<Score>();
        if (_lastScore.TryUpdate(score.Value)) Publisher.Score.Publish(new ScoreChangedMessage(score.Value));
        if (_lastCombo.TryUpdate(score.Combo)) Publisher.Combo.Publish(new ComboChangedMessage(score.Combo));
    }
}
```

```csharp
var options = builder.RegisterMessagePipe();
builder.RegisterMessageBroker<ScoreChangedMessage>(options);
builder.RegisterMessageBroker<ComboChangedMessage>(options);
builder.Register<ScorePublishers>(Lifetime.Singleton);
builder.RegisterSystemFromDefaultWorld<ScorePublishSystem>();
```

束は System と同じスコープに登録してください。束の中の publisher は、そのスコープ（か親）で解決できる必要があります。
例の `ComboChangedMessage` は `IBufferedPublisher<T>` で出すので、class にしてください（struct だと下の「知っておく事」の MessagePipe の注意に当たります）。

### 最初の値を発行するかどうか

注入の時（初めての注入と、スコープを作り直した後の注入し直し）に前回値をどう扱うかを、`OnPublisherInjected()` に 1 行で書きます。

```csharp
// 1. 今の値を発行し直す: 次の更新で、今の値を必ず発行する
protected override void OnPublisherInjected() => _lastScore.Clear();

// 2. ある値を発行済みとして扱う: 今の値がその値なら黙り、違う時だけ発行する
protected override void OnPublisherInjected() => _lastScore.TreatAsPublished(0);

// 3. 前回値を覚えたままにする: OnPublisherInjected を上書きしない（前のスコープで発行した値と同じなら黙る）
```

- 1 は、購読する側が値を受け取るまで何も出せない時（新しいスコープの View を今の値で埋めたい時）に選びます
- 2 は、既定の状態なら何も知らせたくない時に選びます（例: 0 点の時は「スコアが変わった」演出を出したくない）。View の最初の表示が、渡した値の表示と同じになっている時だけ使ってください。そうでないと、今の値が渡した値と同じ時に発行されず、新しい View が最初の表示のまま残ります
- 3 は、スコープを作り直しても購読する側が前の値を覚えている時、または発行を「その時に起きた事」の知らせとして扱い、作り直した後に同じ知らせを繰り返したくない時に選びます

キー別の `LastPublished<TKey, TValue>` では、2 は `TreatEveryKeyAsPublished(value)` です。このメソッドは覚えたキーも全部忘れるので、どこで呼ぶかで意味が変わります。

```csharp
// 注入のたびに、覚えたキーを忘れ、全部のキーを value で発行済みとして扱う
protected override void OnPublisherInjected() => _lastStates.TreatEveryKeyAsPublished(State.Idle);

// System を作った時に 1 度だけ。注入し直しても各キーの前回値は覚えたままで、まだ見ていないキーだけ value で発行済みとして扱う
protected override void OnCreate() => _lastStates.TreatEveryKeyAsPublished(State.Idle);
```

### 発行するかどうかを前回値そのもので決める時

「変わったら発行する」ではなく、前回値を見て決める時（例: 前回が「動いている」だった時だけ「止まった」を出す）は、`TryGetPublished` で読み、発行した時に `TreatAsPublished` で記録します。

```csharp
foreach (var (id, state) in CurrentStates())
{
    // まだ見ていないキーは false と default。TreatEveryKeyAsPublished を呼んであれば true とその値
    var hasPublished = _lastStates.TryGetPublished(id, out var published);
    if (!ShouldPublish(hasPublished, published, state)) continue;

    _lastStates.TreatAsPublished(id, state);
    Publisher.Publish(new StateChangedMessage(id, state));
}
```

### DefaultWorldReset

既定の World を捨てて作り直します（`DefaultWorldInitialization.Initialize`）。Entity を消すだけでは System が持つ状態と World の時刻が残るので、World ごと作り直します。

```csharp
builder.Register<DefaultWorldReset>(Lifetime.Singleton).As<IDefaultWorldReset>();
```

World を作り直すと、System も別のインスタンスになります。`RegisterSystemFromDefaultWorld` で登録したコンテナは古い System を指したままで、新しい System には publisher が入らず、何も発行されません。
Reset の後は、System を登録した LifetimeScope ごと作り直してください。

### WorldClockDriver

既定の World の時間を、`Time.deltaTime` ではなく `IWorldDeltaTimeSource.DeltaSeconds` で進めます（一時停止や倍速を自分の時計で決める時に使う）。
`Start` で既定の World の `UpdateWorldTimeSystem` を止め、`Dispose` で有効に戻します（`Start` の前から無効だった時も有効にします）。1 回に進める時間は `World.MaximumDeltaTime` で頭打ちにします。
`Start` の時の既定の World を持ち続けるので、`DefaultWorldReset` の後はこれを持つスコープも作り直してください。

```csharp
builder.Register<GameClock>(Lifetime.Singleton).As<IWorldDeltaTimeSource>();
builder.RegisterEntryPoint<WorldClockDriver>();
```

## サンプル

Package Manager からこのパッケージの Samples を Import してください。置き方は各サンプルの `README.md` にあります。

- 「Minimal」: カウンターの値が MVP → ECS → MVP と 1 往復します
- 「MultiplePublishers」: 1 つの System が publisher の束で 2 種類のメッセージ（経過秒と、その偶奇）を発行します

## 知っておく事

- MessagePipe の `IBufferedSubscriber<T>` は、メッセージが struct だと、まだ何も発行していなくても購読した時に `default` を渡します（上の表の MessagePipe のコミットで、リポジトリの外の検証用のプロジェクトに MultiplePublishers サンプルを入れた EditMode テストで確かめた。このテストはこのリポジトリには入っていません）。`IBufferedPublisher<T>` で出すメッセージは class にしてください。束に入れた `IBufferedPublisher<T>` でも同じです

## 確かめた事・確かめていない事

- 確かめた事
  - 上の表の版での、このパッケージの EditMode テスト（束を VContainer のコンテナから注入して 2 種類を発行する事を含む）と、2 つのサンプルのコンパイル
  - `DefaultWorldSingleton` などの送る側の部品と `WorldClockDriver` は、0.1.0 を取り込んだ実際のゲームのプロジェクトの Play で、World を作り直した後まで動く事（Unity 6000.7.0b1、Entities 6.7.0、VContainer 1.19.0。上の表の版と同じ。利用側のプロジェクトからの報告による）
  - LifetimeScope を破棄しても、System は破棄済みの publisher を持ち続けます。MessagePipe（上の表のコミット）の破棄済みのブローカーへの `Publish` は、`IPublisher<T>` でも `IBufferedPublisher<T>` でも例外になりません（リポジトリの外の検証用のプロジェクトで、スコープを Dispose してから System を Update する EditMode テストで確かめた。このテストはこのリポジトリには入っていません）
- 確かめていない事
  - `PublishSystem<TPublisher>` と `LastPublishedValue` / `LastPublished` は、実際のゲームではまだ使われていません（EditMode テストだけ）
  - サンプルの Play モードでの動き、ビルドした実機、Entities 1.x、VContainer 1.19.0 より古い版
- 制限
  - 既定の World（`World.DefaultGameObjectInjectionWorld`）だけを扱います。名前付きの World や複数の World には対応していません
