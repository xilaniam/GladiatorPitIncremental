using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(OnValueChangedAttribute))]
    public sealed class OnValueChangedHandler : DecoratorHandler<OnValueChangedAttribute>
    {
        MethodInfo method;
        InspectorProperty source;

        public override int Order => -100;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty == null)
            {
                ReportProblem("OnValueChanged: only works on serialized fields");
                return;
            }
            for (InspectorProperty level = Property; level != null && method == null; level = level.Parent)
            {
                if (level.OwnerType == null) continue;
                method = MemberResolver.FindMethod(level.OwnerType, Attribute.Method);
                source = level;
            }
            if (method == null) ReportProblem($"OnValueChanged: No method '{Attribute.Method}' on {Property.OwnerType?.Name}");
            else if (method.GetParameters().Length > 1) ReportProblem($"OnValueChanged: '{Attribute.Method}' takes {method.GetParameters().Length} parameters; at most 1 is supported");
        }

        public override VisualElement Decorate(VisualElement field)
        {
            if (Problems.Count == 0) field.TrackPropertyValue(Property.SerializedProperty, _ => Invoke());
            return field;
        }

        public void Invoke()
        {
            if (method == null || Problems.Count > 0) return;
            bool passValue = method.GetParameters().Length == 1;
            int calls = method.IsStatic ? 1 : Property.TargetCount;
            for (int i = 0; i < calls; i++)
            {
                object owner = method.IsStatic ? null : source.GetOwner(i);
                if (owner == null && !method.IsStatic) continue;
                try
                {
                    method.Invoke(owner, passValue ? new[] { Property.GetValue(i) } : null);
                }
                catch (TargetInvocationException exception)
                {
                    Debug.LogException(exception.InnerException ?? exception);
                }
                catch (System.ArgumentException exception)
                {
                    Debug.LogException(exception);
                }
            }
            foreach (Object target in Property.Targets)
                if (target != null) EditorUtility.SetDirty(target);
        }
    }
}
