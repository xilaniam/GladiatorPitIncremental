using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    internal interface IValidatorHandler
    {
        void Validate(ValidationReport report);
    }

    public abstract class ValidatorHandler<TAttribute> : AttributeHandler<TAttribute>, IValidatorHandler where TAttribute : InspectorAttribute
    {
        public abstract void Validate(ValidationReport report);

        protected string DisplayName => string.IsNullOrEmpty(Property.Label) ? ObjectNames.NicifyVariableName(Property.Name) : Property.Label;

        protected static ValidationSeverity SeverityOf(InfoMessageType type) => type switch
        {
            InfoMessageType.Error => ValidationSeverity.Error,
            InfoMessageType.Warning => ValidationSeverity.Warning,
            _ => ValidationSeverity.Info
        };
    }
}
