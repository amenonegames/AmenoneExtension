using Data.Utils;
using UnityEngine;

namespace ViewRoot.Utils
{
    public static class AnimationCurveExtensions
    {
        public static float Evaluate(this AnimationCurve curve, float time, float duration, float startValue,
            float endValue)
        {
            var curveTime = time / duration;
            var curveValue = curve.Evaluate(curveTime);
            var value = Mathf.Lerp(startValue, endValue, curveValue);
            return value;
        }

        public static Vector3 Evaluate(this AnimationCurve curve, float time, float duration, Vector3 startValue,
            Vector3 endValue)
        {
            var curveTime = time / duration;
            var curveValue = curve.Evaluate(curveTime);
            var value = startValue.Lerp(endValue, curveValue);
            return value;
        }

        public static Vector2 Evaluate(this AnimationCurve curve, float time, float duration, Vector2 startValue,
            Vector2 endValue)
        {
            var curveTime = time / duration;
            var curveValue = curve.Evaluate(curveTime);
            var value = startValue.Lerp(endValue, curveValue);
            return value;
        }

        public static Color Evaluate(this AnimationCurve curve, float time, float duration, Color startValue,
            Color endValue)
        {
            var curveTime = time / duration;
            var curveValue = curve.Evaluate(curveTime);
            var value = startValue.Lerp(endValue, curveValue);
            return value;
        }
    }
}