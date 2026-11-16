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
  - 新しく要る: MessagePipe、MessagePipe.VContainer、LitMotionText（`com.amenone.litmotiontext`）、uPools、`com.amenone.unityextension`、Input System、Addressables（`com.unity.localization` が連れてくる物を直接参照する）、NuGet の FastEnum と AutoConstructor、Rotary Heart の SerializableDictionary Pro（Asset Store）
  - 要らなくなった: VitalRouter（git パッケージと NuGet の DLL）
  - ソースジェネレータの DLL を同梱しなくなった。1.0.4 はライブラリのフォルダの中に `UnitGenerator.dll` と `View/Amenonegames.AutoComponentProperty.dll` を置いていた。2.0.0 は AutoComponentProperty だけを使い（利用側で入れる）、UnitGenerator は使わない
- ScriptableObject の作成メニューを足した: 「Amenone Dialogue/Create Option Dictionary」（`OptionDictionary`）、「Amenone Dialogue/TextVisualizeTagPrefabIndex」（`TextVisualizePrefabIndex`）
- 文字の演出を足した: 台詞の中の `<Twist>…</Twist>` のようなタグ（`TextVisualizeTag`）で、1 文字ずつの出方（Fall、Twist、Glitch、Melt、Sticky など約 25 種）を選べる。改行の `<br>` を扱う
- View の組み立てに `TextVisualizePrefabIndex` が要るようになった。Sticky の演出のために `StickyPullSettings` とポインタの位置を読む部品を足した
- ゲームの側から足す口として `LineDisplayExtensions`（Root に 1 つ）を足した: 台詞のタグの名前の追加、文字の演出（`IMarkupCharShowAnimator`）の追加、台詞と選択肢を表示の前に加工する `ILineViewContextProcessor` / `IOptionViewContextProcessor`、文字の長さと間隔（`LineCharTiming`）、LitMotion のスケジューラー
- 選択肢の `IsAvailable`（Yarn の条件）を表示の側まで渡すようにした（`LogicOptionData.IsAvailable`、`OptionViewData.IsAvailable`）。`LineViewContext` に Yarn の行 ID（`LineId`）を載せた
- 好きな場面から始められるようにした: `IDialogueStartable.Start(string nodeName)`、`IDialogueNodeLookup.NodeExists`。`LogicContainerBuilder` の `startsAutomatically` を外すと、組み立ての後に自動で `START` から始めなくなる
- 台本の変数を書く `IDialogueVariableWriter` を足した
- 命令の省略できる引数を扱うようにした: 登録したメソッドの引数に省略時の値があれば、台本で省いた引数をその値で埋める（`CommandDispatcher`）
- 選択肢の `Focus` / `UnFocus` で、`OptionViewBase` の UnityEvent（`focused` / `unfocused`）を呼ぶようにした
- EditMode テストを足した（Logic 9 件、通知 22 件）
