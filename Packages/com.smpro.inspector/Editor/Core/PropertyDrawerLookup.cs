using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    internal static class PropertyDrawerLookup
    {
        static readonly FieldInfo TargetType = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo ForChildren = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.Instance | BindingFlags.NonPublic);

        static HashSet<Type> exact;
        static HashSet<Type> withChildren;

        public static bool HasDrawerFor(Type type)
        {
            if (exact == null) Collect();
            if (exact.Contains(type)) return true;
            for (Type current = type; current != null; current = current.BaseType)
                if (withChildren.Contains(current)) return true;
            return false;
        }

        static void Collect()
        {
            exact = new HashSet<Type>();
            withChildren = new HashSet<Type>();
            if (TargetType == null || ForChildren == null) return;
            foreach (Type drawer in TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>())
            {
                if (!typeof(PropertyDrawer).IsAssignableFrom(drawer)) continue;
                foreach (CustomPropertyDrawer attribute in drawer.GetCustomAttributes<CustomPropertyDrawer>(false))
                {
                    if (TargetType.GetValue(attribute) is not Type target) continue;
                    exact.Add(target);
                    if ((bool)ForChildren.GetValue(attribute)) withChildren.Add(target);
                }
            }
        }
    }
}
