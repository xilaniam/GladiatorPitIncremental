using System;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class StateTestComponent : MonoBehaviour
    {
        public enum Mode
        {
            Simple,
            Advanced,
            Expert
        }

        [Serializable]
        public class Nested
        {
            public bool on;
            [ShowIf(nameof(on))] public int inner;
            [ShowIf(nameof(toggle))] public int usesOuter;
        }

        public bool toggle;
        public Mode mode;
        public int count;

        [ShowIf(nameof(toggle))] public int shownWhenOn;
        [HideIf(nameof(toggle))] public int hiddenWhenOn;
        [ShowIf("!" + nameof(toggle))] public int shownWhenOff;
        [ShowIf(nameof(mode), Mode.Advanced, Mode.Expert)] public int advanced;
        [EnableIf(nameof(toggle))] public int enabledWhenOn;
        [DisableIf(nameof(toggle))] public int disabledWhenOn;
        [ReadOnly] public int locked;
        [ShowIf(nameof(toggle)), HideIf(nameof(mode), Mode.Expert)] public int bothConditions;
        [ShowInPlayMode] public int playOnly;
        [HideInPlayMode] public int editOnly;
        [BoxGroup("Box"), ShowIf(nameof(toggle))] public int boxA;
        [BoxGroup("Box"), ShowIf(nameof(toggle))] public int boxB;
        [HorizontalGroup("Row"), ShowIf(nameof(toggle))] public int rowA;
        [HorizontalGroup("Row")] public int rowB;
        [InfoBox("Too many", InfoMessageType.Warning, nameof(IsBig))] public int counted;
        [ShowIf(nameof(toggle)), Title("Decorated")] public int decorated;
        public Nested nested = new();

        bool IsBig => count > 10;
    }
}
