namespace SMPro.Inspector
{
    public sealed class PropertyOrderAttribute : InspectorAttribute
    {
        public PropertyOrderAttribute(int order) => Order = order;

        public int Order { get; }
    }
}
