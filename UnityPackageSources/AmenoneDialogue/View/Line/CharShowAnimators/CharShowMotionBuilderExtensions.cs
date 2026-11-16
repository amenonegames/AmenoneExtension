using LitMotion;

namespace View.Line
{
    internal static class CharShowMotionBuilderExtensions
    {
        // BindToTMPChar* に渡した初期値は、同じ TMP_Text への次の Bind で TMPMotionAnimator.Get が Reset するため失われる。
        // 待機中も開始値を書き続けさせ、入場前の文字が既定値（不透明の白）で描かれるのを防ぐ。
        public static MotionBuilder<TValue, TOptions, TAdapter> WithDelayHoldingStartValue<TValue, TOptions, TAdapter>(
            this MotionBuilder<TValue, TOptions, TAdapter> builder, float delay)
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
            where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
        {
            return builder.WithDelay(delay, skipValuesDuringDelay: false);
        }
    }
}
