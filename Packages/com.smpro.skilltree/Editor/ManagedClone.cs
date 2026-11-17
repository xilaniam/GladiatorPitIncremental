using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace SMPro.SkillTree.EditorTools
{
    internal static class ManagedClone
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static T Clone<T>(T source) => (T)CloneObject(source);

        static object CloneObject(object source)
        {
            if (source == null) return null;

            Type type = source.GetType();
            if (type.IsPrimitive || type.IsEnum || source is string || source is UnityEngine.Object) return source;

            if (source is Array array)
            {
                var copy = Array.CreateInstance(type.GetElementType(), array.Length);
                for (int i = 0; i < array.Length; i++) copy.SetValue(CloneObject(array.GetValue(i)), i);
                return copy;
            }

            if (source is IList list && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var copy = (IList)Activator.CreateInstance(type);
                foreach (object item in list) copy.Add(CloneObject(item));
                return copy;
            }

            object clone = type.GetConstructor(Type.EmptyTypes) != null
                ? Activator.CreateInstance(type)
                : FormatterServices.GetUninitializedObject(type);

            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(Fields))
                {
                    if (!IsSerialized(field)) continue;
                    field.SetValue(clone, CloneObject(field.GetValue(source)));
                }
            }
            return clone;
        }

        static bool IsSerialized(FieldInfo field) =>
            !field.IsNotSerialized &&
            !field.IsInitOnly &&
            (field.IsPublic || field.IsDefined(typeof(SerializeField), true) || field.IsDefined(typeof(SerializeReference), true));
    }
}
