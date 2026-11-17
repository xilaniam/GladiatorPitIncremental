using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    public abstract class PathHandler<TAttribute> : FieldHandler<TAttribute> where TAttribute : InspectorAttribute
    {
        protected abstract bool AbsolutePath { get; }

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType != SerializedPropertyType.String)
                ReportProblem($"{AttributeName}: only works on string fields");
        }

        public override VisualElement CreateField()
        {
            if (Problems.Count > 0) return null;
            var row = new VisualElement { name = "sm-path:" + Property.Path };
            row.AddToClassList("sm-path");
            var field = new PropertyField(Property.SerializedProperty, Property.Label) { name = "PropertyField:" + Property.Path };
            field.style.flexGrow = 1;
            row.Add(field);
            var browse = new Button(Browse) { text = "…", name = "sm-browse:" + Property.Path, tooltip = "Browse" };
            browse.AddToClassList("sm-path__browse");
            row.Add(browse);
            return row;
        }

        void Browse()
        {
            SerializedProperty property = Property.SerializedProperty;
            string picked = Pick(StartFolder(property.stringValue));
            if (string.IsNullOrEmpty(picked)) return;
            property.stringValue = AbsolutePath ? Normalize(picked) : ToProjectPath(picked);
            property.serializedObject.ApplyModifiedProperties();
        }

        protected abstract string Pick(string startFolder);

        static string StartFolder(string current)
        {
            if (string.IsNullOrEmpty(current)) return "Assets";
            string folder = Directory.Exists(current) ? current : Path.GetDirectoryName(current);
            return string.IsNullOrEmpty(folder) ? "Assets" : folder;
        }

        static string Normalize(string path) => Path.GetFullPath(path).Replace('\\', '/');

        public static string ToProjectPath(string path)
        {
            string root = Normalize(Path.Combine(Application.dataPath, ".."));
            string full = Normalize(path);
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length + 1) : full;
        }
    }

    [AttributeHandler(typeof(FilePathAttribute))]
    public sealed class FilePathHandler : PathHandler<FilePathAttribute>
    {
        protected override bool AbsolutePath => Attribute.AbsolutePath;

        protected override string Pick(string startFolder) =>
            EditorUtility.OpenFilePanel($"Choose {Property.Label}", startFolder, Attribute.Extensions ?? string.Empty);
    }

    [AttributeHandler(typeof(FolderPathAttribute))]
    public sealed class FolderPathHandler : PathHandler<FolderPathAttribute>
    {
        protected override bool AbsolutePath => Attribute.AbsolutePath;

        protected override string Pick(string startFolder) =>
            EditorUtility.OpenFolderPanel($"Choose {Property.Label}", startFolder, string.Empty);
    }
}
