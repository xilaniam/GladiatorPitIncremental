using System;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class DecoratorTestComponent : MonoBehaviour
    {
        [Serializable]
        public class Inline
        {
            [TestField] public int x;
            public int y;
        }

        [Title("Combat", "Damage settings")] public int damage;
        [Title("Plain", horizontalLine: false)] public int plain;
        [InfoBox("Careful", InfoMessageType.Warning)] public int risky;
        [LabelText("Move Speed")] public float speed;
        [HideLabel] public string note;
        [Suffix("s")] public float cooldown;
        [Indent(2)] public int indented;
        [Suffix("m"), Title("Stacked"), InfoBox("Read me")] public float stacked;
        [BoxGroup("Box"), Title("In Box")] public int boxed;
        [TestField, Title("Custom")] public int custom;
        [HideLabel] public Inline inline;
        [LabelText("Renamed Nested")] public Inline renamedNested;
    }
}
