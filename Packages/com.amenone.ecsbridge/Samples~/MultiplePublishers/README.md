# MultiplePublishers サンプル

Play してからの経過秒と、その秒が偶数か奇数かを画面に出します。1 つの System が 2 種類のメッセージを発行する書き方の例です。

1. `ElapsedSecondsSystem`（ISystem）が、World の経過時間を整数の秒にして `ElapsedSeconds` に書く
2. `ElapsedSecondsPublishSystem`（`PublishSystem<ElapsedSecondsPublishers>`）が、秒が変わった時に `ElapsedSecondsChangedMessage` を、偶奇が変わった時に `ParityChangedMessage` を発行する（ECS → MVP）
3. `ElapsedSecondsPresenter` が 2 つを購読し、`ElapsedSecondsView` に出す

`ElapsedSecondsPublishers` は publisher を名前付きのフィールドに持つだけのクラス（publisher の束）です。
`ElapsedSecondsLifetimeScope` で束を `Register` すると、VContainer が束を組み立て、基底の `[Inject] Construct` に渡します。System の中では `Publisher.Seconds` と `Publisher.Parity` で使い分けます。
`IPublisher<T>` と `IBufferedPublisher<T>` を混ぜても構いません（このサンプルでは偶奇を `IBufferedPublisher<T>` にしているので、後から購読しても今の偶奇が届きます）。
MessagePipe の `IBufferedSubscriber<T>` は、メッセージが struct だと、まだ何も発行していなくても購読した時に `default` を渡します（上と同じ検証用のプロジェクトの EditMode テストで確かめた）。`ParityChangedMessage` を class にしているのはそのためです。

## 要る物

Entities、VContainer、MessagePipe と MessagePipe.VContainer、UniTask（どれかが無いとサンプルのアセンブリはコンパイルされません）。

Play モードでの動きは確かめていません。確かめたのは、コンパイルが通る事と、リポジトリの外の検証用のプロジェクトにこのサンプルを入れた EditMode テストでの発行と購読（World の時刻を進める → System → 2 種類の発行 → 購読）だけです。このテストはこのリポジトリには入っていません。

## シーンへの置き方

1. 新しいシーンに空の GameObject を作り、`ElapsedSecondsView` を付ける
2. もう 1 つ空の GameObject を作り、`ElapsedSecondsLifetimeScope` を付けて、`View` に 1 の GameObject を入れる
3. Play すると、左上に経過秒と even / odd が出る
