
using Identifier;

namespace LogicAbstract
{
    public readonly record struct DialogueEventMessage( DialogueEvent ev );
    public readonly record struct DialogueInterruptEventMessage( DialogueInterruptEvent interruptEvent );
    public readonly record struct DialogueLineEventMessage( DialogueEvent ev, DialogueLineContext Context );
    public readonly record struct DialogueOptionEventMessage(DialogueEvent ev, DialogueOptionContext Context);
    public readonly record struct DialogueSelectOptionEventMessage(DialogueEvent ev, SelectOptionContext Context);
    public readonly record struct DialogueCommandEventMessage(DialogueEvent ev, DialogueCommandContext Context);

    public readonly record struct InitializeDialogue;
    public readonly record struct NodeStart(NodeStartContext Context);
    public readonly record struct NodeEnd;

    public readonly record struct DialogueOptionContext(LogicOptionData[] OptionData) : IDialogueContext;

    public readonly record struct LogicOptionData(int Id, LogicLineData LineData, bool IsAvailable = true);
    public readonly record struct LogicLineData(string LineId,string[] Substitutions);

    public readonly record struct DialogueLineContext(LogicLineData LineData) : IDialogueContext;

    public readonly record struct SelectOptionContext(int SelectdIndex) : IDialogueContext;

    public readonly record struct DialogueCommandContext(string CommandText) : IDialogueContext;

    public readonly record struct NodeStartContext(string NodeName) : IDialogueContext;

    public interface IDialogueContext
    {
    }

}