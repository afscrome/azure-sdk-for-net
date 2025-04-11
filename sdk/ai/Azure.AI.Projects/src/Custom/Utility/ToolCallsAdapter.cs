// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace Azure.AI.Projects.Custom.Utility
{
    /// <summary>
    /// ToolCallsAdapter is used to resolve tool calls in the streaming API.
    /// </summary>
    internal class ToolCallsAdapter
    {
        private readonly Dictionary<string, Delegate> _delegates = new();

        internal ToolCallsAdapter(Dictionary<string, Delegate> delegates)
        {
            _delegates = delegates;
        }

        /// <summary>
        /// Indicates whether auto tool calls are enabled.
        /// </summary>
        public bool EnableAutoToolCalls
        {
            get
            {
                return _delegates.Count > 0;
            }
        }

        /// <summary>
        /// Resolves the tool call by invoking the delegate associated with the function name.
        /// </summary>
        public ToolOutput GetResolvedToolOutput(string functionName, string toolCallId, string functionArguments)
        {
            if (!EnableAutoToolCalls)
                throw new InvalidOperationException("Auto tool calls are not enabled.");
            if (_delegates.TryGetValue(functionName, out var func))
            {
                JsonDocument argumentsJson = JsonDocument.Parse(functionArguments);
                var method = func.Method;
                var args = new ArrayList();
                foreach (ParameterInfo param in func.Method.GetParameters())
                {
                    if (argumentsJson.RootElement.TryGetProperty(param.Name ?? "", out JsonElement element))
                    {
                        object val = GetArgumentValue(element, param.ParameterType);
                        args.Add(val);
                    }
                    else
                    {
                        args.Add(param.DefaultValue);
                    }
                }

                var rt = func.DynamicInvoke(args.ToArray());
                return new ToolOutput(toolCallId, rt?.ToString());
            }
            throw new InvalidOperationException($"Function {functionName} not found");
        }

        private object GetArgumentValue(JsonElement element, Type type)
        {
            if (type == typeof(string))
            {
                return element.GetString() ?? "";
            }
            else if (type == typeof(ushort))
            {
                if (element.TryGetInt16(out short val))
                    return val;
            }
            else if (type == typeof(float))
            {
                if (element.TryGetSingle(out float val))
                    return val;
            }
            else if (type == typeof(uint))
            {
                if (element.TryGetUInt32(out uint val))
                    return val;
            }
            else if (type == typeof(decimal))
            {
                if (element.TryGetDecimal(out decimal val))
                    return val;
            }
            else if (type == typeof(double))
            {
                if (element.TryGetDouble(out double val))
                    return val;
            }
            else if (type == typeof(long))
            {
                if (element.TryGetInt64(out long val))
                    return val;
            }
            else if (IsDictionaryType(type))
            {
                Type[] genericArguments = type.GetGenericArguments();
                Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), genericArguments[1]);

                // Create an instance of the dictionary
                var dict = Activator.CreateInstance(dictionaryType);

                MethodInfo addMethod = dictionaryType.GetMethod("Add");
                if (addMethod == null || dict == null)
                    throw new Exception();

                foreach (var prop in element.EnumerateObject())
                {
                    object val = GetArgumentValue(prop.Value, genericArguments[1]);
                    addMethod.Invoke(dict, [prop.Name, val]);
                }
                return dict;
            }
            else if (type.IsArray)
            {
                Type elementType = type.GetElementType()!;
                Array array = Array.CreateInstance(elementType, element.GetArrayLength());
                int i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    object val = GetArgumentValue(item, elementType);
                    array.SetValue(val, i++);
                }
                return array;
            }
            throw new ArgumentException($"Received {element.ToString()}, but the argument type in function is {type}");
        }

        private static bool IsDictionaryType(Type type)
        {
            // Check if the type is a generic type
            if (type.IsGenericType)
            {
                // Get the generic type definition
                Type genericTypeDefinition = type.GetGenericTypeDefinition();

                // Check if the generic type definition is Dictionary<,>
                if (genericTypeDefinition == typeof(Dictionary<,>))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
