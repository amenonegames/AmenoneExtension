using UnityEngine;

namespace Data.Utils
{
    public static class VectorExtension
    {
        /// <summary>
        ///     不動小数点の誤差を考慮してVector3を比較する拡張メソッド
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool ApproximatelyMatch(this Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        public static bool ApproximatelyMatch(this Vector4 a, Vector4 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z) &&
                   Mathf.Approximately(a.w, b.w);
        }

        public static bool ApproximatelyMatch(this Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }

        public static bool GraterThan(this Vector3 a, Vector3 b)
        {
            return a.x > b.x && a.y > b.y && a.z > b.z;
        }

        public static bool GraterThan(this Vector2 a, Vector2 b)
        {
            return a.x > b.x && a.y > b.y;
        }

        public static Vector3 Lerp(this Vector3 a, Vector3 b, float t)
        {
            return new Vector3(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t));
        }

        public static Vector2 Lerp(this Vector2 a, Vector2 b, float t)
        {
            return new Vector2(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t));
        }

        public static bool IsRight(Vector2 v , float threshold)
        {
            return v.x > threshold;
        }

        public static bool IsLeft(Vector2 v , float threshold)
        {
            return v.x < -threshold;
        }

        public static bool IsUp(Vector2 v , float threshold)
        {
            return v.y > threshold;
        }

        public static bool IsDown(Vector2 v , float threshold)
        {
            return v.y < -threshold;
        }
    }
}