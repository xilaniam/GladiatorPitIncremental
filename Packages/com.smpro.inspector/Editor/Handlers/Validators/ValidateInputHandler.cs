namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ValidateInputAttribute))]
    public sealed class ValidateInputHandler : ValidatorHandler<ValidateInputAttribute>
    {
        BoundGetter<object> check;

        protected override void OnInitialize()
        {
            check = ResolveValue<object>(Attribute.Method);
            if (!check.IsValid) ReportProblem($"ValidateInput: {check.Error}");
        }

        public override void Validate(ValidationReport report)
        {
            if (Problems.Count > 0) return;
            object value = Property.SerializedProperty != null ? Property.GetValue() : null;
            switch (check.Get(value))
            {
                case bool valid:
                    if (!valid) report.Add(SeverityOf(Attribute.Type), Attribute.Message ?? $"{DisplayName} is not valid");
                    return;
                case string message:
                    if (!string.IsNullOrEmpty(message)) report.Add(SeverityOf(Attribute.Type), message);
                    return;
                case null:
                    return;
                default:
                    report.Error($"ValidateInput: '{Attribute.Method}' must return bool or string");
                    return;
            }
        }
    }
}
