# AmenoneExtension

Unity 向けの自作パッケージ集です。使う物だけを選んで入れます。

UPM のパッケージは、Package Manager の「Add package from git URL...」に URL を入れます。版を固定する時は、URL の末尾に `#<タグかコミット>` を付けます。

## UPM のパッケージ

### com.amenone.csharpextension

C# の拡張メソッド集です。配列・リスト・LINQ・enum・int の拡張と、Inspector で編集できる `SerializableDictionary<TKey, TValue>`、`PooledArray` が入っています。依存はありません。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.csharpextension
```

### com.amenone.unityextension

Unity の型の拡張メソッド集です。GameObject、MonoBehaviour、Vector、Color、AnimationCurve、TextMeshPro の拡張が入っています。TextMeshPro が要ります。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.unityextension
```

### com.amenone.vcontainerextensions

VContainer の登録を助ける拡張です。コンポーネントの配列から目当ての View を探して登録する拡張メソッドと、名前で View を引く一覧（Lookup）の基底クラスが入っています。VContainer が要ります。0.2.0 から、NullObject のジェネレーターは同梱していません。使う時は `com.amenone.nullobjectgenerator` を別に入れます。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.vcontainerextensions
```

### com.amenone.nullobjectgenerator

interface やクラスに属性を書くと、何もしない実装（Null Object）を生成する Source Generator です。生成先は属性を書いた型と同じアセンブリで、asmdef の参照は増えません。Unity 6 以降が対象です。依存はありません。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.nullobjectgenerator#nullobjectgenerator/2.0.0
```

### com.amenone.shadermodules

シェーダーから include して使う関数集（cginc）です。ノイズ、フラクタル、イージング、座標の変換、時間、ゼリージュ模様が入っています。依存はありません。サンプルは URP が要ります。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.shadermodules#shadermodules/2.0.0
```

### com.amenone.ecsbridge

VContainer + MessagePipe の MVP の構成と、Unity Entities（ECS）をつなぐ部品です。作り直される既定の World に追従する読み書きの口と、値が変わった時だけ発行する System の基底クラス、前回値の部品が入っています。Entities と VContainer が要ります。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.ecsbridge#ecsbridge/0.3.1
```

### com.amenone.architectureguard

文章で決めている設計の決まりを、Roslyn Analyzer の警告にするパッケージです。時間の取り方、Unity のメッセージ、ECS の query の作り方、MessagePipe の購読と発行の場所を見ます。想定している構成を AI のコーディングエージェントに伝える指示の文書も入っています。asmdef で参照したアセンブリと、それを参照するアセンブリが対象です。ブランチ `architectureguard` にあり、タグで固定して使います。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.architectureguard#architectureguard/0.3.2
```

## .unitypackage で配る物

### Amenone Dialogue System

Yarn Spinner の台本を、VContainer + MessagePipe の構成で動かす対話ライブラリです。行の表示、選択肢、命令、文字の演出を持ちます。ゲームの基盤として `Assets/AmenoneDialogue/` に展開して使います。Releases から `AmenoneDialogue-<版>.unitypackage` を取ります。依存と入れ方は [README](UnityPackageSources/AmenoneDialogue/README.md) にあります。

```
https://github.com/amenonegames/AmenoneExtension/releases
```
