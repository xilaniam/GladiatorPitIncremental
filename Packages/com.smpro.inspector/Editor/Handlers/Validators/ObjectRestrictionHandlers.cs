using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    public abstract class ObjectRestrictionHandler<TAttribute> : ValidatorHandler<TAttribute> where TAttribute : InspectorAttribute
    {
        protected Object Target => Property.TargetCount > 0 ? Property.Targets[0] : null;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType != SerializedPropertyType.ObjectReference)
                ReportProblem($"{AttributeName}: only works on object references");
        }

        public override void Validate(ValidationReport report)
        {
            if (Problems.Count > 0) return;
            Object value = Property.SerializedProperty.objectReferenceValue;
            if (value == null) return;
            string message = Check(value);
            if (message != null) report.Error(message);
        }

        protected abstract string Check(Object value);
    }

    [AttributeHandler(typeof(AssetsOnlyAttribute))]
    public sealed class AssetsOnlyHandler : ObjectRestrictionHandler<AssetsOnlyAttribute>
    {
        protected override string Check(Object value) => EditorUtility.IsPersistent(value) ? null : $"{DisplayName} must be an asset, not a scene object";

        protected internal override void OnFieldCreated(VisualElement field) =>
            field.RegisterCallback<GeometryChangedEvent>(_ => field.Query<ObjectField>().ForEach(picker => picker.allowSceneObjects = false));
    }

    [AttributeHandler(typeof(SceneObjectsOnlyAttribute))]
    public sealed class SceneObjectsOnlyHandler : ObjectRestrictionHandler<SceneObjectsOnlyAttribute>
    {
        protected override string Check(Object value)
        {
            if (!EditorUtility.IsPersistent(value)) return null;
            Object target = Target;
            if (target != null && EditorUtility.IsPersistent(target) && AssetDatabase.GetAssetPath(value) == AssetDatabase.GetAssetPath(target)) return null;
            return $"{DisplayName} must be a scene object, not an asset";
        }
    }

    [AttributeHandler(typeof(ChildrenOnlyAttribute))]
    public sealed class ChildrenOnlyHandler : ObjectRestrictionHandler<ChildrenOnlyAttribute>
    {
        protected override string Check(Object value)
        {
            Transform root = (Target as Component)?.transform;
            if (root == null) return null;
            Transform candidate = value switch
            {
                Component component => component.transform,
                GameObject gameObject => gameObject.transform,
                _ => null
            };
            if (candidate == null) return $"{DisplayName} must be a GameObject or a component";
            return candidate.IsChildOf(root) ? null : $"{DisplayName} must be this object or one of its children";
        }
    }
}
