using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SMPro.Inspector.EditorTools
{
    internal static class HandlerRegistry
    {
        static Dictionary<Type, Type> handlers;

        static Dictionary<Type, Type> Handlers => handlers ??= Collect();

        public static Type HandlerTypeFor(Type attributeType)
        {
            for (Type current = attributeType; current != null && current != typeof(InspectorAttribute); current = current.BaseType)
                if (Handlers.TryGetValue(current, out Type handlerType)) return handlerType;
            return null;
        }

        public static AttributeHandler Create(InspectorProperty property, InspectorAttribute attribute)
        {
            Type handlerType = HandlerTypeFor(attribute.GetType());
            if (handlerType == null) return null;
            var handler = (AttributeHandler)Activator.CreateInstance(handlerType);
            handler.Initialize(property, attribute);
            return handler;
        }

        static Dictionary<Type, Type> Collect()
        {
            var map = new Dictionary<Type, Type>();
            Assembly builtIn = typeof(HandlerRegistry).Assembly;
            foreach (Type handlerType in TypeCache.GetTypesWithAttribute<AttributeHandlerAttribute>())
            {
                Type attributeType = handlerType.GetCustomAttribute<AttributeHandlerAttribute>().AttributeType;
                string problem = Problem(handlerType, attributeType);
                if (problem != null)
                {
                    Debug.LogWarning($"SMInspectorPro: handler {handlerType.FullName} is ignored: {problem}");
                    continue;
                }
                if (!map.TryGetValue(attributeType, out Type existing))
                {
                    map[attributeType] = handlerType;
                    continue;
                }
                bool existingBuiltIn = existing.Assembly == builtIn;
                bool newBuiltIn = handlerType.Assembly == builtIn;
                if (existingBuiltIn && !newBuiltIn) map[attributeType] = handlerType;
                else if (existingBuiltIn == newBuiltIn)
                    Debug.LogWarning($"SMInspectorPro: {attributeType.Name} has two handlers, {existing.FullName} and {handlerType.FullName}; using {existing.Name}");
            }
            return map;
        }

        static string Problem(Type handlerType, Type attributeType)
        {
            if (handlerType.IsAbstract) return "it is abstract";
            if (handlerType.GetConstructor(Type.EmptyTypes) == null) return "it has no parameterless constructor";
            if (attributeType == null || !typeof(InspectorAttribute).IsAssignableFrom(attributeType)) return "its attribute type is not an InspectorAttribute";
            Type handled = HandledAttribute(handlerType);
            if (handled == null) return "it does not derive from a handler base class";
            if (!handled.IsAssignableFrom(attributeType)) return $"it handles {handled.Name}, not {attributeType.Name}";
            return null;
        }

        static Type HandledAttribute(Type handlerType)
        {
            for (Type current = handlerType; current != null; current = current.BaseType)
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AttributeHandler<>))
                    return current.GetGenericArguments()[0];
            return null;
        }
    }
}
