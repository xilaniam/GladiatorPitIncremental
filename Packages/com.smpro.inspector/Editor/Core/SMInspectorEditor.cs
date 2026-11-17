using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [CanEditMultipleObjects]
    public class SMInspectorEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (ShouldBuild()) return InspectorBuilder.Build(serializedObject);

            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        protected virtual bool ShouldBuild() => target != null && TypeModel.For(target.GetType()).UsesInspectorAttributes;
    }

    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true), CanEditMultipleObjects]
    internal sealed class SMBehaviourEditor : SMInspectorEditor
    {
        protected override bool ShouldBuild() => target != null && SMInspectorSettings.instance.AllowsFallbackFor(target.GetType()) && base.ShouldBuild();
    }

    [CustomEditor(typeof(ScriptableObject), true, isFallback = true), CanEditMultipleObjects]
    internal sealed class SMScriptableObjectEditor : SMInspectorEditor
    {
        protected override bool ShouldBuild() => target != null && SMInspectorSettings.instance.AllowsFallbackFor(target.GetType()) && base.ShouldBuild();
    }
}
