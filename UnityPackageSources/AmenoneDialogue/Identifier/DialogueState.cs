namespace Identifier
{
    public enum DialogueState
    {
        VMRunning,
        VisualizingLine,
        WaitingForTextFeed,
        TextFeedHandling,
        VisualizingOptions,
        WaitingForOptionSelection,
        SelectedOption,
        CommandRunning,
        CommandComplete,
        DialogueStop
    }

    public enum DialogueEvent
    {
        StartDialogue,
        LineVisualizeRequested,
        LineVisualizeFinish,
        TextFeedProvided,
        TextFeedHandleFinish,
        OptionsVisualizeRequested,
        OptionsVisualizeFinish,
        OptionSelected,
        CommandRunRequested,
        CommandExecuteFinish,
        ContinueRequested,
        StopDialogue
    }

    public enum DialogueInterruptEvent
    {
        FastForwardRequested
    }
}