using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.Tests
{
    public class ValidationTestComponent : MonoBehaviour, ISelfValidator
    {
        [Serializable]
        public class Nested : ISelfValidator
        {
            public int value;

            public void Validate(ValidationReport report)
            {
                if (value < 0) report.Error("Nested value is negative", nameof(value));
            }
        }

        [Required] public GameObject requiredObject;
        [Required("Give it a name")] public string title = "named";
        [ValidateInput(nameof(IsEven), "Must be even")] public int even;
        [ValidateInput(nameof(CheckName))] public string nickname;
        [ValidateInput(nameof(IsPositive), "Should be positive", InfoMessageType.Warning)] public float score = 1f;
        [MinValue(0)] public int atLeastZero;
        [MaxValue(10)] public float atMostTen;
        [MinValue(nameof(lowest))] public int atLeastLowest = 5;
        public int lowest = 5;
        [MinValue(0), MaxValue(1)] public Vector2 unitRange;
        [AssetsOnly] public Object assetOnly;
        [SceneObjectsOnly] public Object sceneOnly;
        [ChildrenOnly] public Transform child;
        public int selfChecked;
        public Nested nested = new();

        bool IsEven(int value) => value % 2 == 0;

        string CheckName(string value) => value == "bad" ? "Name is bad" : null;

        bool IsPositive(float value) => value > 0f;

        public void Validate(ValidationReport report)
        {
            if (selfChecked > 100) report.Error("Too big", nameof(selfChecked));
            if (selfChecked == 42) report.Warning("Suspicious");
        }
    }
}
