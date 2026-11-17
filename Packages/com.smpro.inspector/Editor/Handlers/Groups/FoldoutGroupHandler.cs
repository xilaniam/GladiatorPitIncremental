using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(FoldoutGroupAttribute))]
    public sealed class FoldoutGroupHandler : GroupHandler<FoldoutGroupAttribute>
    {
        Foldout foldout;

        public override VisualElement CreateGroup()
        {
            string key = PreferenceKey("expanded");
            foldout = new Foldout { text = Attribute.Name, value = EditorPrefs.GetBool(key, Attribute.Expanded) };
            foldout.AddToClassList("sm-foldout-group");
            foldout.RegisterValueChangedCallback(change =>
            {
                if (change.target == foldout) EditorPrefs.SetBool(key, change.newValue);
            });
            return foldout;
        }

        public override VisualElement ContentFor(GroupAttribute child) => foldout.contentContainer;
    }
}
