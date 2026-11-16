namespace LogicAbstract
{
    public interface IDialogueVariableWriter
    {
        void Write(string variableName, float value);
        void Write(string variableName, bool value);
        void Write(string variableName, string value);
    }
}
