using System;
using Cysharp.Threading.Tasks;

namespace LogicAbstract
{
    /// <summary>
    /// Yarn DialogueにCommandとFunctionを登録するためのインターフェース
    /// Yarn.Unity.IActionRegistrationと同等の機能を提供
    /// </summary>
    public interface IActionRegistration
    {
        // Command registration methods
        void AddCommandHandler(string commandName, Action action);
        void AddCommandHandler<T1>(string commandName, Action<T1> action);
        void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> action);
        void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> action);
        void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> action);

        // Async command registration methods
        void AddCommandHandler(string commandName, Func<UniTask> action);
        void AddCommandHandler<T1>(string commandName, Func<T1, UniTask> action);
        void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, UniTask> action);
        void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, UniTask> action);
        void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, UniTask> action);

        // Function registration methods
        void AddFunction<TResult>(string functionName, Func<TResult> function);
        void AddFunction<T1, TResult>(string functionName, Func<T1, TResult> function);
        void AddFunction<T1, T2, TResult>(string functionName, Func<T1, T2, TResult> function);
        void AddFunction<T1, T2, T3, TResult>(string functionName, Func<T1, T2, T3, TResult> function);
        void AddFunction<T1, T2, T3, T4, TResult>(string functionName, Func<T1, T2, T3, T4, TResult> function);

        // Remove handlers
        void RemoveCommandHandler(string commandName);
        void RemoveFunction(string functionName);
    }
}
