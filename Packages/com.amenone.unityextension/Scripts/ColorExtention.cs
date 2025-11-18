using UnityEngine;

namespace Data.Utils
{
    public static class ColorExtention
    {
        /// <summary>
        ///     カラーを完全に透明にする拡張メソッド。もともとのAlpha値は保持しないため、
        ///     後ほど元のAlpha値に戻したい場合は別途保存が必要
        /// </summary>
        /// <param name="color"></param>
        public static void SetTransparent(ref this Color color)
        {
            color += new Color(0f, 0f, 0f, -255f);
        }

        public static bool ApproximatelyMatch(this Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r)
                   && Mathf.Approximately(a.g, b.g)
                   && Mathf.Approximately(a.b, b.b)
                   && Mathf.Approximately(a.a, b.a);
        }

        public static Color Lerp(this Color a, Color b, float t)
        {
            return new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t), Mathf.Lerp(a.b, b.b, t),
                Mathf.Lerp(a.a, b.a, t));
        }
    }
}