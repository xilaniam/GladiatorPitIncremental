using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class FieldTestComponent : MonoBehaviour
    {
        public enum Element
        {
            Fire,
            Water,
            Earth
        }

        [Flags]
        public enum Tags
        {
            None = 0,
            Red = 1,
            Blue = 2,
            Green = 4
        }

        [ValueDropdown(nameof(Sizes))] public int size = 2;
        [ValueDropdown(nameof(NamedSpeeds))] public float speed = 1f;
        [ValueDropdown(nameof(names))] public string label = "b";
        [EnumToggleButtons] public Element element;
        [EnumToggleButtons] public Tags tags;
        [ProgressBar(0, 100)] public int health = 50;
        [ProgressBar(MaxMember = nameof(maxEnergy))] public float energy = 5f;
        public float maxEnergy = 20f;
        [MinMaxSlider(0f, 10f)] public Vector2 range = new(2f, 8f);
        [MinMaxSlider(0f, 100f)] public Vector2Int intRange = new(10, 20);
        [PreviewField(80f)] public Texture2D icon;
        [FilePath(Extensions = "json,txt")] public string file;
        [FolderPath] public string folder;

        public List<string> names = new() { "a", "b" };

        int[] Sizes => new[] { 1, 2, 4 };

        IEnumerable<ValueDropdownItem> NamedSpeeds => new[]
        {
            new ValueDropdownItem("Slow", 0.5f),
            new ValueDropdownItem("Normal", 1f),
            new ValueDropdownItem("Fast", 2f)
        };
    }
}
