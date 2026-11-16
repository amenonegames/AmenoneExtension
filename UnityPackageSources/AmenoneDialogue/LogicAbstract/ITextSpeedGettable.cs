namespace LogicAbstract
{
    public interface ITextSpeedHandlable : ITextSpeedGettable
    {
        void SetTextSpeed(float speed);
    }

    public interface ITextSpeedGettable
    {
        float GetTextSpeed();
    }
}