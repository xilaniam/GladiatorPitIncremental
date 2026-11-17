namespace SMPro.Inspector.EditorTools
{
    public struct PropertyState
    {
        public bool Visible;
        public bool Enabled;

        public static PropertyState Default => new() { Visible = true, Enabled = true };
    }

    internal interface IStateHandler
    {
        bool NeedsPolling { get; }
        void Apply(ref PropertyState state);
    }

    public abstract class StateHandler<TAttribute> : AttributeHandler<TAttribute>, IStateHandler where TAttribute : InspectorAttribute
    {
        public virtual bool NeedsPolling => false;

        public abstract void Apply(ref PropertyState state);
    }

    public abstract class ConditionHandler<TAttribute> : StateHandler<TAttribute> where TAttribute : ConditionAttribute
    {
        BoundGetter<bool> condition;

        public override bool NeedsPolling => condition.IsValid && !condition.ReadsSerializedField;

        protected override void OnInitialize()
        {
            condition = ResolveCondition(Attribute.Member, Attribute.Values);
            if (!condition.IsValid) ReportProblem($"{AttributeName}: {condition.Error}");
        }

        protected bool IsMet(bool whenInvalid) => condition.IsValid ? condition.Get() : whenInvalid;
    }
}
