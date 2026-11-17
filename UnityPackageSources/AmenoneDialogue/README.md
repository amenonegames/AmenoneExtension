# Amenone Dialogue System

版: 2.0.1（`package-version.txt`）。変更の記録は `CHANGELOG.md`。

## 1. これは何か

[Yarn Spinner](https://www.yarnspinner.dev/) の `Yarn.Dialogue`（台本の VM）を土台にした、Unity 向けの対話ライブラリ。台詞・選択肢・命令の進行を状態機械で回し、TextMeshPro の台詞を 1 文字ずつ演出つきで出す。組み立ては VContainer、層の間の通知は MessagePipe。

層ごとに asmdef が分かれている。

| フォルダ | asmdef | 役割 |
|---|---|---|
| `Identifier/` | `amenone.dialogue.identifier` | 状態（`DialogueState`）、入力の状態、演出のタグ（`TextVisualizeTag`）の列挙 |
| `LogicAbstract/` | `amenone.dialogue.logic.abstract` | Logic の口（`IDialogueStartable`、`ICommandStorage`、`IDialogueVariableWriter` ほか）と、層の間で送るメッセージの型 |
| `Logic/` | `amenone.dialogue.logic` | `Yarn.Dialogue` を持ち、行・選択肢・命令・場面の始まりと終わりをメッセージにして発行する。命令の振り分け（`CommandDispatcher`）、台本の変数、文字の速さ。組み立ては `LogicContainerBuilder` |
| `ViewAbstract/` | `amenone.dialogue.view.abstract` | 表示に渡すデータ（`LineViewContext`、`OptionViewContext`）と、表示の拡張の口（`LineDisplayExtensions`、`IMarkupCharShowAnimator`） |
| `View/` | `amenone.dialogue.view` | 台詞の表示（`LineView`、TMP）、文字の演出（`Line/CharShowAnimators/`、約 25 種）、選択肢、送り・早送りのボタン。組み立ては `ViewContainerBuilder` |
| `Presenter/` | `amenone.dialogue.presenter` | メッセージを購読して状態機械を回す（`DialogueState/`）、待っている入力に合わせてボタンを出し入れする（`Input/`） |
| `Presenter/Adapters/` | `amenone.dialogue.adapter` / `amenone.dialogue.adapter.abstract` | 行 ID から文を引き（Yarn の言語別の文、または Unity Localization）、タグを 1 文字ごとの印に分けて表示のデータにする |
| `Presenter/ContainerBuilder/` | `amenone.dialogue.presenterlayer.container` | Presenter の組み立て（`PresenterContainerBuilder`） |
| `Root/Abstract/` | `amenone.dialogue.rootAbstract` | 対話の寿命の CancellationToken の口（`ITokenGettable`） |
| `Root/Container/` | `amenone.dialogue.root` | いちばん親の組み立て（`RootContainerBuilder`）。MessagePipe のブローカーの登録（`DialogueMessagingExtensions`）と `LineDisplayExtensions` を置く |
| `Tests/` | `amenone.dialogue.logic.tests` / `amenone.dialogue.messaging.tests` / `amenone.dialogue.view.tests` | EditMode テスト（Editor のみ、`UNITY_INCLUDE_TESTS` の時だけ） |

## 2. 入れ方

1. 先に「3. 依存の一覧」の物を入れる（入れないと、取り込んだ直後にコンパイルが止まる）
2. `.unitypackage` は https://github.com/amenonegames/AmenoneExtension の Releases に、タグ `dialogue/<版>` で置く（ファイル名 `AmenoneDialogue-<版>.unitypackage`）。取ったファイルを Unity の Assets > Import Package > Custom Package で入れる（バッチモードなら `-importPackage <パス>`）
   - この `.unitypackage` には署名が無い。Unity の版によっては、開いている Editor で取り込む時に、署名の無いパッケージの信頼度（Untrusted）の確認の画面が出る。画面を人が進めるまで Editor のメインスレッドが止まるので、`AssetDatabase.ImportPackage` をスクリプトや外部のツールから呼ぶ自動の取り込みは、そこで応答が無くなる。人が画面で進めると取り込みは続く。バッチモードの `-importPackage`（Unity 6000.7.0b1）では画面は出なかった
3. `Assets/AmenoneDialogue/` に展開される。`.meta` は同梱の物がそのまま使われ、GUID は変わらない

ライブラリは Unity のパッケージ（UPM）ではなく `Assets/` の下のソースとして入る。利用側で書き換えてよい。書き換えた後に新しい版を入れると、書き換えは上書きされるので、どの版から書き換え始めたかを `package-version.txt` で控えておく。

既に同じ GUID のファイルを持つプロジェクト（旧版を `Assets/Scripts/` などに置いているプロジェクト）に入れると、次のようになる。

- 既にあるファイルは、今の場所のまま中身が新しい版に置き換わる
- 既にあるフォルダの中に増えたファイルも、親のフォルダの GUID をたどって今の場所（例: `Assets/Scripts/Root/Container/DialogueMessagingExtensions.cs`）に置かれる
- 根のフォルダの直下に増えた物（2.0.0 では `README.md`、`CHANGELOG.md`、`package-version.txt`、旧版に無ければ `Tests/`）は、旧版の根のフォルダ（例: `Assets/Scripts`）には入らず、`Assets/AmenoneDialogue/` という別の木に置かれる。ログに `GUID [b5dc9995de9d4339b194f0ae42ae4a8d] for asset 'Assets/AmenoneDialogue' conflicts with: 'Assets/Scripts' (current owner) Assigning a new guid.` と出て、`Assets/AmenoneDialogue` のフォルダには新しい GUID が振られる。旧版を先に `Assets/AmenoneDialogue` へ移しておけば起きない（「7. 旧版からの上げ方」）
- パッケージに無いファイルは消されずに残る（2.0.0 で、旧版からファイル単位で消えた物はソースジェネレータの DLL だけ。これも残る）

上げる時の手順は「7. 旧版からの上げ方」。

## 3. 依存の一覧

### `Packages/manifest.json` に足す物

git の依存はコミットの hash で固定した例（「10. 確かめた構成」の組み合わせ）。

```json
{
  "dependencies": {
    "com.amenone.csharpextension": "https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.csharpextension#d966d0e7d47e55fa76565cafe784643a3bb5b8c1",
    "com.amenone.unityextension": "https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.unityextension#1ec3f5553fb72cab03f758bef9a89070fd8e44be",
    "com.amenone.vcontainerextensions": "https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.vcontainerextensions#1ec3f5553fb72cab03f758bef9a89070fd8e44be",
    "com.amenone.litmotiontext": "https://github.com/amenonegames/LitMotionText.git?path=/Assets/LitMotionText#c32ed88afbdc961b5ed1232a6b5020b8cbe2e092",
    "com.annulusgames.lit-motion": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion#422eb124051c81a9bc3f422ebf190703c5053514",
    "com.annulusgames.u-pools": "https://github.com/AnnulusGames/uPools.git?path=/Assets/uPools#e11ce9c12c65c12f3bc35ae2ef7cacde6826e503",
    "com.cysharp.messagepipe": "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe#3adc01deb1a283cb2faeb36c00178b84884a7fc3",
    "com.cysharp.messagepipe.vcontainer": "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer#3adc01deb1a283cb2faeb36c00178b84884a7fc3",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#f74aace058f4ee7d3aa5bcc89cc98dbfb2a02892",
    "jp.hadashikick.vcontainer": "1.19.0",
    "com.unity.inputsystem": "6.7.0",
    "com.unity.localization": "2.0.0",
    "com.unity.ugui": "2.7.0",
    "com.unity.test-framework": "1.9.0"
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "jp.hadashikick.vcontainer"
      ]
    }
  ]
}
```

- `com.amenone.csharpextension` は 0.1.5 以上が要る（`SerializableDictionary<TKey, TValue>` を使う）。上の例の hash は 0.1.5 を含むコミット。行に hash を書かない形で入れているプロジェクトでは、版は `Packages/packages-lock.json` の hash で固定される。lock の hash が 0.1.5 より古いと新しい型が無く、`TextVisualizePrefabIndex` などがコンパイルエラーになるので、Package Manager の Update を押すか、lock の `com.amenone.csharpextension` の塊を消して開き直し、パッケージを新しくする
- VContainer は OpenUPM から入れるので、`scopedRegistries` の項目も要る（既にあれば `scopes` に `jp.hadashikick.vcontainer` を足す）
- `com.unity.addressables` はライブラリが直接参照するが、`com.unity.localization` が連れてくるので、manifest に書かなくてよい
- `com.unity.test-framework` は、ライブラリの EditMode テストを回す時だけ要る

### NuGet（NuGetForUnity の `Assets/packages.config`）

AutoConstructor は 1.0.4 には無く、2.0.0 で要る（`DialogueVariableWriter` の `[AutoConstructor]`）。旧版から上げる時に足りない事がある（「7. 旧版からの上げ方」）。

```xml
<package id="AutoConstructor" version="5.6.0" manuallyInstalled="true" />
<package id="FastEnum" version="1.8.0" manuallyInstalled="true" />
```

### ソースジェネレータ

- `Amenonegames.AutoComponentProperty.dll`（`[CompoProp]`。`LineView` と `OptionViewBase` が使う）: `RoslynAnalyzer` のラベルを付けた `.meta` ごと `Assets/` の下に置く。このパッケージには入れていない。公開の取得元は未確認
- NullObjectGenerator（`IContinueActivatable` の `[InterfaceToNullObj]`）: `com.amenone.vcontainerextensions` に同梱の `Scripts/DLL/NullObjectGeneratorForNewerThan2022.2.dll` が受ける。追加の作業は要らない

### 自分で用意する物（同梱していない）

| 物 | 置き場所の例 | メモ |
|---|---|---|
| Yarn Spinner for Unity（`dev.yarnspinner.unity`） | `Packages/dev.yarnspinner.unity/`（埋め込み） | Asset Store の有償の版。アドオンは要らない |

再配布できないので、このパッケージに入れていない。

### Scripting Define Symbols

- `FORCE_YARNLOCALIZATION`: 付けると、台詞の文を Yarn のプロジェクトの言語別の文から引く（`YarnLocalizationLineProvider`）。付けないと Unity Localization から引く（`UniyLocalizationLineProvider`）。切り替えは `PresenterContainerBuilder` にある

### `csc.rsp`

`LogicAbstract/csc.rsp` と `ViewAbstract/csc.rsp`（`-langVersion:10`）を同梱している。`record struct` を使うため。追加の作業は要らない。

## 4. 組み立て方と落とし穴

シーンや prefab の見本は、この版には同梱していない。

### 最小の組み立て

1. 4 つの GameObject に、`RootContainerBuilder`、`LogicContainerBuilder`、`ViewContainerBuilder`、`PresenterContainerBuilder` を 1 つずつ付ける（どれも VContainer の `LifetimeScope`）
2. 親子の順は Root → Logic → View → Presenter。各 `LifetimeScope` の Inspector の Parent（VContainer の `parentReference`）に、1 つ上の型を選ぶ（Logic の親は `RootContainerBuilder`、View の親は `LogicContainerBuilder`、Presenter の親は `ViewContainerBuilder`）。ゲームの `LifetimeScope` を Root の親に置いてもよい（「5. MessagePipe の登録」）
3. 下の表の「必ず割り当てる」物を割り当てる
4. `LogicContainerBuilder` の `startsAutomatically` が付いていれば、組み立てが終わった時に `START` の場面から始まる

| ContainerBuilder | 必ず割り当てる | 任意 |
|---|---|---|
| `RootContainerBuilder` | なし | なし |
| `LogicContainerBuilder` | `yarnProject`（`YarnProject`）。対話を作る時に `YarnProject.Program` を読む | `startsAutomatically`（既定は付いている。外すと自動で始まらない） |
| `ViewContainerBuilder` | `lineViewParent`（子に `LineView` を置いた Transform）、`optionRootsParent`（子に `OptionRoot` を置いた Transform）、`textFeedButton`（`TextFeedButton`）、`fastForwardButton`（`FastForwardButton`）、`optionViewBasePrefab`（`OptionViewBase` の prefab）、`textVisualizePrefabIndex`（`TextVisualizePrefabIndex`）。さらに VContainer の `autoInjectGameObjects` に、`LineView` を置いた GameObject を入れる | `stickyPullSettings`（既定値あり） |
| `PresenterContainerBuilder` | なし | なし |

- 必ず割り当てる物が無い時、組み立て（`Configure`）の中で null の参照を使う。`lineViewParent` と `optionRootsParent` は `GetComponentsInChildren` を呼ぶので例外になる。ほかの物も含め、どの例外で止まるかは Play モードで確かめていない
- `LineView` は `lineViewParent` の子から集めて配列のまま登録するので、`autoInjectGameObjects` に入れないと `[Inject]`（`Construct`）が届かない。入れ忘れると、台詞を出す所で例外になって進行が止まる（例外がどこに出るかは未確認）
- `LineView` は `[CompoProp]`、選択肢の prefab の `OptionViewBase` は `[CompoProp(GetFrom.Children)]`（子から）で `TMP_Text` を取るので、その位置に `TMP_Text` を置く
- `LineView` の `lineViewKey` と `OptionRoot` の `key` が、行のタグ（`#MiddleCenter` など、`LineViewKey`）と選択肢のタグ（`OptionViewKey`）で選ぶ置き場所になる
- この最小の手順そのものを Play モードで動かす事は確かめていない（Play モードで確かめたのは、実際のゲームの組み立て。「10. 確かめた構成」）

### 落とし穴

- `textVisualizePrefabIndex` の中身は空でよい。`Fall` の演出が `TextVisualizeTag.Fall` のプレハブを引く時だけ中身が要る。作るのはメニュー「Amenone Dialogue/TextVisualizeTagPrefabIndex」
- 選択肢の置き場所の対応の `OptionDictionary` は、メニュー「Amenone Dialogue/Create Option Dictionary」で作る
- `TextVisualizePrefabIndex` の `dic` と `OptionDictionary` は、`com.amenone.csharpextension` の `SerializableDictionary<TKey, TValue>`。インスペクタでは `pairs`（`key` と `value` の組のリスト）として編集する。リストに要素を足すと直前の要素が複製されてキーが重なるので、足した後にキーを直す。キーが重なっていても読み込みで例外にはならず、先に書かれた組が使われる
- 台詞の文をどこから引くかは `FORCE_YARNLOCALIZATION` で決まる（「3. 依存の一覧」）
- 台詞の中のタグ（`<Twist>…</Twist>` など）は 1 文字ごとの印（`CharData.MarkUp`）になる。受けるタグの名前は `TextVisualizeTag` の列挙と、`LineDisplayExtensions.AddMarkupName` で足した名前。知らないタグは TMP の文字としてそのまま出る

## 5. MessagePipe の登録

ライブラリの Logic・View・Presenter は別々のスコープなので、ブローカー（`IAsyncPublisher<T>` / `IAsyncSubscriber<T>`）は全員が見える親のスコープに 1 組だけ置く必要がある。`RootContainerBuilder` は `builder.RegisterDialogueMessaging()` を呼び、次のように振る舞う（`Root/Container/DialogueMessagingExtensions.cs`）。

- 親のスコープに `IAsyncPublisher<NodeStart>` があれば、何もせず親のブローカーを使う
- 親のスコープに `MessagePipeOptions` だけがあり、ブローカーが無ければ、直し方を書いた `InvalidOperationException` を投げる
- どちらも無ければ（単体で動かす時）、Root が自分で `RegisterMessagePipe()` を呼び、ライブラリのメッセージのブローカーを登録する

ゲームに組み込む時は、ゲームのいちばん親のスコープで MessagePipe を登録し、その options をライブラリに渡す。

```csharp
protected override void Configure(IContainerBuilder builder)
{
    var options = builder.RegisterMessagePipe();
    builder.RegisterDialogueMessageBrokers(options);
    // ゲームのメッセージのブローカーも同じ options で登録する
}
```

ゲームの asmdef に `MessagePipe`、`MessagePipe.VContainer`、`amenone.dialogue.root` への参照が要る。

- MessagePipe（コア）の asmdef（`MessagePipe.asmdef`）は、`versionDefines` で VContainer 1.14.0 以上を見つけると `MESSAGEPIPE_OPENGENERICS_SUPPORT` を付ける。`MESSAGEPIPE_OPENGENERICS_SUPPORT && UNITY_2022_1_OR_NEWER` の時、`RegisterMessagePipe()` は型を決めない（open generic の）登録を一括で行う（`ServiceCollectionExtensions.cs`）。このため、この条件では `RegisterDialogueMessageBrokers(options)` を省いても、ライブラリの型が解決でき、Root も例外を投げない
- それでも明示を勧める。条件を満たさない環境（VContainer 1.14.0 より前など）では一括の登録が無いので、省くと Root が例外を投げる。IL2CPP では、型を決めた登録の方が AOT で型が落ちる心配が少ない（open generic の登録が IL2CPP で動くかは未確認）。また、どの型がどこで登録されているかがコードから読める
- `RegisterDialogueMessageBrokers` に渡す options の `InstanceLifetime` は `Singleton`（MessagePipe の既定）にする。`Scoped` などだと、Logic・View・Presenter のスコープごとに別々のブローカーができてメッセージが届かないので、例外を投げる
- 同時に 2 つの対話を走らせる場合、ブローカーをゲームのいちばん親に置くと、2 つの対話のメッセージが同じブローカーを通って混ざる。対話の連鎖（Root → Logic → View → Presenter）ごとに、その Root か Root のすぐ上のスコープで登録する（例えば、ゲームの親に MessagePipe を置かず、Root に自分で登録させる）。2 つ同時に走らせる形は未確認

メッセージは `IAsyncPublisher<T>.Publish`（待たずに進む）で発行される。受ける側は `IAsyncSubscriber<T>` で購読する。同期の `ISubscriber<T>` は MessagePipe の別のブローカーなので、ライブラリのメッセージは届かない。

## 6. ゲーム側からの拡張点

ゲームの `LifetimeScope` を、使う口が登録されているスコープの子孫に置いて解決する。Logic の口（`ICommandStorage`、`IDialogueVariableWriter`、`IDialogueStartable` など）は `LogicContainerBuilder` の子孫から、`LineDisplayExtensions` は `RootContainerBuilder` の子孫から解決できる。

### 命令の登録（`ICommandStorage`、`LogicAbstract`）

台本の `<<名前 引数…>>` を、名前で登録したメソッドに振り分ける。引数は文字列から引数の型に変換される。省略時の値を持つ引数は台本で省ける。`UniTask` を返すメソッドを登録すると、終わるまで次へ進まない。

```csharp
public sealed class AudioCommands
{
    private readonly ICommandStorage _commands;
    public AudioCommands(ICommandStorage commands) => _commands = commands;

    public void RegisterCommands()
    {
        _commands.RegisterCommandHandler<string>("play_sound", PlaySound);      // <<play_sound door>>
        _commands.RegisterCommandHandler<float>("fade_out", FadeOutAsync);     // <<fade_out 0.5>>
    }

    private void PlaySound(string soundName) { /* ... */ }
    private UniTask FadeOutAsync(float seconds) { /* ... */ return UniTask.CompletedTask; }
}
```

登録は組み立ての後に行う（例: `builder.RegisterBuildCallback(r => r.Resolve<AudioCommands>().RegisterCommands());`）。

### 表示の拡張（`LineDisplayExtensions`、`ViewAbstract`）

Root に 1 つだけある。子のスコープで登録した物は親から見えないので、ゲームの組み立ての `RegisterBuildCallback` で解決して足す。

```csharp
builder.RegisterBuildCallback(resolver =>
{
    var display = resolver.Resolve<LineDisplayExtensions>();
    display.AddMarkupName("emphasis");                       // 台詞のタグ <emphasis>…</emphasis> を印として受ける
    display.AddCharShowAnimator(new MyCharShowAnimator());   // 印の名前（MarkupName）で文字の演出を足す。名前も受けるようになる
    display.AddContextProcessor(new MyLineProcessor());      // 台詞を表示の前に加工する
    display.AddOptionContextProcessor(new MyOptionProcessor());
    display.UseCharTiming(new LineCharTiming(0.08f, 0.06f)); // 1 文字の長さと次の文字までの間隔（秒、文字の速さを掛ける前）
    display.UseMotionScheduler(myScheduler);                 // LitMotion のスケジューラー（ゲームの時計に乗せる時）
});
```

- `IMarkupCharShowAnimator`: `MarkupName`（受ける印の名前）、`Register`（その文字の動きを `MotionSequenceBuilder` に積む）、`GetIntervalToNextCharEntrance`（次の文字が出始めるまでの間隔）、`OnTextFeed`（送られた時）
- `ILineViewContextProcessor.Process(LineViewContext)`: 文字・話し手の名前・印・行 ID（`LineId`）を受けて、替えた物を返す。足した順に回る
- `IOptionViewContextProcessor.Process(OptionViewContext)`: 選択肢を出す前に、文と出すかどうかを替える。`OptionViewData.IsAvailable` には Yarn の条件の結果が入って来る。`index` は Yarn の選択肢の番号のまま保つ

### 台本の変数の書き込み（`IDialogueVariableWriter`、`LogicAbstract`）

```csharp
_variables.Write("$score", 10f);
_variables.Write("$met_guide", true);
_variables.Write("$player_name", "Alex");
```

対話が読む Logic のスコープの変数の入れ物に書く（子のスコープから解決しても同じ入れ物）。

### 場面を指定して開始（`IDialogueStartable`、`IDialogueNodeLookup`、`LogicAbstract`）

`LogicContainerBuilder` の `startsAutomatically` を外し、ゲームの側から始める。

```csharp
if (_nodes.NodeExists("Intro")) _dialogue.Start("Intro");
```

`Start()` は `START` から始める。`Stop()` で止める。

### 終わりの通知（`NodeEnd`、`LogicAbstract`）

`NodeEnd` は、Yarn の対話全体が終わった時（`<<jump>>` せずに最後の場面が終わった時。`Dialogue.DialogueCompleteHandler`）に 1 度発行される。場面ごとの終わりではない。

```csharp
public sealed class DialogueEndListener : IInitializable, IDisposable
{
    private readonly IAsyncSubscriber<NodeEnd> _nodeEnd;
    private IDisposable _subscription;

    public DialogueEndListener(IAsyncSubscriber<NodeEnd> nodeEnd) => _nodeEnd = nodeEnd;

    public void Initialize() => _subscription = _nodeEnd.Subscribe((_, token) => OnDialogueEndAsync(token));

    public void Dispose() => _subscription?.Dispose();

    private UniTask OnDialogueEndAsync(CancellationToken token) { /* ... */ return UniTask.CompletedTask; }
}
```

場面の始まりは `NodeStart`（`Context.NodeName`）で受けられる。

### そのほか

- 文字の速さ: `ITextSpeedHandlable.SetTextSpeed(float)`（既定 1）
- 選択肢の見た目: `OptionViewBase` の `focused` / `unfocused`（UnityEvent）が `Focus` / `UnFocus` で呼ばれる

## 7. 旧版からの上げ方

旧版（gon のリポジトリで公開していた 1.0.4 と、それより後の公開していない版）は、根のフォルダと各ファイルの GUID が 2.0.0 と同じなので、上書きで入る。ただし旧版を `Assets/AmenoneDialogue/` 以外（`Assets/Scripts/` など）に置いていると、根の直下に増えた物が別の木に置かれる（「2. 入れ方」）。実際のプロジェクトは版がはっきり分かれているとは限らないので、版で分けずに、次のうち自分のプロジェクトに当てはまる物だけを行う。

### 1. 入れる前に

- [ ] **旧版を置いているフォルダ**: `Assets/AmenoneDialogue/` 以外に置いていて、そのフォルダにライブラリだけが入っているなら、Unity を閉じた状態で、フォルダを `.meta` ごと `Assets/AmenoneDialogue` に名前を変えて移す（例: `Assets/Scripts` → `Assets/AmenoneDialogue`、`Assets/Scripts.meta` → `Assets/AmenoneDialogue.meta`）。GUID は同じなので参照は切れない。フォルダを動かしたくない時は、入れた後に「3. 入れた後に」の手順で片付ける
- [ ] **辞書のアセット**: `TextVisualizePrefabIndex` か `OptionDictionaryAsset` のアセットを持っているか探す（`grep -rl -e 23f54df89d71456b8616a10a678bc9fe -e 037fce639990408ba79957c5b635e9e9 Assets --include=*.asset`）。あれば控えておく（git に入っていればコミットしておく）。書き換えは「辞書のアセットの書き換え」
- [ ] **依存**: 「3. 依存の一覧」のうち、まだ無い物を入れる。旧版から上げる時に足りなくなりやすいのは、MessagePipe.VContainer（無いと `DialogueMessagingExtensions.cs` で `RegisterMessagePipe` / `RegisterMessageBroker` が見つからない `error CS1061`）、NuGet の AutoConstructor（`DialogueVariableWriter` の `[AutoConstructor]` が使う。1.0.4 には無かった）、`com.amenone.csharpextension` 0.1.5 以上（lock の更新が要る事がある）

### 2. 入れる

- `.unitypackage` を入れる。辞書のアセットは、書き換えるまでインスペクタで触ったり保存したりしない
- 開いている Editor で取り込むと、署名の無いパッケージの信頼度（Untrusted）の確認の画面が出る事がある。この画面は人の操作が要る。Editor を外部のツールから動かして取り込む時は、応答が無くなったらこの画面が出ていないかを見て、人が画面で進める（「2. 入れ方」）

### 3. 入れた後に

- [ ] **別の木ができた時**（旧版のフォルダを移さずに入れた時）: `Assets/AmenoneDialogue/` に、根の直下に増えた物（`README.md`、`CHANGELOG.md`、`package-version.txt`、`Tests/` など）が置かれる。Unity を閉じた状態で、その中身を 1 つずつ `.meta` ごと旧版のフォルダの直下へ移し、空になった `Assets/AmenoneDialogue/` と `Assets/AmenoneDialogue.meta` を消す（この `.meta` は Unity が新しく振った GUID なので、残さなくてよい）。開き直すと、GUID の衝突は出ない
- [ ] **辞書のアセット**: 下の「辞書のアセットの書き換え」で書き換える
- [ ] **VitalRouter を使うゲームのコード**: 次のとおり直す
  - 購読: `ICommandSubscribable.Subscribe<T>((cmd, ctx) => …)` → `IAsyncSubscriber<T>.Subscribe((message, token) => …)`。戻り値の `IDisposable` を持ち、捨てる時に `Dispose` する
  - 発行: `ICommandPublisher.PublishAsync(message)` → `IAsyncPublisher<T>.PublishAsync(message, token)`（待つ）または `Publish(message, token)`（待たない）
  - メッセージの型は `ICommand` を実装しなくなった。`IDialogueCommand` は無い
  - `[Routes]` と `RegisterVitalRouter` で受けていた所は、上の購読に替える
  - ゲームに MessagePipe の親のスコープがあれば、そこに `RegisterDialogueMessageBrokers(options)` を足す（「5. MessagePipe の登録」）
- [ ] **ゲームの asmdef**: VitalRouter の参照を外し、`MessagePipe`（購読・発行する所）と `MessagePipe.VContainer`（登録する所）を足す。Root の asmdef の名前が `amenone.gon.rootcontainer` から `amenone.dialogue.root` に変わったので、名前で参照していた所は替える（GUID は同じ。ライブラリの asmdef 同士は GUID で参照しているので直さなくてよい）
- [ ] **VitalRouter を外す（任意）**: ゲームのコードが VitalRouter を使っていなければ、`jp.hadashikick.vitalrouter.unity`（git パッケージ）と NuGet の `VitalRouter`（`packages.config` の行と `Assets/Packages/VitalRouter.<版>`）を外せる。2.0.0 のライブラリは VitalRouter を参照していない。`VitalRouter.R3` など VitalRouter に依る物を入れていれば一緒に外す。外した後に `Packages/packages-lock.json` を消して開き直すと、lock に残らない
- [ ] **ライブラリのフォルダの中のソースジェネレータの DLL**: 旧版は `UnitGenerator.dll` と `Amenonegames.AutoComponentProperty.dll` をライブラリのフォルダの中に置いていた事がある。パッケージはどちらも入れないので、入れた後も残る。AutoComponentProperty はそのまま使える（ほかの場所にも置いていれば 1 か所にまとめる。2 つ置いた時の挙動は未確認）。UnitGenerator は 2.0.0 が使わないので消してよい
- [ ] **作成メニュー**: `ScriptableObject` の作成メニューは「Amenone Dialogue/Create Option Dictionary」と「Amenone Dialogue/TextVisualizeTagPrefabIndex」。旧版で「gon/…」の場所にあった物はここに移った。作成済みのアセットはそのまま使える
- [ ] **SerializableDictionary Pro（Asset Store）**: 2.0.0 のライブラリは参照しないので、ほかに使っていなければ、辞書のアセットを書き換えた後に外せる。使い続けて、同じファイルで `Amenone.CSharpExtension` とそのアセットの名前空間の両方を `using` する場合、同じ名前の型を持っていると型名が衝突し得るので、どちらかを完全修飾する（例: `Amenone.CSharpExtension.SerializableDictionary<TKey, TValue>`）

### 辞書のアセットの書き換え


旧版の辞書は SerializableDictionary Pro の形（`m_keys` と `m_values` を別々に持つ）で保存されている。2.0.0 は `pairs` という組のリストで読むので、そのまま読み込ませると、例外にはならずに空の辞書になる。Unity はフィールドの名前が合わないデータを黙って捨て、そのアセットを次に保存した時にファイルからも消える。

1. 入れる前に、対象のアセットを控える（「1. 入れる前に」）。対象は、スクリプトの GUID `23f54df89d71456b8616a10a678bc9fe`（`TextVisualizePrefabIndex`）か `037fce639990408ba79957c5b635e9e9`（`OptionDictionaryAsset`）を `m_Script` に持つ `.asset`
2. `.unitypackage` を入れる。書き換えるまで、対象のアセットをインスペクタで触ったり保存したりしない
3. Unity を閉じて、各アセットの辞書の塊をテキストで書き換える。`m_keys` の n 番目のキーと `m_values` の n 番目の値を 1 つの組にし、`m_val`、`initialCapacity`、`m_keys`、`m_values` の 4 行（と `m_values` の要素）を消す。列挙のキー（`TextVisualizeTag`）は、`m_keys` に 4 バイトずつの 16 進（リトルエンディアン）で並んでいるので、8 文字ずつ数に直す（`04000000` は 4、`06000000` は 6）。`pairs` の `key` には列挙の数を書く
4. Unity を開き直し、インスペクタで組の数とキーを確かめる

書き換える前（キーが 4 と 6 の 2 組）:

```yaml
  dic:
    m_val: 7
    initialCapacity: 0
    m_keys: 0400000006000000
    m_values:
    - {fileID: 1000000000000000001, guid: 0123456789abcdef0123456789abcdef, type: 3}
    - {fileID: 1000000000000000002, guid: 0123456789abcdef0123456789abcdef, type: 3}
```

書き換えた後:

```yaml
  dic:
    pairs:
    - key: 4
      value: {fileID: 1000000000000000001, guid: 0123456789abcdef0123456789abcdef, type: 3}
    - key: 6
      value: {fileID: 1000000000000000002, guid: 0123456789abcdef0123456789abcdef, type: 3}
```

組が 1 つも無い辞書（`m_keys: ` と `m_values: []`）は `pairs: []` にする。`OptionDictionaryAsset`（`optionDictionary`、キーは文字列）も同じく `pairs` の `key` に文字列を書く。SerializableDictionary Pro が文字列のキーをどう保存していたかは未確認（その形のアセットを見ていない）

## 8. 既知の制限と未確認の事

- IL2CPP ビルドで MessagePipe のブローカーが生成できるか（AOT）は未確認
- ビルド（実機）での動作は未確認。Play モードでは、台本 1 本を終わりまで通す事（行送り、早送り、入力の切り替え、文字の演出、Play の停止と再開）を確かめた（「10. 確かめた構成」）
- 対話のスコープは、会話の途中でも、`NodeEnd` の購読の中からでも破棄してよい。破棄の時に実行中だった状態の処理は最後まで走り、順番を待っていた処理は実行されない（2.0.0 は、この時に `ObjectDisposedException` をログに出していた）
- 選択肢を出して選ぶ流れは、実際のゲームのプロジェクトの Play モードで、出て、選べて、選んだ先へ進む事を確かめた（2.0.0）。EditMode のテストは、選択肢のメッセージが届く所までを見ている
- 台本の関数（`<<if f() >= 2>>` のような読み出し）を登録する `IActionRegistration.AddFunction` は、`ActionRegistration` が要る `Yarn.Dialogue` がコンテナに登録されていないので、解決できない
- 話し手の名前（`LineViewContext.SpeakerName`）はいつも空。Yarn の `名前: 台詞` の書き方は、名前が台詞の中に混ざって出る
- 台詞の文を ID で引く所（`ILineProvider`）と文字の解釈（`LineTextParser`）は Presenter の実装の側（`amenone.dialogue.adapter`）にあり、状態機械を通さずに台詞を 1 行だけ出す使い方には、抽象に出す直しが要る
- 状態機械は「送りを待つ」前提で、自動送りは無い
- `DialogueHolder.Start` はノードを決めてから `StartDialogue` を発行し、自分では `Continue` を呼ばない。最初の行へ進めるのは、`StartDialogue` を受けて `VMRunning` に入った状態機械（`VMRunningHandler`）。Presenter の状態機械を組まずに Logic だけで `Start` を呼んでも台本は進まないので、その時は `IDialogueContinuable.Continue()` を自分で呼ぶ
- 選択肢を出している間（`WaitingForOptionSelection`）は命令が走らない
- 2 つの対話を同時に走らせる形は未確認
- 「3. 依存の一覧」の版より前の Unity・VContainer・MessagePipe での動作は未確認

## 9. 開発のしかた

- 正本は https://github.com/amenonegames/AmenoneExtension の `UnityPackageSources/AmenoneDialogue/`（とフォルダの `.meta` の `UnityPackageSources/AmenoneDialogue.meta`）。AmenoneExtension の Unity プロジェクトはライブラリの依存を持たないので、Unity が読まない `UnityPackageSources/` に置いている
- 編集は、依存のそろった Unity プロジェクトの `Assets/AmenoneDialogue/` に写して行い、直した物を `UnityPackageSources/AmenoneDialogue/` に戻す
- 新しいファイルの `.meta` は Unity に作らせる（手で書かない）。GUID を変えない
- `.unitypackage` の作り方（Python 3.10 以上の標準ライブラリだけ。Unity は要らない）:

  ```sh
  python Tools/build_unitypackage.py --source UnityPackageSources/AmenoneDialogue --root Assets/AmenoneDialogue --output Build/AmenoneDialogue-2.0.1.unitypackage
  ```

  - `.meta` の無いファイル、対応するファイルの無い `.meta`、GUID の重複があると、エラーで止まる
  - Unity と同じく、名前が `.` で始まる物と `~` で終わる物は対象から外す（外した物は標準エラーに出す）
  - 同じ Python と zlib なら、同じ入力から同じバイト列ができる。環境が違うと gzip の結果は変わり得るので、比べる時はスクリプトが出す `tar sha256` を見る
  - 改行コードは、リポジトリの `.gitattributes`（`UnityPackageSources/** -text`）で git の変換を止めている。どこで checkout しても同じバイトになる
  - `Build/` と `*.unitypackage` は git で無視している
- テストの回し方（依存のそろったプロジェクトで。`-quit` は付けない）:

  ```sh
  Unity -batchmode -nographics -projectPath <プロジェクト> -runTests -testPlatform EditMode \
    -assemblyNames "amenone.dialogue.logic.tests;amenone.dialogue.messaging.tests;amenone.dialogue.view.tests" -testResults results.xml -logFile test.log
  ```

  `com.amenone.csharpextension` のテスト（`amenone.csharpextension.tests`）も回す時は、`Packages/manifest.json` の `"testables"` に `com.amenone.csharpextension` を足す

- 版を上げる手順
  1. `package-version.txt`、`CHANGELOG.md`、この README の先頭の版を直す
  2. 上のスクリプトで `Build/AmenoneDialogue-<版>.unitypackage` を組み立てる
  3. タグ `dialogue/<版>` を打ち、GitHub の Releases に `.unitypackage` を添付する

## 10. 確かめた構成

2.0.0 で、次の組み合わせで確かめた（Play モードの確かめ以外はバッチモード）。2.0.1 は、同じ組み合わせで、コンパイルの `error CS` 0 と、EditMode テスト 43 件（Logic 11、通知 30、View 2）と csharpextension のテスト 11 件が全件通る事を確かめた。2.0.1 で足した 8 件のうち 4 件は、2.0.0 の `DialogueStateQueue` では落ちる。

- Unity 6000.7.0b1、VContainer 1.19.0、MessagePipe / MessagePipe.VContainer（コミット 3adc01d）、Yarn Spinner 3.2.8、com.unity.localization 2.0.0、com.unity.inputsystem 6.7.0、com.unity.ugui 2.7.0、com.unity.test-framework 1.9.0、AutoConstructor 5.6.0、FastEnum 1.8.0（NuGetForUnity は入れず、展開済みの `Assets/Packages/` を置いた）、com.amenone.csharpextension 0.1.5
- Scripting Define Symbols は `FORCE_YARNLOCALIZATION`
- 新規に入れる形: 依存だけを入れたプロジェクトに `-importPackage` で入れ、全ファイルが `Assets/AmenoneDialogue/` に `.meta` を含めてバイト単位で同じに展開された。コンパイルの `error CS` 0、EditMode テスト 35 件（Logic 11、通知 22、View 2）と csharpextension のテスト 11 件が全件通った
- 上書きで入れる形: 1.0.4 より後の未公開の版（VitalRouter 版、`Tests/` を持たない物）を `Assets/Scripts/` に置き、コンパイルが通る状態から入れた
  - そのまま入れると、根の直下に増えた物（`README.md`、`CHANGELOG.md`、`package-version.txt`、`Tests/`）が `Assets/AmenoneDialogue/` という別の木に置かれ、GUID の衝突のログが出た。コンパイルとテストは通った
  - 入れる前に、Unity を閉じた状態で `Assets/Scripts` を `.meta` ごと `Assets/AmenoneDialogue` に移すと、衝突は出ず、全部が 1 つの木に入った
  - そのまま入れた後に、Unity を閉じた状態で別の木の中身を `.meta` ごと `Assets/Scripts` の直下へ移し、空のフォルダとその `.meta` を消すと、開き直した時に衝突は出ず、全部が `Assets/Scripts` に入った
  - どの形でも `error CS` 0、EditMode テスト 44 件（ライブラリ 33、csharpextension 11）が全件通った。VitalRouter を外した後もコンパイルとテストが通る事は、辞書の型を替える前の組み立てで確かめた
- 実際のゲームのプロジェクトの複製に上書きで入れ、コンパイル、EditMode テスト、Play モードで台本 1 本を終わりまで（行送り、早送り、入力の切り替え、文字の演出、Play の停止と再開を 2 回）確かめた。選択肢を出して選ぶ流れは確かめていない
- 辞書のアセット: 旧版の形（SerializableDictionary Pro）の YAML をそのまま読ませると、例外にならずに空の辞書になった。「7. 旧版からの上げ方」の手順で書き換えた YAML は、組の数とキーの並びのとおりに読めた（値の参照先はプロジェクトに無い物で確かめた）
- `com.amenone.csharpextension` 0.1.5 は、Unity 2022.3.62f3 でも、このパッケージだけを入れたプロジェクトでコンパイルとテスト 11 件が通った
- 親のスコープが `RegisterMessagePipe()` だけを呼んだ時（VContainer 1.19.0）、`RegisterDialogueMessaging()` が例外を投げず、`IAsyncPublisher<NodeStart>` が解決できた
