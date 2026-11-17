using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SMPro.Inspector.EditorTools
{
    public enum MemberKind
    {
        SerializedField,
        Field,
        Property,
        Method
    }

    public sealed class MemberModel
    {
        internal MemberModel(MemberInfo info, MemberKind kind, Type valueType, int declarationOrder, InspectorAttribute[] attributes)
        {
            Info = info;
            Kind = kind;
            ValueType = valueType;
            DeclarationOrder = declarationOrder;
            Attributes = attributes;
        }

        public string Name => Info.Name;
        public MemberInfo Info { get; }
        public MemberKind Kind { get; }
        public Type ValueType { get; }
        public int DeclarationOrder { get; }
        public IReadOnlyList<InspectorAttribute> Attributes { get; }

        public T Get<T>() where T : InspectorAttribute
        {
            for (int i = 0; i < Attributes.Count; i++)
                if (Attributes[i] is T match) return match;
            return null;
        }

        public bool Has<T>() where T : InspectorAttribute => Get<T>() != null;
    }

    public sealed class TypeModel
    {
        const BindingFlags DeclaredMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        static readonly Dictionary<Type, TypeModel> cache = new();

        readonly List<MemberModel> members = new();
        readonly Dictionary<string, MemberModel> byName = new(StringComparer.Ordinal);

        public static TypeModel For(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!cache.TryGetValue(type, out TypeModel model))
            {
                model = new TypeModel(type);
                cache[type] = model;
            }
            return model;
        }

        TypeModel(Type type)
        {
            Type = type;
            UsesInspectorAttributes = type.IsDefined(typeof(InspectorAttribute), true);
            foreach (Type declaring in UserHierarchy(type))
            {
                CollectFields(declaring);
                CollectProperties(declaring);
                CollectMethods(declaring);
            }
        }

        public Type Type { get; }
        public bool UsesInspectorAttributes { get; private set; }
        public IReadOnlyList<MemberModel> Members => members;

        public MemberModel Find(string name) => name != null && byName.TryGetValue(name, out MemberModel member) ? member : null;

        static List<Type> UserHierarchy(Type type)
        {
            var chain = new List<Type>();
            for (Type current = type; current != null && !IsEngineType(current); current = current.BaseType)
                chain.Add(current);
            chain.Reverse();
            return chain;
        }

        static bool IsEngineType(Type type)
        {
            if (type == typeof(object) || type == typeof(ValueType)) return true;
            string assembly = type.Assembly.GetName().Name;
            return assembly.StartsWith("UnityEngine", StringComparison.Ordinal) || assembly.StartsWith("UnityEditor", StringComparison.Ordinal);
        }

        static bool IsSerialized(FieldInfo field)
        {
            if (field.IsStatic || field.IsLiteral || field.IsInitOnly || field.IsNotSerialized) return false;
            return field.IsPublic || field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false);
        }

        void CollectFields(Type declaring)
        {
            FieldInfo[] fields = declaring.GetFields(DeclaredMembers);
            Array.Sort(fields, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            foreach (FieldInfo field in fields)
            {
                InspectorAttribute[] attributes = AttributesOf(field);
                if (IsSerialized(field)) Add(field, MemberKind.SerializedField, field.FieldType, attributes);
                else if (attributes.Length > 0) Add(field, MemberKind.Field, field.FieldType, attributes);
            }
        }

        void CollectProperties(Type declaring)
        {
            PropertyInfo[] properties = declaring.GetProperties(DeclaredMembers);
            Array.Sort(properties, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length > 0) continue;
                InspectorAttribute[] attributes = AttributesOf(property);
                if (attributes.Length > 0) Add(property, MemberKind.Property, property.PropertyType, attributes);
            }
        }

        void CollectMethods(Type declaring)
        {
            MethodInfo[] methods = declaring.GetMethods(DeclaredMembers);
            Array.Sort(methods, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            foreach (MethodInfo method in methods)
            {
                if (method.IsSpecialName) continue;
                InspectorAttribute[] attributes = AttributesOf(method);
                if (attributes.Length > 0) Add(method, MemberKind.Method, method.ReturnType, attributes);
            }
        }

        void Add(MemberInfo info, MemberKind kind, Type valueType, InspectorAttribute[] attributes)
        {
            var member = new MemberModel(info, kind, valueType, members.Count, attributes);
            members.Add(member);
            byName[member.Name] = member;
            if (attributes.Length > 0) UsesInspectorAttributes = true;
        }

        static InspectorAttribute[] AttributesOf(MemberInfo member)
        {
            Attribute[] found = Attribute.GetCustomAttributes(member, typeof(InspectorAttribute), true);
            if (found.Length == 0) return Array.Empty<InspectorAttribute>();
            var attributes = new InspectorAttribute[found.Length];
            for (int i = 0; i < found.Length; i++) attributes[i] = (InspectorAttribute)found[i];
            return attributes;
        }
    }
}
