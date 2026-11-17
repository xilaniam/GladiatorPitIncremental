using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(PreviewFieldAttribute))]
    public sealed class PreviewFieldHandler : FieldHandler<PreviewFieldAttribute>, IRefreshable
    {
        const long RetryDelay = 100;

        Image preview;
        Object shown;
        bool hasShown;

        public bool NeedsPolling => false;
        public Image Preview => preview;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType != SerializedPropertyType.ObjectReference)
                ReportProblem("PreviewField: only works on object references");
        }

        public override VisualElement CreateField()
        {
            if (Problems.Count > 0) return null;
            var container = new VisualElement { name = "sm-preview-field:" + Property.Path };
            container.Add(new PropertyField(Property.SerializedProperty, Property.Label) { name = "PropertyField:" + Property.Path });

            preview = new Image { name = "sm-preview:" + Property.Path, scaleMode = ScaleMode.ScaleToFit };
            preview.AddToClassList("sm-preview");
            preview.style.width = Attribute.Height;
            preview.style.height = Attribute.Height;
            var input = new VisualElement();
            input.Add(preview);
            container.Add(new FieldRow(" ", input));
            Refresh();
            return container;
        }

        public void Refresh()
        {
            if (preview == null) return;
            Object value = Property.SerializedProperty.objectReferenceValue;
            if (hasShown && value == shown) return;
            hasShown = true;
            shown = value;
            LoadPreview();
        }

        void LoadPreview()
        {
            if (shown == null)
            {
                preview.image = null;
                return;
            }
            Texture texture = AssetPreview.GetAssetPreview(shown);
            preview.image = texture != null ? texture : AssetPreview.GetMiniThumbnail(shown);
            if (texture == null && AssetPreview.IsLoadingAssetPreview(shown.GetInstanceID()))
                preview.schedule.Execute(LoadPreview).StartingIn(RetryDelay);
        }
    }
}
