using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS0414

namespace SMPro.Inspector.Tests
{
    public class MemberTestComponent : MonoBehaviour
    {
        public enum Mode
        {
            Idle,
            Busy
        }

        [Serializable]
        public class Nested
        {
            public int bumps;

            [Button] public void Bump() => bumps++;
        }

        public static int staticCalls;

        public int counter;
        public bool showConditional;
        [ShowInInspector] public int serializedAndShown;
        [OnValueChanged(nameof(Changed))] public int watched;
        [OnValueChanged(nameof(ChangedWithValue))] public float watchedValue;
        public Nested nested = new();

        [ShowInInspector] string status = "idle";
        [ShowInInspector] Dictionary<string, int> complex = new() { { "a", 1 } };

        [NonSerialized] public int changedCalls;
        [NonSerialized] public float lastValue;
        [NonSerialized] public Mode lastMode;
        [NonSerialized] public string lastNote;
        [NonSerialized] public bool lastFlag;

        [ShowInInspector] public int Doubled => counter * 2;

        [Button] public void Increment() => counter++;

        [Button("Add Many", ButtonSize.Large)]
        void AddMany(int amount = 5) => counter += amount;

        [Button]
        void Configure(Mode mode, string note, bool flag)
        {
            lastMode = mode;
            lastNote = note;
            lastFlag = flag;
        }

        [Button] static void CountStatic() => staticCalls++;

        [Button] void Throws() => throw new InvalidOperationException("button boom");

        [Button, ShowIf(nameof(showConditional))] void Conditional() { }

        [Button, BoxGroup("Actions")] void Grouped() { }

        [Button] void Unsupported(List<int> values) { }

        void Changed() => changedCalls++;

        void ChangedWithValue(float value) => lastValue = value;
    }
}
