using NullObjectGenerator;

namespace ViewAbstract
{
    [InterfaceToNullObj]
    public interface ITextFeedButtonActivatable
    {
        void Activate();
        void Deactivate();
    }
    [InterfaceToNullObj]
    public interface ITextFastForwardButtonActivatable
    {
        void Activate();
        void Deactivate();
    }
}