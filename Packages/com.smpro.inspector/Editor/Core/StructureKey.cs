using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    internal readonly struct StructureKey
    {
        readonly SerializedProperty property;
        readonly string signature;

        StructureKey(SerializedProperty property)
        {
            this.property = property;
            signature = Read(property);
        }

        public static StructureKey Of(SerializedProperty property) => new(property.Copy());

        public bool Changed => Read(property) != signature;

        static string Read(SerializedProperty property) => property.propertyType == SerializedPropertyType.ManagedReference
            ? property.managedReferenceFullTypename
            : property.arraySize.ToString();
    }
}
