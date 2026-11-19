# Minimal サンプル

ボタンを押すとカウンターが 1 増え、その値が画面に出ます。

1. `CounterView`（MonoBehaviour）がボタンを押された事を `CounterPresenter` に知らせる
2. `CounterIncrementSender` が `DefaultWorldSingleton<CounterIncrementRequest>` に増やす量を書く（MVP → ECS）
3. `CounterSystem`（ISystem）が `Counter` を進める
4. `CounterPublishSystem`（`PublishSystem<IPublisher<CounterChangedMessage>>`）が、値が変わった時だけ発行する（ECS → MVP）
5. `CounterPresenter` が `ISubscriber<CounterChangedMessage>` で受け、`CounterView` に出す

## 要る物

Entities、VContainer、MessagePipe と MessagePipe.VContainer、UniTask（どれかが無いとサンプルのアセンブリはコンパイルされません）。

Play モードでの動きは確かめていません。確かめたのは、コンパイルが通る事と、リポジトリの外の検証用のプロジェクトにこのサンプルを入れた EditMode テストでの 1 往復（送る側 → System → 発行 → 購読）だけです。このテストはこのリポジトリには入っていません。

## シーンへの置き方

1. 新しいシーンに空の GameObject を作り、`CounterView` を付ける
2. もう 1 つ空の GameObject を作り、`CounterLifetimeScope` を付けて、`View` に 1 の GameObject を入れる
3. Play して、左上の「+1」を押す

`CounterPublishSystem` の `OnPublisherInjected()` は `_lastValue.Clear()` で、注入のたびに次の更新で今の値を発行し直します（新しいスコープの `CounterView` の「-」を今の値で埋めるため）。最初の値を発行しない書き方は、パッケージの README の「最初の値を発行するかどうか」にあります。

`CounterSystem` と `CounterPublishSystem` は、Entities が既定の World を作る時に自動で作られます。`CounterLifetimeScope` は `RegisterSystemFromDefaultWorld` でその System を見つけて publisher を注入します。
