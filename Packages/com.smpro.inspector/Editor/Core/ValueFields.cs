using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    internal interface IValueField
    {
        VisualElement Element { get; }
        object BoxedValue { get; set; }
    }

    internal sealed class ValueField<T> : IValueField
    {
        public ValueField(BaseField<T> field) => Field = field;

        public BaseField<T> Field { get; }
        public VisualElement Element => Field;

        public object BoxedValue
        {
            get => Field.value;
            set => Field.SetValueWithoutNotify(value is T typed ? typed : default);
        }
    }

    internal static class ValueFields
    {
        public static Type FieldValueType(Type type)
        {
            if (type == null) return null;
            if (type.IsEnum) return typeof(Enum);
            if (typeof(Object).IsAssignableFrom(type)) return typeof(Object);
            return Supported(type) ? type : null;
        }

        static bool Supported(Type type) =>
            type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) ||
            type == typeof(string) || type == typeof(bool) || type == typeof(Vector2) || type == typeof(Vector3) ||
            type == typeof(Vector4) || type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(Color) ||
            type == typeof(Rect) || type == typeof(RectInt) || type == typeof(Bounds) || type == typeof(BoundsInt) ||
            type == typeof(AnimationCurve) || type == typeof(Gradient);

        public static IValueField Create(Type type, string label)
        {
            if (type.IsEnum)
            {
                var initial = (Enum)Enum.ToObject(type, 0);
                BaseField<Enum> enumField = type.IsDefined(typeof(FlagsAttribute), false) ? new EnumFlagsField(label, initial) : new EnumField(label, initial);
                return new ValueField<Enum>(enumField);
            }
            if (typeof(Object).IsAssignableFrom(type)) return new ValueField<Object>(new ObjectField(label) { objectType = type, allowSceneObjects = true });
            if (type == typeof(int)) return new ValueField<int>(new IntegerField(label));
            if (type == typeof(long)) return new ValueField<long>(new LongField(label));
            if (type == typeof(float)) return new ValueField<float>(new FloatField(label));
            if (type == typeof(double)) return new ValueField<double>(new DoubleField(label));
            if (type == typeof(string)) return new ValueField<string>(new TextField(label));
            if (type == typeof(bool)) return new ValueField<bool>(new Toggle(label));
            if (type == typeof(Vector2)) return new ValueField<Vector2>(new Vector2Field(label));
            if (type == typeof(Vector3)) return new ValueField<Vector3>(new Vector3Field(label));
            if (type == typeof(Vector4)) return new ValueField<Vector4>(new Vector4Field(label));
            if (type == typeof(Vector2Int)) return new ValueField<Vector2Int>(new Vector2IntField(label));
            if (type == typeof(Vector3Int)) return new ValueField<Vector3Int>(new Vector3IntField(label));
            if (type == typeof(Color)) return new ValueField<Color>(new ColorField(label));
            if (type == typeof(Rect)) return new ValueField<Rect>(new RectField(label));
            if (type == typeof(RectInt)) return new ValueField<RectInt>(new RectIntField(label));
            if (type == typeof(Bounds)) return new ValueField<Bounds>(new BoundsField(label));
            if (type == typeof(BoundsInt)) return new ValueField<BoundsInt>(new BoundsIntField(label));
            if (type == typeof(AnimationCurve)) return new ValueField<AnimationCurve>(new CurveField(label));
            if (type == typeof(Gradient)) return new ValueField<Gradient>(new GradientField(label));
            return null;
        }

        public static object DefaultFor(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
