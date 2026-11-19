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

- `OnPublisherInjected()`: World を作り直さずに LifetimeScope だけを作り直すと、World に残った同じ System へ publisher が注入し直されます。前回値はここで戻してください（戻さないと、新しいスコープの購読者に今の値が届きません）
- `LastPublishedValue<T>`: `TryUpdate(value)` は初回と値が変わった時に true。`Clear()` で初回に戻ります。比較は `EqualityComparer<T>.Default`
- `LastPublished<TKey, TValue>`: キーごとの前回値。使い方は同じです

基底クラスを使う時と使わない時の目安:

- 1 つの System が 1 種類のメッセージを出すなら `PublishSystem<TPublisher>` を継ぎます
- 複数の種類のメッセージを出す System は、この基底を使わず `SystemBase` を直接継ぎ、`[Inject] public void Construct(...)` で publisher を全部受け、`OnUpdate` の先頭で受け取り済みかを確かめてください

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

Package Manager からこのパッケージの Samples の「Minimal」を Import してください。カウンターの値が MVP → ECS → MVP と 1 往復します。置き方は `Samples~/Minimal/README.md` にあります。

## 既知の制限・確かめていない事

- 既定の World（`World.DefaultGameObjectInjectionWorld`）だけを扱います。名前付きの World や複数の World には対応していません
- LifetimeScope を破棄しても、System は破棄済みの publisher を持ち続けます。MessagePipe（上の表のコミット）の破棄済みのブローカーへの `Publish` は、`IPublisher<T>` でも `IBufferedPublisher<T>` でも例外になりません（スコープを Dispose してから System を Update する EditMode テストで確かめた）
- 確かめたのは上の表の版での EditMode テストと、サンプルのコンパイルだけです。Play モードでの動き、ビルドした実機、Entities 1.x、VContainer 1.19.0 より古い版では確かめていません
