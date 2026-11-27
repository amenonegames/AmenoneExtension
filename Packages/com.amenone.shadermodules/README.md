# Shader Modules

Unity のシェーダーから `#include` して使う、HLSL の関数の集まりです。ノイズ、イージング、uv の変換、2D のフラクタル、ゼリージュ（タイルの模様）が入っています。

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

- `time.cginc`・`fractal.cginc`・`zellige.cginc` は `_Time` を使います。先に `UnityCG.cginc`（Built-in）か URP の `Core.hlsl` を include してください。`noise.cginc`・`ease.cginc`・`transpose.cginc` は他に何も要らないので、compute shader でも使えます
- `fwidth` や `ddx` / `ddy` を使う関数は、フラグメントシェーダー専用です（関数の上のコメントに書いてあります）
- 関数名に接頭辞がありません。同じ名前（`noise`、`random`、`rotate`、`mod289` など）を持つ別のライブラリと一緒に include するとぶつかります

## 入れ方

Package Manager の「Add package from git URL」に次を入れます。

```
https://github.com/amenonegames/AmenoneExtension.git?path=/Packages/com.amenone.shadermodules#shadermodules/2.0.0
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

## サンプル

Package Manager でこのパッケージを選び、Samples から取り込みます。

| サンプル | 中身 | 要る物 |
|---|---|---|
| Shader Sample | マテリアルのトグルで、各ノイズ・フラクタル・ゼリージュを切り替えて見る unlit シェーダーとシーン | URP（`Core.hlsl` を include する） |
| Compute Particle Sample | `noise.cginc` の `random` と `curl_noise` を使う compute shader で動かすパーティクル（`SimpleMove`、`NoisedMove` の 2 シーン）。`Graphics.RenderMeshIndirect` で描く | URP（`Particle.shader` が `Core.hlsl` を include する）、compute shader が動くプラットフォーム |
| Compute Shader Sample | テクスチャをブロックに分けてアルファの平均を出し、RawImage に表示する compute shader。このパッケージの include は使わない | uGUI、compute shader が動くプラットフォーム。シーンのカメラに URP の追加データが付いているので、URP の無いプロジェクトでは Missing Script が出る |

- サンプルの C# スクリプトに asmdef はありません。取り込むと `Assembly-CSharp` にコンパイルされます（名前空間は `Sandbox`）
- シーンは Unity 6 で保存した物です

## ライセンス

MIT です。[LICENSE.md](LICENSE.md) を見てください。

`noise.cginc` の `simplex_noise` は [webgl-noise](https://github.com/ashima/webgl-noise)（Ashima Arts、MIT）を HLSL へ移して手を入れた物で、`simplex_noise_deriv` はそれを元にしています。再配布する時は、LICENSE.md の Third-Party Notices も含めてください。

## 確かめた事と確かめていない事

確かめた事（2.0.0）:

- Unity 6000.7.0b1 + URP 17.7.0: 3 つのサンプルを取り込んだ形にして、シェーダーがエラー無しで取り込まれる事（警告は `pow` に負の値が入り得るという物が 2 件。`noise.cginc` の `voronoi_blur` と、Sample.shader の caustic の所）。サンプルの C# がコンパイルされ、4 つのシーンを開いて Missing Script が無く、スクリプトの欄（compute shader、マテリアル、テクスチャなど）がつながっている事
- Unity 6000.7.0b1 と 2022.3.62f3（Built-in）: 6 本の cginc をすべて include して関数を呼ぶ小さなシェーダーと、上の「使い方」のシェーダーが、エラー無しで取り込まれる事
- サンプルと検証用の compute shader の全カーネルが、FXC（`cs_5_0`）でエラー無しでコンパイルできる事
- 書き直した `fractal_apollonian`: Unity 6000.7.0b1 で 512×512 に描き、NaN と無限大が無い事（反復 8 回と 16 回、`strength` が 0 と負の時も）と、円の詰まった模様として見える事

確かめていない事:

- 実機・Direct3D 11 以外のグラフィックス API（Vulkan、Metal、モバイルなど）でのコンパイルと見た目
- サンプルを Play して、パーティクルや表示が意図どおりに動くか
- Unity 2022.3 でのサンプル（シーンは Unity 6 で保存した物）
- `package.json` の `"unity": "2022.3"` は宣言です。2022.3 で確かめたのは上の 2022.3.62f3 の事だけです
