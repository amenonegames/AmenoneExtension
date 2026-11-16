using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using LogicAbstract;
using UnityEngine;

namespace Logic
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class YarnCommandAttribute : Attribute
    {
        public string CommandName { get; }

        public YarnCommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }

    public class CommandDispatcher : ICommandDispatcher , ICommandStorage
    {
        private readonly Dictionary<string, CommandRegistration> _commands = new();

        private class CommandRegistration
        {
            public string Name { get; }
            public MethodInfo Method { get; }
            public object Target { get; }
            public ParameterInfo[] Parameters { get; }
            private int RequiredParameterCount { get; }

            public CommandRegistration(string name, MethodInfo method, object target)
            {
                Name = name;
                Method = method;
                Target = target;
                Parameters = method.GetParameters();
                RequiredParameterCount = Parameters.Count(parameter => !parameter.HasDefaultValue);
            }

            public async UniTask InvokeAsync(string[] args, CancellationToken cancellationToken)
            {
                var convertedArgs = ConvertArguments(args);
                var result = Method.Invoke(Target, convertedArgs);

                if (result is UniTask unitask)
                {
                    await unitask;
                }
                else if (result is System.Threading.Tasks.Task task)
                {
                    await task;
                }
                else
                {
                    // void or other return types - execute synchronously (completed in 1 frame)
                    return;
                }
            }

            private object[] ConvertArguments(string[] args)
            {
                if (args.Length < RequiredParameterCount || args.Length > Parameters.Length)
                {
                    throw new ArgumentException(
                        $"Command '{Name}' expects {RequiredParameterCount} to {Parameters.Length} arguments, but {args.Length} were provided.");
                }

                // 省かれた引数は、登録したメソッドの省略時の値で埋める
                var convertedArgs = new object[Parameters.Length];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    convertedArgs[i] = i < args.Length
                        ? ConvertArgument(args[i], Parameters[i].ParameterType, Parameters[i].Name)
                        : Parameters[i].DefaultValue;
                }

                return convertedArgs;
            }

            private object ConvertArgument(string arg, Type targetType, string parameterName)
            {
                if (targetType == typeof(string))
                {
                    return arg;
                }

                if (targetType == typeof(int))
                {
                    if (int.TryParse(arg, out var intValue))
                        return intValue;
                    throw new ArgumentException($"Cannot convert '{arg}' to int for parameter '{parameterName}'");
                }

                if (targetType == typeof(float))
                {
                    if (float.TryParse(arg, out var floatValue))
                        return floatValue;
                    throw new ArgumentException($"Cannot convert '{arg}' to float for parameter '{parameterName}'");
                }

                if (targetType == typeof(bool))
                {
                    if (arg.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    if (bool.TryParse(arg, out var boolValue))
                        return boolValue;
                    throw new ArgumentException($"Cannot convert '{arg}' to bool for parameter '{parameterName}'");
                }

                if (typeof(GameObject).IsAssignableFrom(targetType))
                {
                    var go = GameObject.Find(arg);
                    if (go == null)
                        throw new ArgumentException($"GameObject '{arg}' not found for parameter '{parameterName}'");
                    return go;
                }

                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    var go = GameObject.Find(arg);
                    if (go == null)
                        throw new ArgumentException($"GameObject '{arg}' not found for parameter '{parameterName}'");

                    var component = go.GetComponentInChildren(targetType);
                    if (component == null)
                        throw new ArgumentException(
                            $"Component of type '{targetType.Name}' not found on GameObject '{arg}' for parameter '{parameterName}'");
                    return component;
                }

                throw new NotSupportedException($"Parameter type '{targetType.Name}' is not supported for command arguments");
            }
        }

        public void RegisterCommand(string commandName, MethodInfo method, object target)
        {
            if (string.IsNullOrEmpty(commandName))
                throw new ArgumentException("Command name cannot be null or empty", nameof(commandName));

            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && target == null)
                throw new ArgumentException("Target cannot be null for instance methods", nameof(target));

            if (_commands.ContainsKey(commandName))
            {
                Debug.LogWarning($"Command '{commandName}' is already registered. Overwriting.");
            }

            _commands[commandName] = new CommandRegistration(commandName, method, target);
        }

        public void RegisterCommandsFromInstance(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<YarnCommandAttribute>();
                if (attribute != null)
                {
                    RegisterCommand(attribute.CommandName, method, instance);
                }
            }
        }

        public void RegisterStaticCommands(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<YarnCommandAttribute>();
                if (attribute != null)
                {
                    RegisterCommand(attribute.CommandName, method, null);
                }
            }
        }

        public void RegisterCommandHandler(string commandName, Action action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1>(string commandName, Action<T1> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1, T2>(string commandName, Action<T1, T2> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler(string commandName, Func<UniTask> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1>(string commandName, Func<T1, UniTask> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1, T2>(string commandName, Func<T1, T2, UniTask> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, UniTask> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void RegisterCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, UniTask> action)
        {
            RegisterCommand(commandName, action.Method, action.Target);
        }

        public void UnregisterCommandHandler(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
                throw new ArgumentException("Command name cannot be null or empty", nameof(commandName));

            if (_commands.ContainsKey(commandName))
            {
                _commands.Remove(commandName);
            }
            else
            {
                Debug.LogWarning($"Command '{commandName}' is not registered.");
            }
        }

        public async UniTask DispatchCommandAsync(string command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                Debug.LogWarning("Empty command received");
                return;
            }

            var parts = ParseCommand(command);
            if (parts.Length == 0)
            {
                Debug.LogWarning($"Invalid command format: '{command}'");
                return;
            }

            var commandName = parts[0];
            var args = parts.Skip(1).ToArray();

            if (!_commands.TryGetValue(commandName, out var registration))
            {
                Debug.LogWarning($"Unknown command: '{commandName}'");
                return;
            }

            try
            {
                await registration.InvokeAsync(args, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing command '{commandName}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        private string[] ParseCommand(string command)
        {
            var parts = new List<string>();
            var currentPart = string.Empty;
            var inQuotes = false;

            for (int i = 0; i < command.Length; i++)
            {
                var c = command[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(currentPart))
                    {
                        parts.Add(currentPart);
                        currentPart = string.Empty;
                    }
                }
                else
                {
                    currentPart += c;
                }
            }

            if (!string.IsNullOrEmpty(currentPart))
            {
                parts.Add(currentPart);
            }

            return parts.ToArray();
        }
    }
}
