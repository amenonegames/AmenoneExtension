using System;
using UnityEngine;

namespace View.Line
{
    [Serializable]
    public class StickyPullSettings
    {
        [SerializeField] private float attachDistance = 70f;
        [SerializeField] private float detachDistance = 240f;
        [SerializeField, Range(0f, 1f)] private float pullStrength = 0.95f;
        [SerializeField, Range(0f, 0.5f)] private float anchorInset = 0.2f;
        [SerializeField] private float springStiffness = 200f;
        [SerializeField] private float springDamping = 10f;
        [SerializeField] private float maxEntranceStretch = 420f;

        public float AttachDistance => attachDistance;
        public float DetachDistance => detachDistance;
        public float PullStrength => pullStrength;
        public float AnchorInset => anchorInset;
        public float SpringStiffness => springStiffness;
        public float SpringDamping => springDamping;
        public float MaxEntranceStretch => maxEntranceStretch;
    }
}
