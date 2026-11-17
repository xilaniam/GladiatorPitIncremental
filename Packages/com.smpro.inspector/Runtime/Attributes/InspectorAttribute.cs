using System;

namespace SMPro.Inspector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true)]
    public abstract class InspectorAttribute : Attribute
    {
    }
}
