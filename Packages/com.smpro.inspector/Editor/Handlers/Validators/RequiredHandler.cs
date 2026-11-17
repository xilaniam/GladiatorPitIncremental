using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(RequiredAttribute))]
    public sealed class RequiredHandler : ValidatorHandler<RequiredAttribute>
    {
        string Message => Attribute.Message ?? $"{DisplayName} is required";

        protected override void OnInitialize()
        {
            SerializedPropertyType? type = Property.SerializedProperty?.propertyType;
            if (type is SerializedPropertyType.ObjectReference or SerializedPropertyType.String or SerializedPropertyType.ManagedReference) return;
            ReportProblem("Required: only works on object references, strings and [SerializeReference] fields");
        }

        public override void Validate(ValidationReport report)
        {
            if (Problems.Count > 0) return;
            SerializedProperty property = Property.SerializedProperty;
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    if (property.objectReferenceValue != null) return;
                    report.Error(property.objectReferenceInstanceIDValue != 0 ? $"{DisplayName} is missing (the referenced object no longer exists)" : Message);
                    return;
                case SerializedPropertyType.String:
                    if (string.IsNullOrEmpty(property.stringValue)) report.Error(Message);
                    return;
                case SerializedPropertyType.ManagedReference:
                    if (property.managedReferenceValue == null) report.Error(Message);
                    return;
            }
        }
    }
}
