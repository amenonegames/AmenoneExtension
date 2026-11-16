using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LogicAbstract
{
    public interface ICommandDispatcher
    {
        UniTask DispatchCommandAsync(string command, CancellationToken cancellationToken);
    }

    public interface ICommandStorage
    {
        void RegisterCommandHandler(string commandName, Action action);
        void RegisterCommandHandler<T1>(string commandName, Action<T1> action);
        void RegisterCommandHandler<T1, T2>(string commandName, Action<T1, T2> action);
        void RegisterCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> action);
        void RegisterCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> action);

        void RegisterCommandHandler(string commandName, Func<UniTask> action);
        void RegisterCommandHandler<T1>(string commandName, Func<T1, UniTask> action);
        void RegisterCommandHandler<T1, T2>(string commandName, Func<T1, T2, UniTask> action);
        void RegisterCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, UniTask> action);
        void RegisterCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, UniTask> action);

        void UnregisterCommandHandler(string commandName);
    }
}