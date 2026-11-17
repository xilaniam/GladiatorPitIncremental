using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace SMPro.Inspector.EditorTools
{
    public abstract class LimitHandler<TAttribute> : ValidatorHandler<TAttribute> where TAttribute : LimitAttribute
    {
        BoundGetter<double> member;

        protected abstract bool IsMinimum { get; }

        protected override void OnInitialize()
        {
            if (!Supports(Property.SerializedProperty))
            {
                ReportProblem($"{AttributeName}: only works on numbers and vectors");
                return;
            }
            if (Attribute.Member == null) return;
            member = ResolveValue<double>(Attribute.Member);
            if (!member.IsValid) ReportProblem($"{AttributeName}: {member.Error}");
        }

        static bool Supports(SerializedProperty property) => property != null && property.propertyType switch
        {
            SerializedPropertyType.Integer or SerializedPropertyType.Float or
            SerializedPropertyType.Vector2 or SerializedPropertyType.Vector3 or SerializedPropertyType.Vector4 or
            SerializedPropertyType.Vector2Int or SerializedPropertyType.Vector3Int => true,
            _ => false
        };

        public override void Validate(ValidationReport report)
        {
            if (Problems.Count > 0) return;
            double limit = Attribute.Member != null ? member.Get() : Attribute.Value;
            SerializedProperty property = Property.SerializedProperty;
            if (!IsOutOfRange(property, limit)) return;

            if (report.ApplyFixes)
            {
                Clamp(property, limit);
                property.serializedObject.ApplyModifiedProperties();
                return;
            }
            string bound = IsMinimum ? "at least" : "at most";
            report.Error($"{DisplayName} must be {bound} {limit.ToString(CultureInfo.InvariantCulture)}");
        }

        bool Outside(double value, double limit) => IsMinimum ? value < limit : value > limit;

        float ClampFloat(float value, double limit) => Outside(value, limit) ? (float)limit : value;

        int ClampInt(int value, double limit)
        {
            if (!Outside(value, limit)) return value;
            return (int)(IsMinimum ? Math.Ceiling(limit) : Math.Floor(limit));
        }

        bool IsOutOfRange(SerializedProperty property, double limit)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return Outside(property.longValue, limit);
                case SerializedPropertyType.Float: return Outside(property.doubleValue, limit);
                case SerializedPropertyType.Vector2: return AnyOutside(property.vector2Value, limit);
                case SerializedPropertyType.Vector3: return AnyOutside(property.vector3Value, limit);
                case SerializedPropertyType.Vector4: return AnyOutside(property.vector4Value, limit);
                case SerializedPropertyType.Vector2Int: return Outside(property.vector2IntValue.x, limit) || Outside(property.vector2IntValue.y, limit);
                case SerializedPropertyType.Vector3Int:
                    Vector3Int vector = property.vector3IntValue;
                    return Outside(vector.x, limit) || Outside(vector.y, limit) || Outside(vector.z, limit);
                default: return false;
            }
        }

        bool AnyOutside(Vector4 vector, double limit) =>
            Outside(vector.x, limit) || Outside(vector.y, limit) || Outside(vector.z, limit) || Outside(vector.w, limit);

        void Clamp(SerializedProperty property, double limit)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.longValue = (long)(IsMinimum ? Math.Ceiling(limit) : Math.Floor(limit));
                    break;
                case SerializedPropertyType.Float:
                    property.doubleValue = limit;
                    break;
                case SerializedPropertyType.Vector2:
                    Vector2 two = property.vector2Value;
                    property.vector2Value = new Vector2(ClampFloat(two.x, limit), ClampFloat(two.y, limit));
                    break;
                case SerializedPropertyType.Vector3:
                    Vector3 three = property.vector3Value;
                    property.vector3Value = new Vector3(ClampFloat(three.x, limit), ClampFloat(three.y, limit), ClampFloat(three.z, limit));
                    break;
                case SerializedPropertyType.Vector4:
                    Vector4 four = property.vector4Value;
                    property.vector4Value = new Vector4(ClampFloat(four.x, limit), ClampFloat(four.y, limit), ClampFloat(four.z, limit), ClampFloat(four.w, limit));
                    break;
                case SerializedPropertyType.Vector2Int:
                    Vector2Int twoInt = property.vector2IntValue;
                    property.vector2IntValue = new Vector2Int(ClampInt(twoInt.x, limit), ClampInt(twoInt.y, limit));
                    break;
                case SerializedPropertyType.Vector3Int:
                    Vector3Int threeInt = property.vector3IntValue;
                    property.vector3IntValue = new Vector3Int(ClampInt(threeInt.x, limit), ClampInt(threeInt.y, limit), ClampInt(threeInt.z, limit));
                    break;
            }
        }
    }

    [AttributeHandler(typeof(MinValueAttribute))]
    public sealed class MinValueHandler : LimitHandler<MinValueAttribute>
    {
        protected override bool IsMinimum => true;
    }

    [AttributeHandler(typeof(MaxValueAttribute))]
    public sealed class MaxValueHandler : LimitHandler<MaxValueAttribute>
    {
        protected override bool IsMinimum => false;
    }
}
