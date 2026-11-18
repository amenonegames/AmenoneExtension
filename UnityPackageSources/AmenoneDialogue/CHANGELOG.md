# Changelog

## [1.0.4] - 2025-12-11
### Changes
- gon のリポジトリで公開していた版。通知は VitalRouter。この版までの変更はこの CHANGELOG に記録していない


## [2.0.0] - 2026-11-16
### Changes
- 互換性の無い変更: 通知の仕組みを VitalRouter から MessagePipe に置き換えた
  - ライブラリの中の発行は `IAsyncPublisher<T>`、購読は `IAsyncSubscriber<T>`。メッセージの型（`NodeStart`、`NodeEnd`、`DialogueEventMessage` ほか）は VitalRouter の `ICommand` を実装しなくなり、`IDialogueCommand` は無くなった
  - ブローカーの登録は `RootContainerBuilder` の `RegisterDialogueMessaging()` が行う。親のスコープに MessagePipe があればそれを使い、無ければ自分で `RegisterMessagePipe` を呼ぶ。ゲームのスコープで MessagePipe を使う時は、`RegisterMessagePipe` の戻り値の options を `RegisterDialogueMessageBrokers(options)` に渡す
- 互換性の無い変更: Root の組み立ての asmdef の名前を `amenone.gon.rootcontainer` から `amenone.dialogue.root` に変えた（GUID は同じ）
- 互換性の無い変更: 依存が変わった（1.0.4 との比較）
  - 新しく要る: MessagePipe、MessagePipe.VContainer、LitMotionText（`com.amenone.litmotiontext`）、uPools、`com.amenone.unityextension`、Input System、Addressables（`com.unity.localization` が連れてくる物を直接参照する）、NuGet の FastEnum と AutoConstructor、`com.amenone.csharpextension` 0.1.5 以上（`SerializableDictionary<TKey, TValue>`）
  - 要らなくなった: VitalRouter（git パッケージと NuGet の DLL）
  - ソースジェネレータの DLL を同梱しなくなった。1.0.4 はライブラリのフォルダの中に `UnitGenerator.dll` と `View/Amenonegames.AutoComponentProperty.dll` を置いていた。2.0.0 は AutoComponentProperty だけを使い（利用側で入れる）、UnitGenerator は使わない
- 辞書の型は `com.amenone.csharpextension` の `SerializableDictionary<TKey, TValue>`（`key` と `value` の組のリスト `pairs` で保存する）を使う（`TextVisualizePrefabIndex` の `SerializableDicGameObjPerTextVisualizeTag`、`OptionDictionary`）。Asset Store の有償アセットは要らない。1.0.4 より後の未公開の版は SerializableDictionary Pro を使っていたので、その形で保存したアセットは YAML の書き換えが要る（README の「7. 旧版からの上げ方」）
- ScriptableObject の作成メニューを足した: 「Amenone Dialogue/Create Option Dictionary」（`OptionDictionary`）、「Amenone Dialogue/TextVisualizeTagPrefabIndex」（`TextVisualizePrefabIndex`）
- 文字の演出を足した: 台詞の中の `<Twist>…</Twist>` のようなタグ（`TextVisualizeTag`）で、1 文字ずつの出方（Fall、Twist、Glitch、Melt、Sticky など約 25 種）を選べる。改行の `<br>` を扱う
- View の組み立てに `TextVisualizePrefabIndex` が要るようになった。Sticky の演出のために `StickyPullSettings` とポインタの位置を読む部品を足した
- ゲームの側から足す口として `LineDisplayExtensions`（Root に 1 つ）を足した: 台詞のタグの名前の追加、文字の演出（`IMarkupCharShowAnimator`）の追加、台詞と選択肢を表示の前に加工する `ILineViewContextProcessor` / `IOptionViewContextProcessor`、文字の長さと間隔（`LineCharTiming`）、LitMotion のスケジューラー
- 選択肢の `IsAvailable`（Yarn の条件）を表示の側まで渡すようにした（`LogicOptionData.IsAvailable`、`OptionViewData.IsAvailable`）。`LineViewContext` に Yarn の行 ID（`LineId`）を載せた
- 好きな場面から始められるようにした: `IDialogueStartable.Start(string nodeName)`、`IDialogueNodeLookup.NodeExists`。`LogicContainerBuilder` の `startsAutomatically` を外すと、組み立ての後に自動で `START` から始めなくなる
- 台本の変数を書く `IDialogueVariableWriter` を足した
- 命令の省略できる引数を扱うようにした: 登録したメソッドの引数に省略時の値があれば、台本で省いた引数をその値で埋める（`CommandDispatcher`）
- 選択肢の `Focus` / `UnFocus` で、`OptionViewBase` の UnityEvent（`focused` / `unfocused`）を呼ぶようにした
- 会話を始めるたびに `DialogueException`（No node has been selected）が起きていたのを直した。`DialogueHolder.Start` はノードを決めてから `StartDialogue` を発行し、最初の `Continue` は状態機械に任せる。1.0.4 も同じ順序だったが、例外は表に出ていなかった。Presenter の状態機械を組まずに `Start` を呼んでいた場合は、台本が自動では進まなくなる
- EditMode テストを足した（Logic 11 件、通知 22 件、View 2 件）


## [2.0.1] - 2026-11-17
### Changes
- 対話のスコープを破棄するたびに `ObjectDisposedException`（The semaphore has been disposed）がログに出る事があったのを直した。`NodeEnd` の購読の中でスコープを破棄するなど、状態の処理が走っている間に `DialogueStateQueue` が破棄されると、その処理が終わる時に起きていた。破棄の時に実行中だった処理は例外を出さずに終わり、順番を待っていた処理は実行されない
- 上の直しを守る EditMode テストを 8 件足した（`DialogueStateQueueTest`。通知のテストのアセンブリは計 30 件、ライブラリ全体で 43 件）
- README に、署名の無いパッケージの信頼度（Untrusted）の確認の画面で自動の取り込みが止まる事を書いた


## [2.0.2] - 2026-11-17
### Changes
- 動きの変更: 対話のスコープを破棄すると、実行中の状態の処理（行の表示の演出の待ちなど）は、受けた token のキャンセルで止まるようにした。2.0.1 は最後まで走り、破棄済みの View に触る事があった
  - `CancellationTokenHolder`（`ITokenGettable` の実装。Root に 1 つ）が `CancellationTokenSource` を持ち、Root のスコープの破棄でキャンセルされる token を返すようになった。2.0.1 までは、キャンセルされない token（`default`）を返していた
  - `DialogueStateQueue` は、実行する処理に「呼び出し側の token・Root の token・キュー自身の破棄」のどれかでキャンセルされる token を渡す。状態の処理（`DialogueStateHandlerBase` の `OnEnterState`）が受ける token はこれになった
  - token を見ない処理は止まらない（README の「8. 既知の制限と未確認の事」）。順番を待っていた処理を実行しない事と、破棄の後の `EnqueueAsync` が `ObjectDisposedException` になる事は 2.0.1 と同じ
  - 順番が来た時に、渡す token がもうキャンセルされていれば（キューの破棄・Root の token・呼び出し側の token のどれでも）、処理を実行しない。`UniTask` を返す版は正常に終わり、`UniTask<T>` を返す版は `OperationCanceledException` になる。状態の処理では、`StateHolder.Transition` も呼ばれない
  - キャンセルで止まった時に、エラーのログは出ない。`Publish`（待たない発行）と `PublishAsync` のどちらから入った処理でも同じ
  - `PublishAsync` を `await` している側には、キャンセルで止まった時に `OperationCanceledException` が届く。2.0.1 では正常に終わっていた。エラーのログは出ない
  - `DialogueStateQueue` を自分でコンテナに登録している場合は、`ITokenGettable` も解決できるようにする（VContainer は `[Inject]` の付いた `ITokenGettable` を受けるコンストラクタを選ぶ）。`RootContainerBuilder` の子孫なら登録済み
- 動きの変更: 命令に token を渡すようにした。登録したメソッドの `CancellationToken` 型の引数（位置は問わない）に、状態の処理の token が入る。台本の側は引数が増えない（引数の数の検査、省略時の値、エラーの文の数は `CancellationToken` を除いて数える）。`[YarnCommand]` のメソッドでも `RegisterCommandHandler` のラムダ（`Func<float, CancellationToken, UniTask>` など）でも同じ。`CancellationToken` を受けない命令は今までどおり動く
  - token のキャンセルで命令が止まった時（`OperationCanceledException`）は、エラーのログを出さずに `DispatchCommandAsync` からキャンセルとして抜ける。同期のメソッドが投げて `TargetInvocationException` に包まれた物も同じ。token がキャンセルされていない時の例外は、今までどおりエラーのログに出して握る
  - token を見ない命令は最後まで走るが、終わった時に token がキャンセル済みなら `DispatchCommandAsync` は `OperationCanceledException` で抜け、`CommandExecuteFinish` を発行しない（次の状態へ進まない）
  - `<<wait>>`（`DefaultYarnCommands.Wait`）は token のキャンセルで止まる。引数は `Wait(float seconds, CancellationToken token = default)` になった
- `DialogueStateQueue` と `CancellationTokenHolder` の `Dispose` は、`Cancel()` が例外を出しても `CancellationTokenSource` を破棄する
- `UniyLocalizationLineProvider.RefreshLocale` は、キャンセルで止まった時も読み込み中の印を戻す（2.0.1 は印が残り、`GetDialogueLine` が待ち続けていた）
- `DialogueStateQueue` に、token を受け取る処理を積む `EnqueueAsync(Func<CancellationToken, UniTask>, CancellationToken)` と `<T>` 版を足した。`Func<UniTask>` を受ける今までの口も残している。コンストラクタは、`ITokenGettable` を受ける物（VContainer はこちらを使う）と、引数なしの物（キューの破棄と呼び出し側の token だけで止まる）がある
- Presenter の asmdef（`amenone.dialogue.presenter`）が `amenone.dialogue.rootAbstract` を参照するようになった。通知のテストの asmdef（`amenone.dialogue.messaging.tests`）も同じ
- `UniyLocalizationLineProvider.Initialize` は、Root のスコープの破棄で止まった時のキャンセルを、未処理の例外としてログに出さない
- README の「6. ゲーム側からの拡張点」に、送りと早送りをゲームのコードから起こす方法を書いた
- 決まりを足した: MessagePipe の購読（subscriber 系の interface を受け取る事）は Presenter のアセンブリ（`amenone.dialogue.presenter`）だけで行う。発行は縛らない。ライブラリの中は `SubscriberLocationTest` が、Presenter 以外のアセンブリの型がコンストラクタの引数・`[Inject]` のメソッドの引数・フィールド・プロパティで subscriber を持たない事を確かめる（README の「6. ゲーム側からの拡張点」）
- EditMode テストを 50 件足した（`CancellationTokenHolderTest` 5、`DialogueStateQueueCancellationTest` 16、`DialogueStateHandlerCancellationTest` 5、`CommandRunningHandlerCancellationTest` 3、`SubscriberLocationTest` 3、`CommandDispatcherCancellationTest` 18。Logic のテストのアセンブリは計 29 件、通知のテストのアセンブリは計 62 件、ライブラリ全体で 93 件）


## [2.1.0] - 2026-11-17
### Changes
- 失敗の通知 `DialogueFailed`（`LogicAbstract`）を足した。`DialogueHolder.Continue` の中で Yarn の VM が例外を出した時（`<<jump>>` 先のノードが無いなど）に、例外をログに出してから 1 度発行する。今までは例外が状態の処理の外へ抜け、`NodeEnd` も届かずに対話が止まったままになり、ゲームの側が気づく口が無かった
  - 発行した後の対話は止まったまま。続けるかどうかはゲームの側が決める（`IDialogueStartable.Start` で始め直すか、閉じる）。`Stop` を呼んだ後に `Start` で別のノードを始め直せる（その最初の行が届き、`DialogueFailed` は増えない事をテストで確かめている）
  - 命令（`CommandDispatcher`）の例外は今までどおりエラーのログに出して握り、対話は次へ進む。`DialogueFailed` は出ない
  - `RegisterDialogueMessageBrokers` が `DialogueFailed` のブローカーも登録する。`RegisterDialogueMessageBrokers` を使わずにブローカーを自分で登録している場合は、`DialogueFailed` の登録も要る
- 互換性の無い変更: `DialogueHolder` のコンストラクタの引数に `IAsyncPublisher<DialogueFailed>` が増えた（`eventPublisher` と `TokenGettable` の間）。コンテナ（`LogicContainerBuilder`）に作らせている場合は直しは要らない。自分で `new` している場合は引数を足す
- EditMode テストを 3 件足した（`DialogueHolderTest`。Logic のテストのアセンブリは計 32 件、ライブラリ全体で 96 件）


## [2.1.1] - 2026-11-18
### Changes
- `<<wait>>` を `[YarnCommand]` でなく `RegisterCommandHandler` で登録するようにした（`DefaultYarnCommands.RegisterTo`）。Yarn Spinner for Unity のソースジェネレータは、名前が `YarnCommandAttribute` の属性を付けたメソッドから言語サーバー用の命令の一覧（`ProjectSettings/Packages/dev.yarnspinner/<アセンブリ名>-generated.ysls.json`）を作り、2.0.2 で `Wait` に足した `CancellationToken` の引数を Yarn の型でないと見て、`wait` を `containsErrors: true` にしていた（コンパイルの警告 `YS1011`）。`amenone.dialogue.logic` の一覧には `wait` が載らなくなる。台本の書き方（`<<wait 1>>`）と、token のキャンセルで止まる動きは変わらない
- 互換性の注意: `DefaultYarnCommands.Wait` の署名は変わらないが、属性が外れたので、`RegisterCommandsFromInstance(new DefaultYarnCommands())` では `wait` が登録されなくなった。自分でそう登録している場合は `new DefaultYarnCommands().RegisterTo(dispatcher)` に替える。`LogicContainerBuilder` に作らせている場合は直しは要らない
- README の「6. ゲーム側からの拡張点」に、token を受ける命令で一覧をきれいに保つ書き方を、「8. 既知の制限と未確認の事」に、一覧が作られる仕組みと、テストのアセンブリの一覧（`amenone.dialogue.logic.tests-generated.ysls.json`）が作られる事を書いた
