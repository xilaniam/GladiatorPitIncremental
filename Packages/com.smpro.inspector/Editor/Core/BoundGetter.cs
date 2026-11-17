using System;

namespace SMPro.Inspector.EditorTools
{
    public readonly struct BoundGetter<T>
    {
        readonly Getter<T> getter;
        readonly InspectorProperty source;

        BoundGetter(Getter<T> getter, InspectorProperty source, bool readsSerializedField)
        {
            this.getter = getter;
            this.source = source;
            ReadsSerializedField = readsSerializedField;
        }

        public bool IsValid => getter.IsValid;
        public string Error => getter.Error;
        public bool ReadsSerializedField { get; }

        public T Get() => getter.Get(source?.GetOwner());
        public T Get(object argument) => getter.Get(source?.GetOwner(), argument);

        internal static BoundGetter<T> Value(InspectorProperty property, string path) =>
            Bind(property, path, owner => MemberResolver.Value<T>(owner, path));

        internal static BoundGetter<bool> Condition(InspectorProperty property, string path, object[] values) =>
            BoundGetter<bool>.Bind(property, path, owner => MemberResolver.Condition(owner, path, values));

        static BoundGetter<T> Bind(InspectorProperty property, string path, Func<Type, Getter<T>> resolve)
        {
            Getter<T> nearest = Getter<T>.Fail(property?.OwnerType == null ? "No owner type" : null);
            bool hasNearest = false;
            for (InspectorProperty level = property; level != null; level = level.Parent)
            {
                if (level.OwnerType == null) continue;
                Getter<T> getter = resolve(level.OwnerType);
                if (getter.IsValid) return new BoundGetter<T>(getter, level, IsSerializedField(level.OwnerType, path));
                if (hasNearest) continue;
                nearest = getter;
                hasNearest = true;
            }
            return new BoundGetter<T>(nearest, property, false);
        }

        static bool IsSerializedField(Type owner, string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string name = path.Trim().TrimStart('!').Trim();
            if (name.IndexOf('.') >= 0) return false;
            return TypeModel.For(owner).Find(name)?.Kind == MemberKind.SerializedField;
        }
    }
}
