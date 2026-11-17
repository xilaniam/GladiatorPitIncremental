using System;

namespace SMPro.Inspector
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class RequiredAttribute : InspectorAttribute
    {
        public RequiredAttribute(string message = null) => Message = message;

        public string Message { get; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class ValidateInputAttribute : InspectorAttribute
    {
        public ValidateInputAttribute(string method, string message = null, InfoMessageType type = InfoMessageType.Error)
        {
            Method = method;
            Message = message;
            Type = type;
        }

        public string Method { get; }
        public string Message { get; }
        public InfoMessageType Type { get; }
    }

    public abstract class LimitAttribute : InspectorAttribute
    {
        protected LimitAttribute(double value) => Value = value;

        protected LimitAttribute(string member) => Member = member;

        public double Value { get; }
        public string Member { get; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class MinValueAttribute : LimitAttribute
    {
        public MinValueAttribute(double value) : base(value)
        {
        }

        public MinValueAttribute(string member) : base(member)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class MaxValueAttribute : LimitAttribute
    {
        public MaxValueAttribute(double value) : base(value)
        {
        }

        public MaxValueAttribute(string member) : base(member)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class AssetsOnlyAttribute : InspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class SceneObjectsOnlyAttribute : InspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class ChildrenOnlyAttribute : InspectorAttribute
    {
    }
}
