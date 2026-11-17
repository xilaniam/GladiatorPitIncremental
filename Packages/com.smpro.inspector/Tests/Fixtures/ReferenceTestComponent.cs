using System;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public interface IShape
    {
    }

    [Serializable]
    public class Circle : IShape
    {
        public float radius = 1f;
    }

    [Serializable]
    public class Square : IShape
    {
        [MinValue(0)] public float size = 2f;
        public bool rounded;
        [ShowIf(nameof(rounded))] public float corner;
    }

    public class UnserializableShape : IShape
    {
    }

    [Serializable]
    public abstract class Shape
    {
    }

    [Serializable]
    public class Triangle : Shape
    {
        public float height = 1f;
    }

    public class ReferenceTestComponent : MonoBehaviour
    {
        public SerializableDictionary<string, int> scores = new();
        [SerializeReference, TypePicker] public IShape shape;
        [SerializeReference, TypePicker] public Shape baseShape;
        [TypePicker] public int notAReference;
        [InlineEditor] public InlineTestAsset settings;
        [InlineEditor] public int notAnObject;
    }
}
