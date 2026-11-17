using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public abstract class InspectorTestBase
    {
        readonly List<Object> created = new();

        [SetUp]
        public void ClearReportedErrors() => InspectorBuilder.ClearReportedErrors();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object item in created)
                if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        protected T NewComponent<T>() where T : Component
        {
            GameObject owner = EditorUtility.CreateGameObjectWithHideFlags(typeof(T).Name, HideFlags.HideAndDontSave, typeof(T));
            created.Add(owner);
            return owner.GetComponent<T>();
        }

        protected T NewAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        protected Editor EditorFor(Object target)
        {
            Editor editor = Editor.CreateEditor(target);
            created.Add(editor);
            return editor;
        }

        protected Editor EditorFor(Object[] targets)
        {
            Editor editor = Editor.CreateEditor(targets);
            created.Add(editor);
            return editor;
        }

        protected VisualElement Inspect(Object target) => EditorFor(target).CreateInspectorGUI();

        protected static string[] ChildNames(VisualElement element) => element.Children().Select(child => child.name).ToArray();
    }
}
