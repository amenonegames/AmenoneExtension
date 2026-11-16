using LogicAbstract;

namespace Logic
{
    public class TextSpeedHandler : ITextSpeedHandlable
    {
        private const float DefaultTextSpeed = 1f;
        private float Speed { get; set; } = DefaultTextSpeed;

        public float GetTextSpeed() => Speed;

        public void SetTextSpeed(float speed) => Speed = speed;
    }
}