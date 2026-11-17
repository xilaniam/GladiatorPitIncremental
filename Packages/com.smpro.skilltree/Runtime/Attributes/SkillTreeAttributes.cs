using System;
using UnityEngine;

namespace SMPro.SkillTree
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TypePickerAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class StatIdAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class NodeIdAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class SkillEffectMenuAttribute : Attribute
    {
        public string Path { get; }

        public SkillEffectMenuAttribute(string path) => Path = path;
    }
}
