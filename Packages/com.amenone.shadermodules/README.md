# Shader Modules

Unity のシェーダーから `#include` して使う、HLSL の関数の集まりです。ノイズ、イージング、uv の変換、2D のフラクタル、ゼリージュ（タイルの模様）、ブラーが入っています。

## 入っている物

すべて `Shaders/` の下にあります。各ファイルに include ガードがあるので、何度 include しても構いません。

| ファイル | 中身 |
|---|---|
| `noise.cginc` | 乱数（`hash22`、`hash23`、`random`）、値ノイズと fbm（`noise`、`fbm`）、simplex noise（`simplex_noise`、値と勾配を返す `simplex_noise_deriv`）と、それを重ねた `turbulence`・`ridge`。voronoi 系（`voronoi`、`cellular`、`voronoi_normalized`、`voronoi_blur` と、それぞれの `fbm_` 版）、`curl_noise`、`domain_warp` |
| `ease.cginc` | イージング。`ease_in_sine` から `ease_in_out_bounce` までの in / out / in_out と、冪乗で鋭さを変える `ease_in(t, power)`・`ease_out`・`ease_in_out`・`ease_out_in` |
| `transpose.cginc` | uv の変換（`uv_to_polar` / `polar_to_uv`、`scale`、`rotate`、`spiral`）。uv は [0,1]、中心は (0.5, 0.5) |
| `time.cginc` | `_Time` を使う時間の補助（`get_framed_time`、`oscillate_by_time`、`cycle_by_time`） |
| `fractal.cginc` | 2D のフラクタルと折りたたみ（`fold_kaleidoscope`、`fractal_kifs`、`fractal_sierpinski_triangle`、`fractal_koch_snowflake`、`fractal_box_fold`、`fractal_apollonian`、`fractal_kali` など）とらせん座標（`log_spiral_coord`、`golden_spiral_coord`）。そのまま色を返す `fractal_*_color` と推奨値の `fractal_*_default_style()`、無限に拡大し続ける `fractal_zoom_*_color` もある。`time.cginc` と `transpose.cginc` を include する。2.0.0 で `fractal_apollonian` の実装を替えた（下の「前の置き場所から移る時」） |
| `zellige.cginc` | ゼリージュ（8 芒星と Hankin 線のタイル）の `zellige_pattern`、`zellige_color`、`zellige_distance`。`fractal.cginc` を include する |
| `blur.cginc` | バイリニア補間に平均を取らせて、少ないサンプル数でぼかす関数。半分の解像度へのダウンサンプル（`blur_downsample_box4`、`blur_downsample_center5`）、対になるアップサンプル（`blur_upsample`）、分離ガウス（`blur_gaussian5`、`blur_gaussian9`）。下の「ブラー」 |

- `time.cginc`・`fractal.cginc`・`zellige.cginc` は `_Time` を使います。先に `UnityCG.cginc`（Built-in）か URP の `Core.hlsl` を include してください。`noise.cginc`・`ease.cginc`・`transpose.cginc`・`blur.cginc` は他に何も要らないので、compute shader でも使えます
- `fwidth` や `ddx` / `ddy` を使う関数は、フラグメントシェーダー専用です（関数の上のコメントに書いてあります）
- 関数名に接頭辞がありません。同じ名前（`noise`、`random`、`rotate`、`mod289` など）を持つ別のライブラリと一緒に include するとぶつかります

## 入れ方

Package Manager の「Add package from git URL」に次を入れます。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.shadermodules#shadermodules/2.1.0
```

### 前の置き場所から移る時

別のリポジトリの `https://github.com/amenonegames/ShaderModules.git?path=/Packages/Mods` で入れていた時は、`Packages/manifest.json` の URL を上の物に替えるだけで構いません。パッケージ名（`com.amenone.shadermodules`）も `.meta` の GUID も同じなので、`#include` のパスもアセットからの参照も切れません。

ただし 2.0.0 では、`fractal.cginc` の次の 2 点が 1.0.x と違います。どちらも、出どころを確かめられなかったための変更です。

- `fractal_glow_rings`、`fractal_glow_rings_color`（全オーバーロード）、`FRACTAL_GLOW_RINGS_*` の定数を外しました。使っていたシェーダーはコンパイルが止まります
- `fractal_apollonian` の実装を書き直しました。名前と引数、戻り値の .y（原点への最小距離の 2 乗）の意味は同じです。戻り値の .x は、単位円と座標軸までの距離の見積もりに変わりました（測る相手が前と違います）。絵も変わります。`fractal_apollonian_color` と推奨値（`fractal_apollonian_default_style()`）も合わせて変わりました

1.0.x ではサンプルがパッケージの中にありました。2.0.0 では `Samples~` に移したので、サンプルのマテリアルやシーンを使っていた時は、Package Manager の Samples から取り込み直してください。取り込んだ物は前と同じ GUID を持つので、参照はつながり直します。

## 使い方

```hlsl
#include "UnityCG.cginc" // URP なら "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.amenone.shadermodules/Shaders/noise.cginc"
#include "Packages/com.amenone.shadermodules/Shaders/fractal.cginc"

float4 frag(v2f_img i) : SV_Target
{
    float2 p = i.uv * 2.0 - 1.0;
    float n = simplex_noise(i.uv * 4.0) * 0.5 + 0.5;
    return float4(fractal_kifs_color(p) * n, 1.0);
}
```

`fractal_*_color` と `zellige_color` は、座標を [-1,1] で受けます。見た目を変える時は `*_default_style()` で推奨値を取り、欄を書き換えて渡します。

## ブラー（blur.cginc）

テクセルとテクセルの間を Bilinear のサンプラーで読むと、GPU が周りのテクセルを重み付きで平均した値を返します。`blur.cginc` の関数はこれを使い、1 回のサンプルで 2 つ（または 2×2）のテクセルを読みます。

| 関数 | サンプル数 | 中身 |
|---|---|---|
| `blur_downsample_box4` | 4 | 半分の解像度へ縮める。各サンプルがテクセルの角を読むので、4×4 テクセルの箱の平均になる |
| `blur_downsample_center5` | 5 | 半分の解像度へ縮める。中心 1/2 と斜め 4 回 1/8 ずつ。読む範囲は box4 と同じ 4×4 テクセルで、重みが中心に寄る（内側の 2×2 は 5/32、外側の 12 個は 1/32） |
| `blur_upsample` | 8 | 縮めた物を倍の解像度へ戻す。上下左右（`offset` テクセル先）を 1/12 ずつ、斜め（`offset/2` テクセル先）を 1/6 ずつ。`offset` を大きくすると広がる（1 が標準） |
| `blur_gaussian5` | 3 | 分離ガウスの 5 タップ（二項係数 1,4,6,4,1 / 16）。`direction` に (1,0) と (0,1) を渡して 2 パスかける |
| `blur_gaussian9` | 5 | 分離ガウスの 9 タップ（二項係数 1,8,28,56,70,56,28,8,1 / 256） |

どれも重みの合計は 1 で、`float4` を返します。引数はすべて `(Texture2D tex, SamplerState linearClampSampler, float2 uv, float2 texelSize, ...)` です。

- `linearClampSampler` には、フィルターが Bilinear、ラップが Clamp のサンプラーを渡します。Point だと 2 つのテクセルを平均しないので、ぼけ方が変わります
- `texelSize` は、**読む方**（入力）のテクセルサイズ（1/幅, 1/高さ）です。`uv` は書く方（出力）のピクセルの中心です
- ダウンサンプルは出力が入力の半分、アップサンプルは出力が入力の倍の解像度の時の重みです
- ガウスは、入力と出力が同じ解像度で、`uv` がテクセルの中心にある前提で重みを決めています。2 テクセルを 1 回にまとめる位置は (o1·w1 + o2·w2)/(w1+w2)、重みは w1+w2 です（例: 9 タップの 1 と 2 テクセル先は、位置 (1·56 + 2·28)/84 = 4/3、重み 84/256）
- ガウスの 2 テクセルの間の位置（4/3 など）は、GPU のバイリニアの小数部の精度に丸められます。素直に 9（5）テクセルを読んだ時との差は、試した GPU で最大 1e-3 ほどでした
- mip 0 を `SampleLevel` で読むので、フラグメントシェーダーの外（compute shader など）でも使えます

### サンプラーとテクセルサイズの渡し方

Built-in では、`sampler2D` ではなく `Texture2D` で宣言します。サンプラーは名前に `linear` と `clamp` を含めると、Unity がその設定のサンプラーを作ります（インラインサンプラー）。テクセルサイズは `_MainTex_TexelSize` に Unity が入れます。

```hlsl
#include "UnityCG.cginc"
#include "Packages/com.amenone.shadermodules/Shaders/blur.cginc"

Texture2D _MainTex;
SamplerState sampler_linear_clamp;
float4 _MainTex_TexelSize;
float4 _Direction;

float4 frag(v2f_img i) : SV_Target
{
    return blur_gaussian9(_MainTex, sampler_linear_clamp, i.uv, _MainTex_TexelSize.xy, _Direction.xy);
}
```

`Graphics.Blit` で回すシェーダーでは、`Properties` に `_MainTex("Source", 2D) = "black" {}` を書いてください。書かなかった時、Unity 6000.7.0b1 では Blit の入力が届かず、灰色の既定のテクスチャが読まれました。

URP（Unity 6 の Blitter / RenderGraph の Blit）では、入力は `_BlitTexture`、テクセルサイズは `_BlitTexture_TexelSize`、サンプラーは `Core.hlsl` が用意する `sampler_LinearClamp` です。

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.amenone.shadermodules/Shaders/blur.cginc"

float4 FragDownsample(Varyings input) : SV_Target
{
    return blur_downsample_center5(_BlitTexture, sampler_LinearClamp, input.texcoord, _BlitTexture_TexelSize.xy);
}
```

XR の single-pass instanced では `_BlitTexture` が `Texture2DArray` になるので、`Texture2D` を受けるこの関数には渡せません。

### 広くぼかすには、パスを重ねる

1 パスの中のサンプルの節約（9 タップを 5 回で読む、など）はシェーダーだけで済みます。けれども 1 パスで届くのは数テクセル先までです。画面の何分の一にも及ぶ広いぼかしは、半分ずつ縮めるパスを何段か重ね、同じ段数だけ拡大するパスで戻して作ります。縮めた先の 1 テクセルは元の何テクセルにも当たるので、少ないサンプルで遠くまで届きます。

パスを重ねる事（縮めた途中のテクスチャを用意し、順に描き、最後に書き戻す事）は、シェーダーの外の仕事です。URP なら Renderer Feature、Built-in なら `CommandBuffer` や `Graphics.Blit` を並べるスクリプトが受け持ちます。その例が下の Blur Sample です。

### 色空間とアルファの注意

- 関数は、読んだ値をそのまま重み付きで足します。Linear の色空間なら、sRGB のテクスチャは読む時に線形へ直されるので、線形の値で平均されます。Gamma の色空間では、ガンマのかかった値のまま平均するので、明るい色と暗い色の境目が、線形で平均した時より暗めになります
- 4 つのチャンネルを同じ重みで足します。アルファが premultiplied でない（straight alpha の）テクスチャでは、透明なテクセルの RGB（黒などの、見えていない色）が縁ににじみ、暗い縁が出ます。premultiplied の形にしてからぼかし、要るなら後でアルファで割ってください
- Clamp で読むので、画面の端では端のテクセルが繰り返されます。端に触れている模様では、明るさの合計が端の分だけ変わります
- 重みの合計は 1 なので、全体の明るさは保たれます。HDR の、極端に明るい 1 ピクセルは、縮めた先でも強く残ります（明るい所だけを取り出す処理などは入れていません）

## サンプル

Package Manager でこのパッケージを選び、Samples から取り込みます。

| サンプル | 中身 | 要る物 |
|---|---|---|
| Shader Sample | マテリアルのトグルで、各ノイズ・フラクタル・ゼリージュを切り替えて見る unlit シェーダーとシーン | URP（`Core.hlsl` を include する） |
| Compute Particle Sample | `noise.cginc` の `random` と `curl_noise` を使う compute shader で動かすパーティクル（`SimpleMove`、`NoisedMove` の 2 シーン）。`Graphics.RenderMeshIndirect` で描く | URP（`Particle.shader` が `Core.hlsl` を include する）、compute shader が動くプラットフォーム |
| Compute Shader Sample | テクスチャをブロックに分けてアルファの平均を出し、RawImage に表示する compute shader。このパッケージの include は使わない | uGUI、compute shader が動くプラットフォーム。シーンのカメラに URP の追加データが付いているので、URP の無いプロジェクトでは Missing Script が出る |
| Blur Sample | カメラの色を `blur_downsample_center5` で指定の段数だけ半分ずつ縮め、`blur_upsample` で戻して書き戻す、全画面のブラーの Renderer Feature（RenderGraph）と、そのシェーダー。シーンは無い | Unity 6 と URP。RenderGraph の API を使うので Unity 2022.3 では動かない。XR の single-pass instanced には対応していない（`_BlitTexture` が `Texture2DArray` になるため） |

- サンプルの C# スクリプトに asmdef はありません。取り込むと `Assembly-CSharp` にコンパイルされます（名前空間は `Sandbox`）
- シーンは Unity 6 で保存した物です

### Blur Sample の使い方

1. Package Manager でこのパッケージを選び、Samples から Blur Sample を取り込む
2. 使っている URP の Renderer（URP Asset の Renderer List にある Universal Renderer Data）を選び、Inspector の Add Renderer Feature から Blur Renderer Feature を足す
3. Shader の欄に `BlurSample.shader`（`Hidden/amenone_module/blur_sample`）が入っている事を確かめる。スクリプトの既定の参照にしてあるので入るはずだが、Inspector から足した時は確かめていない。入らなければ、取り込んだ `BlurSample.shader` を手で入れる。空のままだと、Feature を作った時に警告を 1 回出して何もしない
4. 欄を決める

| 欄 | 中身 |
|---|---|
| Iterations | 縮める段数（1〜6）。段数が 1 増えるごとに、いちばん小さく縮めた先がさらに半分になり、ぼけが広がる |
| Upsample Offset | `blur_upsample` に渡すずらし量。1 が標準。大きくすると、同じ段数でも広がる |
| Pass Event | 差し込む位置。既定は `BeforeRenderingPostProcessing`。`AfterRendering` では、パスが中間テクスチャを求めていても、その時点のカメラの色はバックバッファ（最終の書き先）になっていて読めないので、何もしない（ログも出さない） |

縮めた途中のテクスチャは RenderGraph の一時テクスチャで、最後のアップサンプルがカメラの色（`activeColorTexture`）へ直接書き戻します。

- 掛けるのは、カメラの種類が Game と SceneView の時だけです。Preview（Inspector のプレビューなど）と Reflection のカメラには掛けません
- 縮めた先の大きさは、カメラの色のテクスチャの幅と高さを 2 で割って切り捨てた物です。幅や高さが奇数の段では、縮めた先が 2 対 1 にならないので、ぼけの中心が 1 テクセルの数分の一ずれます

## ライセンス

MIT です。[LICENSE.md](LICENSE.md) を見てください。

`noise.cginc` の `simplex_noise` は [webgl-noise](https://github.com/ashima/webgl-noise)（Ashima Arts、MIT）を HLSL へ移して手を入れた物で、`simplex_noise_deriv` はそれを元にしています。再配布する時は、LICENSE.md の Third-Party Notices も含めてください。

## 確かめた事と確かめていない事

確かめた事（2.1.0 の `blur.cginc` と Blur Sample）:

- Unity 6000.7.0b1 + URP 17.7.0（Direct3D 12）: Blur Sample を取り込んだ形にして、シェーダーがエラーも警告も無く取り込まれ、C# がコンパイルされる事。`blur.cginc` の 5 つの関数を呼ぶ compute shader がエラー無くコンパイルされる事
- Unity 2022.3.62f3（Built-in、Direct3D 11）: `blur.cginc` を include する Built-in の頂点・フラグメントシェーダーと compute shader が、エラー無しで取り込まれる事
- 数値（6000.7.0b1 と 2022.3.62f3 の両方で同じ結果）: 32×32 の float のテクスチャ（1 テクセルの点、縦の縞、乱数）を各関数に通し、float の RenderTexture から読み出して比べた
  - 1 テクセルの点の明るさの合計が、どの関数でも 1.000000 のまま（解像度の比を掛けて比べた）
  - `blur_downsample_box4` が CPU で計算した 4×4 の箱の平均と、`blur_downsample_center5` が上の重みの CPU 計算と、`blur_upsample`（`offset` 1 と 2）が CPU で計算したバイリニアの 8 タップと、最大誤差 1.2e-7 で一致する
  - `blur_gaussian9`（5 回）と、同じ重みで 9 テクセルを `Load` した参照との最大誤差が 8.3e-4、`blur_gaussian5`（3 回）と 5 テクセルの参照との最大誤差が 4.4e-4。点を通した時の重みは 0.984, 8.016, 27.891, 56.109, 70, …（/256。正しくは 1, 8, 28, 56, 70）。2 テクセルの間の位置 4/3 や 28/9 が、GPU のバイリニアの小数部の精度（試した GPU では 1/256 刻み）に丸められるための差
- Blur Sample の Renderer Feature: Unity 6000.7.0b1 のバッチモードで、この Feature だけを足した Renderer のカメラで、色の違う板を 5 枚描いた（320×180、Gamma の色空間、MSAA なし）。隣り合うピクセルの差の 2 乗の平均が、Feature なしの 0.0192 から、Iterations 1 で 0.0030、4 で 0.00012、6 で 0.000005 に減り、画面全体の明るさの平均（0.360）はほぼ変わらない
  - Iterations 4 で Upsample Offset を変えると結果が変わる。差の 2 乗の平均は 0.5 で 0.00022、1 で 0.00012、2 で 0.00004（大きいほど広がる）
  - Pass Event が `AfterRendering` の時は、カメラの色がバックバッファになっていて、ぼけない（Feature なしと同じ画像になる）
  - カメラの種類を Preview と Reflection にすると掛からず（Feature なしと同じ画像）、Game では掛かる
  - カメラの色のテクスチャの大きさ（`GetTextureDesc` の幅と高さ）は、320×180 でも 321×181 でも、Render Scale 0.5（160×90）でも、カメラの書き先の大きさと一致した

確かめた事（2.0.0）:

- Unity 6000.7.0b1 + URP 17.7.0: 3 つのサンプルを取り込んだ形にして、シェーダーがエラー無しで取り込まれる事（警告は `pow` に負の値が入り得るという物が 2 件。`noise.cginc` の `voronoi_blur` と、Sample.shader の caustic の所）。サンプルの C# がコンパイルされ、4 つのシーンを開いて Missing Script が無く、スクリプトの欄（compute shader、マテリアル、テクスチャなど）がつながっている事
- Unity 6000.7.0b1 と 2022.3.62f3（Built-in）: 6 本の cginc をすべて include して関数を呼ぶ小さなシェーダーと、上の「使い方」のシェーダーが、エラー無しで取り込まれる事
- サンプルと検証用の compute shader の全カーネルが、FXC（`cs_5_0`）でエラー無しでコンパイルできる事
- 書き直した `fractal_apollonian`: Unity 6000.7.0b1 で 512×512 に描き、NaN と無限大が無い事（反復 8 回と 16 回、`strength` が 0 と負の時も）と、円の詰まった模様として見える事

確かめていない事:

- 実機・Direct3D 11 / 12 以外のグラフィックス API（Vulkan、Metal、モバイルなど）でのコンパイルと見た目
- Blur Sample: Editor の Game / Scene ビューでの見た目（SceneView のカメラに掛かる事も含む）、Inspector から Feature を足した時に Shader の欄が入るか、XR、MSAA、動的解像度、カメラのスタック、Linear の色空間での見た目、2 つ以上のカメラ
- サンプルを Play して、パーティクルや表示が意図どおりに動くか
- Unity 2022.3 でのサンプル（シーンは Unity 6 で保存した物）
- `package.json` の `"unity": "2022.3"` は宣言です。2022.3 で確かめたのは上の 2022.3.62f3 の事だけです
