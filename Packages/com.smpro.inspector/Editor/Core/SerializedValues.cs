using System;
using System.Globalization;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    internal static class SerializedValues
    {
        public static bool IsNumber(SerializedProperty property) =>
            property != null && property.propertyType is SerializedPropertyType.Integer or SerializedPropertyType.Float;

        public static double ReadNumber(SerializedProperty property) =>
            property.propertyType == SerializedPropertyType.Integer ? property.longValue : property.doubleValue;

        public static void WriteNumber(SerializedProperty property, double value)
        {
            if (property.propertyType == SerializedPropertyType.Integer) property.longValue = (long)Math.Round(value);
            else property.doubleValue = value;
        }

        public static void Write(SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Float:
                    property.doubleValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Enum:
                    property.intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as Object;
                    break;
                default:
                    property.boxedValue = value;
                    break;
            }
        }

        public static bool AreEqual(object a, object b)
        {
            if (Equals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a is IConvertible && b is IConvertible && IsNumeric(a) && IsNumeric(b))
                return Convert.ToDouble(a, CultureInfo.InvariantCulture) == Convert.ToDouble(b, CultureInfo.InvariantCulture);
            return false;
        }

        static bool IsNumeric(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal || value is Enum;
    }

    internal sealed class FieldRow : BaseField<bool>
    {
        public FieldRow(string label, VisualElement input) : base(label, input)
        {
            AddToClassList(alignedFieldUssClassName);
            AddToClassList("sm-field-row");
            input.style.flexGrow = 1;
        }
    }
}
