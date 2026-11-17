namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ShowIfAttribute))]
    public sealed class ShowIfHandler : ConditionHandler<ShowIfAttribute>
    {
        public override void Apply(ref PropertyState state)
        {
            if (!IsMet(true)) state.Visible = false;
        }
    }

    [AttributeHandler(typeof(HideIfAttribute))]
    public sealed class HideIfHandler : ConditionHandler<HideIfAttribute>
    {
        public override void Apply(ref PropertyState state)
        {
            if (IsMet(false)) state.Visible = false;
        }
    }

    [AttributeHandler(typeof(EnableIfAttribute))]
    public sealed class EnableIfHandler : ConditionHandler<EnableIfAttribute>
    {
        public override void Apply(ref PropertyState state)
        {
            if (!IsMet(true)) state.Enabled = false;
        }
    }

    [AttributeHandler(typeof(DisableIfAttribute))]
    public sealed class DisableIfHandler : ConditionHandler<DisableIfAttribute>
    {
        public override void Apply(ref PropertyState state)
        {
            if (IsMet(false)) state.Enabled = false;
        }
    }
}
