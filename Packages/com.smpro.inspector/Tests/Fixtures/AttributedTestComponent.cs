using System;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class AttributedTestComponent : MonoBehaviour
    {
        [Serializable]
        public class NestedWithAttributes
        {
            [TestField] public int inner;
            public int plainInner;
        }

        [Serializable]
        public class NestedPlain
        {
            public int value;
        }

        [TestField] public int marked;
        public int unmarked;
        [TestField, TestPriority] public int both;
        [TestUnhandled] public int unhandled;
        public NestedWithAttributes nested;
        public NestedPlain plain;
        [Header("Stats"), Tooltip("Nested stats")] public NestedWithAttributes headed;
    }
}
