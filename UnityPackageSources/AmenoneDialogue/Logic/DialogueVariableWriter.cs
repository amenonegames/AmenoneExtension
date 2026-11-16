using LogicAbstract;
using Yarn;

namespace Logic
{
    [AutoConstructor]
    public sealed partial class DialogueVariableWriter : IDialogueVariableWriter
    {
        private readonly IVariableStorage _variableStorage;

        public void Write(string variableName, float value) => _variableStorage.SetValue(variableName, value);

        public void Write(string variableName, bool value) => _variableStorage.SetValue(variableName, value);

        public void Write(string variableName, string value) => _variableStorage.SetValue(variableName, value);
    }
}
