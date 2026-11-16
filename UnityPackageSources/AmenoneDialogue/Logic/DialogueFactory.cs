using Identifier;
using LogicAbstract;
using MessagePipe;
using Yarn;
using Yarn.Unity;

namespace Logic
{
    public class DialogueFactory : IDialogueFactory
    {
        private readonly IAsyncPublisher<DialogueLineEventMessage> _linePublisher;
        private readonly IAsyncPublisher<DialogueOptionEventMessage> _optionPublisher;
        private readonly IAsyncPublisher<DialogueCommandEventMessage> _commandEventPublisher;
        private readonly IAsyncPublisher<NodeStart> _nodeStartPublisher;
        private readonly IAsyncPublisher<NodeEnd> _nodeEndPublisher;
        private readonly IVariableStorage _variableStorage;
        private readonly YarnProject _yarnProject;

        public DialogueFactory(
            IAsyncPublisher<DialogueLineEventMessage> linePublisher,
            IAsyncPublisher<DialogueOptionEventMessage> optionPublisher,
            IAsyncPublisher<DialogueCommandEventMessage> commandEventPublisher,
            IAsyncPublisher<NodeStart> nodeStartPublisher,
            IAsyncPublisher<NodeEnd> nodeEndPublisher,
            IVariableStorage variableStorage,YarnProject yarnProject)
        {
            _linePublisher = linePublisher;
            _optionPublisher = optionPublisher;
            _commandEventPublisher = commandEventPublisher;
            _nodeStartPublisher = nodeStartPublisher;
            _nodeEndPublisher = nodeEndPublisher;
            _variableStorage = variableStorage;
            _yarnProject = yarnProject;
        }
        public Dialogue Create()
        {
            var dialogue = new Dialogue(_variableStorage)
            {
                LineHandler = (line) => _linePublisher.Publish(new DialogueLineEventMessage(
                    DialogueEvent.LineVisualizeRequested,
                    new DialogueLineContext(new LogicLineData(line.ID, line.Substitutions)))),

                OptionsHandler = (options) =>
                {
                    var logicOptions = new LogicOptionData[options.Options.Length];
                    for (int i = 0; i < options.Options.Length; i++)
                    {
                        var option = options.Options[i];
                        logicOptions[i] = new LogicOptionData(
                            option.ID,
                            new LogicLineData(option.Line.ID, option.Line.Substitutions),
                            option.IsAvailable);
                    }
                    var optionContext = new DialogueOptionContext(logicOptions);
                    _optionPublisher.Publish(new DialogueOptionEventMessage(
                        DialogueEvent.OptionsVisualizeRequested,
                        optionContext));
                },

                CommandHandler = (command) =>
                    _commandEventPublisher.Publish(new DialogueCommandEventMessage(
                        DialogueEvent.CommandRunRequested,
                        new DialogueCommandContext(command.Text))),

                NodeStartHandler = (x) => _nodeStartPublisher.Publish(new NodeStart(new NodeStartContext(x))),
                DialogueCompleteHandler = () => _nodeEndPublisher.Publish(new NodeEnd())
            };
            dialogue.SetProgram(_yarnProject.Program);

            return dialogue;
        }
    }
}