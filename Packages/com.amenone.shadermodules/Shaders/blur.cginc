#ifndef AMENONE_BLUR_INCLUEDED
#define AMENONE_BLUR_INCLUEDED

// 二項係数 1,4,6,4,1 (/16) から。隣り合う 2 テクセルを 1 回にまとめる: 位置 = (o1·w1 + o2·w2)/(w1+w2)、重み = w1+w2
static const float BLUR_GAUSSIAN5_WEIGHT0 = 6.0 / 16.0;
static const float BLUR_GAUSSIAN5_OFFSET1 = (1.0 * 4.0 + 2.0 * 1.0) / (4.0 + 1.0);
static const float BLUR_GAUSSIAN5_WEIGHT1 = (4.0 + 1.0) / 16.0;

// 二項係数 1,8,28,56,70,56,28,8,1 (/256) から。まとめ方は 5 タップと同じ
static const float BLUR_GAUSSIAN9_WEIGHT0 = 70.0 / 256.0;
static const float BLUR_GAUSSIAN9_OFFSET1 = (1.0 * 56.0 + 2.0 * 28.0) / (56.0 + 28.0);
static const float BLUR_GAUSSIAN9_WEIGHT1 = (56.0 + 28.0) / 256.0;
static const float BLUR_GAUSSIAN9_OFFSET2 = (3.0 * 8.0 + 4.0 * 1.0) / (8.0 + 1.0);
static const float BLUR_GAUSSIAN9_WEIGHT2 = (8.0 + 1.0) / 256.0;

// in: Texture2D, SamplerState（Bilinear・Clamp）, float2 uv  out: float4 mip 0 を読んだ色（微分を使わないので compute でも使える）
float4 blur_sample(Texture2D tex, SamplerState linearClampSampler, float2 uv)
{
    return tex.SampleLevel(linearClampSampler, uv, 0);
}

// in: Texture2D, SamplerState（Bilinear・Clamp）, float2 出力のuv, float2 入力のテクセルサイズ  out: float4 4 回のサンプルで読んだ 4x4 テクセルの箱の平均
// 半分の解像度へ縮める時に使う。各サンプルはテクセルの角を読み、周りの 2x2 をバイリニアに平均させる
float4 blur_downsample_box4(Texture2D tex, SamplerState linearClampSampler, float2 uv, float2 texelSize)
{
    float4 color = blur_sample(tex, linearClampSampler, uv + float2(-1.0, -1.0) * texelSize);
    color += blur_sample(tex, linearClampSampler, uv + float2(1.0, -1.0) * texelSize);
    color += blur_sample(tex, linearClampSampler, uv + float2(-1.0, 1.0) * texelSize);
    color += blur_sample(tex, linearClampSampler, uv + float2(1.0, 1.0) * texelSize);
    return color * 0.25;
}

// in: Texture2D, SamplerState（Bilinear・Clamp）, float2 出力のuv, float2 入力のテクセルサイズ  out: float4 中心 1/2 と斜め 4 回 1/8 ずつで縮めた色
// 斜めは出力の半テクセル（入力の 1 テクセル）ずらす。4x4 テクセルを、内側の 2x2 が 5/32、外側の 12 個が 1/32 の重みで読む
float4 blur_downsample_center5(Texture2D tex, SamplerState linearClampSampler, float2 uv, float2 texelSize)
{
    float4 center = blur_sample(tex, linearClampSampler, uv);
    return center * 0.5 + blur_downsample_box4(tex, linearClampSampler, uv, texelSize) * 0.5;
}

// in: Texture2D, SamplerState（Bilinear・Clamp）, float2 出力のuv, float2 入力のテクセルサイズ, float ずらし量（1 が標準）  out: float4 周囲 8 方向を足して拡大した色
// 上下左右は offset テクセル先を 1/12 ずつ、斜めは (offset/2, offset/2) テクセル先を、近いので倍の 1/6 ずつ足す
float4 blur_upsample(Texture2D tex, SamplerState linearClampSampler, float2 uv, float2 texelSize, float offset)
{
    float2 axis = texelSize * offset;
    float2 diagonal = axis * 0.5;
    float4 color = blur_sample(tex, linearClampSampler, uv + float2(-axis.x, 0.0));
    color += blur_sample(tex, linearClampSampler, uv + float2(axis.x, 0.0));
    color += blur_sample(tex, linearClampSampler, uv + float2(0.0, -axis.y));
    color += blur_sample(tex, linearClampSampler, uv + float2(0.0, axis.y));
    float4 diagonalColor = blur_sample(tex, linearClampSampler, uv + float2(-diagonal.x, -diagonal.y));
    diagonalColor += blur_sample(tex, linearClampSampler, uv + float2(diagonal.x, -diagonal.y));
    diagonalColor += blur_sample(tex, linearClampSampler, uv + float2(-diagonal.x, diagonal.y));
    diagonalColor += blur_sample(tex, linearClampSampler, uv + float2(diagonal.x, diagonal.y));
    return (color + diagonalColor * 2.0) / 12.0;
}

// in: Texture2D, SamplerState（Bilinear・Clamp）, float2 テクセルの中心のuv, float2 テクセルサイズ, float2 方向 (1,0) か (0,1)  out: float4 3 回のサンプルで 5 タップのガウスをかけた色
// 入力と出力が同じ解像度で、uv がテクセルの中心にある前提
float4 blur_gaussian5(Texture2D tex, SamplerState linearClampSampler, float2 uv, float2 texelSize, float2 direction)
{
    float2 step1 = direction * texelSize * BLUR_GAUSSIAN5_OFFSET1;
    float4 color = blur_sample(tex, linearClampSampler, uv) * BLUR_GAUSSIAN5_WEIGHT0;
    color += (blur_sample(tex, linearClampSampler, uv - step1) + blur_sample(tex, linearClampSampler, uv + step1)) * BLUR_GAUSSIAN5_WEIGHT1;
    return color;
}

// in: Texture2D, SamplerState（Bilinear・Clamp）, float2 テクセルの中心のuv, float2 テクセルサイズ, float2 方向 (1,0) か (0,1)  out: float4 5 回のサンプルで 9 タップのガウスをかけた色
// 入力と出力が同じ解像度で、uv がテクセルの中心にある前提
float4 blur_gaussian9(Texture2D tex, SamplerState linearClampSampler, float2 uv, float2 texelSize, float2 direction)
{
    float2 step1 = direction * texelSize * BLUR_GAUSSIAN9_OFFSET1;
    float2 step2 = direction * texelSize * BLUR_GAUSSIAN9_OFFSET2;
    float4 color = blur_sample(tex, linearClampSampler, uv) * BLUR_GAUSSIAN9_WEIGHT0;
    color += (blur_sample(tex, linearClampSampler, uv - step1) + blur_sample(tex, linearClampSampler, uv + step1)) * BLUR_GAUSSIAN9_WEIGHT1;
    color += (blur_sample(tex, linearClampSampler, uv - step2) + blur_sample(tex, linearClampSampler, uv + step2)) * BLUR_GAUSSIAN9_WEIGHT2;
    return color;
}
#endif