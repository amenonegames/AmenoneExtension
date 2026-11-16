using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LogicAbstract;
using Yarn;

namespace Logic
{
    public class VariableStorage : IVariableStorage
    {
        private readonly Dictionary<string, object> _variables = new();
        private readonly Dictionary<string, Type> _variableTypes = new();

        public Program Program { get; set; }
        public ISmartVariableEvaluator SmartVariableEvaluator { get; set; }

        private void ValidateVariableName(string variableName)
        {
            if (!variableName.StartsWith("$"))
            {
                throw new ArgumentException(
                    $"{variableName} is not a valid variable name: Variable names must start with a '$'. " +
                    $"(Did you mean to use '${variableName}'?)");
            }
        }

        public void SetValue(string variableName, string stringValue)
        {
            ValidateVariableName(variableName);
            _variables[variableName] = stringValue;
            _variableTypes[variableName] = typeof(string);
        }

        public void SetValue(string variableName, float floatValue)
        {
            ValidateVariableName(variableName);
            _variables[variableName] = floatValue;
            _variableTypes[variableName] = typeof(float);
        }

        public void SetValue(string variableName, bool boolValue)
        {
            ValidateVariableName(variableName);
            _variables[variableName] = boolValue;
            _variableTypes[variableName] = typeof(bool);
        }

        public bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
        {
            ValidateVariableName(variableName);

            switch (GetVariableKind(variableName))
            {
                case VariableKind.Stored:
                    if (TryGetAsType(_variables, variableName, out result))
                    {
                        return true;
                    }
                    else
                    {
                        if (Program is null)
                        {
                            throw new InvalidOperationException(
                                $"Can't get initial value for variable {variableName}, because {nameof(Program)} is not set");
                        }
                        return Program.TryGetInitialValue(variableName, out result);
                    }

                case VariableKind.Smart:
                    if (SmartVariableEvaluator is null)
                    {
                        throw new InvalidOperationException(
                            $"Can't get value for smart variable {variableName}, because {nameof(SmartVariableEvaluator)} is not set");
                    }
                    return SmartVariableEvaluator.TryGetSmartVariable(variableName, out result);

                case VariableKind.Unknown:
                default:
                    result = default;
                    return false;
            }
        }

        public VariableKind GetVariableKind(string name)
        {
            if (_variables.ContainsKey(name))
            {
                return VariableKind.Stored;
            }
             if (Program != null)
            {
                return Program.GetVariableKind(name);
            }

            return VariableKind.Unknown;
        }

        public void Clear()
        {
            _variables.Clear();
            _variableTypes.Clear();
        }

        public bool Contains(string variableName)
        {
            return _variables.ContainsKey(variableName);
        }

        public (Dictionary<string, float>, Dictionary<string, string>, Dictionary<string, bool>) GetAllVariables()
        {
            var floatDict = new Dictionary<string, float>();
            var stringDict = new Dictionary<string, string>();
            var boolDict = new Dictionary<string, bool>();

            foreach (var variable in _variables)
            {
                var type = _variableTypes[variable.Key];

                if (type == typeof(float))
                {
                    floatDict.Add(variable.Key, Convert.ToSingle(variable.Value));
                }
                else if (type == typeof(string))
                {
                    stringDict.Add(variable.Key, Convert.ToString(variable.Value));
                }
                else if (type == typeof(bool))
                {
                    boolDict.Add(variable.Key, Convert.ToBoolean(variable.Value));
                }
            }

            return (floatDict, stringDict, boolDict);
        }

        public void SetAllVariables(
            Dictionary<string, float> floats,
            Dictionary<string, string> strings,
            Dictionary<string, bool> bools,
            bool clear = true)
        {
            if (clear)
            {
                Clear();
            }

            foreach (var value in floats)
            {
                SetValue(value.Key, value.Value);
            }

            foreach (var value in strings)
            {
                SetValue(value.Key, value.Value);
            }

            foreach (var value in bools)
            {
                SetValue(value.Key, value.Value);
            }
        }

        private static bool TryGetAsType<T>(Dictionary<string, object> dictionary, string key, [NotNullWhen(true)] out T result)
        {
            if (dictionary.TryGetValue(key, out var objectResult) &&
                typeof(T).IsAssignableFrom(objectResult.GetType()))
            {
                result = (T)objectResult;
                return true;
            }

            result = default;
            return false;
        }
    }
}
