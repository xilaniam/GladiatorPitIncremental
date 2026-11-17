using System;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class LayoutTestComponent : MonoBehaviour
    {
        [Serializable]
        public class Nested
        {
            [BoxGroup("Inner")] public int a;
            [BoxGroup("Inner")] public int b;
            public int c;
        }

        public int first;
        [BoxGroup("Stats")] public int health;
        [BoxGroup("Stats")] public int armor;
        public int between;
        [BoxGroup("Hidden Title", false)] public int untitled;
        [FoldoutGroup("Closed")] public int closedValue;
        [FoldoutGroup("Open", true)] public int openValue;
        [TabGroup("Tabs", "Combat")] public int damage;
        [TabGroup("Tabs", "Loot")] public int gold;
        [TabGroup("Tabs", "Combat")] public float range;
        [BoxGroup("Tabs/Combat/Crits")] public float critChance;
        [HorizontalGroup("Row")] public float speed;
        [HorizontalGroup("Row", 0.3f)] public bool canDash;
        [HorizontalGroup("Row", 80f)] public int lane;
        [VerticalGroup("Row/Column")] public int columnTop;
        [VerticalGroup("Row/Column")] public int columnBottom;
        [BoxGroup("Implicit/Child")] public int implicitChild;
        [PropertyOrder(-1)] public int movedFirst;
        [PropertyOrder(5)] public int movedLast;
        public Nested nested;
    }
}
