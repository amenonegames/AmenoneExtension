using System;
using Cysharp.Threading.Tasks;
using LogicAbstract;
using Yarn;

namespace Logic
{
    /// <summary>
    /// Yarn DialogueにCommandとFunctionを登録する実装クラス
    /// Yarn.Unity.DialogueRunnerのIActionRegistration実装と同等の機能を提供
    /// CommandはCommandDispatcherに登録され、FunctionはDialogue.Libraryに登録される
    /// </summary>
    public class ActionRegistration : IActionRegistration
    {
        private readonly Dialogue _dialogue;
        private readonly ICommandStorage _commandDispatcher;

        public ActionRegistration(Dialogue dialogue, ICommandStorage commandDispatcher)
        {
            _dialogue = dialogue;
            _commandDispatcher = commandDispatcher;
        }

        // Command registration methods - CommandDispatcherに登録
        public void AddCommandHandler(string commandName, Action action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1>(string commandName, Action<T1> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        // Async command registration methods - CommandDispatcherに登録
        public void AddCommandHandler(string commandName, Func<UniTask> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1>(string commandName, Func<T1, UniTask> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, UniTask> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, UniTask> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, UniTask> action)
        {
            _commandDispatcher.RegisterCommandHandler(commandName, action);
        }

        // Function registration methods
        public void AddFunction<TResult>(string functionName, Func<TResult> function)
        {
            _dialogue.Library.RegisterFunction(functionName, function);
        }

        public void AddFunction<T1, TResult>(string functionName, Func<T1, TResult> function)
        {
            _dialogue.Library.RegisterFunction(functionName, function);
        }

        public void AddFunction<T1, T2, TResult>(string functionName, Func<T1, T2, TResult> function)
        {
            _dialogue.Library.RegisterFunction(functionName, function);
        }

        public void AddFunction<T1, T2, T3, TResult>(string functionName, Func<T1, T2, T3, TResult> function)
        {
            _dialogue.Library.RegisterFunction(functionName, function);
        }

        public void AddFunction<T1, T2, T3, T4, TResult>(string functionName, Func<T1, T2, T3, T4, TResult> function)
        {
            _dialogue.Library.RegisterFunction(functionName, function);
        }

        // Remove handlers
        public void RemoveCommandHandler(string commandName)
        {
            _commandDispatcher.UnregisterCommandHandler(commandName);
        }

        public void RemoveFunction(string functionName)
        {
            _dialogue.Library.DeregisterFunction(functionName);
        }
    }
}
