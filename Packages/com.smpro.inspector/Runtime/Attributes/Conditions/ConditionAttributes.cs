using System;

namespace SMPro.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ConditionAttribute : InspectorAttribute
    {
        protected ConditionAttribute(string member, object[] values)
        {
            Member = member;
            Values = values ?? Array.Empty<object>();
        }

        public string Member { get; }
        public object[] Values { get; }
    }

    public sealed class ShowIfAttribute : ConditionAttribute
    {
        public ShowIfAttribute(string member, params object[] values) : base(member, values)
        {
        }
    }

    public sealed class HideIfAttribute : ConditionAttribute
    {
        public HideIfAttribute(string member, params object[] values) : base(member, values)
        {
        }
    }

    public sealed class EnableIfAttribute : ConditionAttribute
    {
        public EnableIfAttribute(string member, params object[] values) : base(member, values)
        {
        }
    }

    public sealed class DisableIfAttribute : ConditionAttribute
    {
        public DisableIfAttribute(string member, params object[] values) : base(member, values)
        {
        }
    }

    public sealed class ReadOnlyAttribute : InspectorAttribute
    {
    }

    public sealed class ShowInPlayModeAttribute : InspectorAttribute
    {
    }

    public sealed class HideInPlayModeAttribute : InspectorAttribute
    {
    }
}
