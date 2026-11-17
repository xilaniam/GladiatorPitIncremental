using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class WindowTests : InspectorTestBase
    {
        const string Folder = "Assets/SMInspectorProWindowTemp";

        [TearDown]
        public void Clean()
        {
            TestMenuWindow.Items.Clear();
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        static IEnumerator WaitUntil(Func<bool> condition)
        {
            double deadline = EditorApplication.timeSinceStartup + 5.0;
            while (!condition() && EditorApplication.timeSinceStartup < deadline) yield return null;
        }

        [UnityTest]
        public IEnumerator EditorWindow_DrawsItsOwnFieldsWithAttributes()
        {
            var window = ScriptableObject.CreateInstance<TestToolWindow>();
            window.Show();
            try
            {
                VisualElement root = window.rootVisualElement;
                yield return WaitUntil(() => root.Q("SMInspectorPro") != null);

                Assert.IsNotNull(root.Q("sm-group:Settings"));
                Assert.IsNull(root.Q("PropertyField:m_Script"));
                Assert.IsNotNull(root.Q<Button>("sm-button:Increment"));
                Assert.IsNotNull(root.Q("sm-validation:reference").Q<HelpBox>());

                InspectorBuilder.RefresherOf(root.Q("SMInspectorPro")).Find("Increment").Handlers.OfType<ButtonHandler>().Single().Invoke();
                Assert.AreEqual(1, window.count);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MenuTree_NormalizesPaths_AndNamesEntries()
        {
            var tree = new MenuTree();

            MenuEntry entry = tree.Add(" Enemies // Goblin ", null);
            tree.Add("", null);

            Assert.AreEqual("Enemies/Goblin", entry.Path);
            Assert.AreEqual("Goblin", entry.Name);
            Assert.AreEqual(1, tree.Entries.Count);
        }

        [Test]
        public void MenuTree_AddsEveryAssetOfAType()
        {
            AssetDatabase.CreateFolder("Assets", "SMInspectorProWindowTemp");
            var first = ScriptableObject.CreateInstance<ValidatedTestAsset>();
            var second = ScriptableObject.CreateInstance<ValidatedTestAsset>();
            AssetDatabase.CreateAsset(second, Folder + "/Beta.asset");
            AssetDatabase.CreateAsset(first, Folder + "/Alpha.asset");
            var tree = new MenuTree();

            tree.AddAllAssetsOf<ValidatedTestAsset>("Data", Folder);

            CollectionAssert.AreEqual(new[] { "Data/Alpha", "Data/Beta" }, tree.Entries.Select(entry => entry.Path));
            Assert.AreSame(first, tree.Entries[0].Target);
        }

        [UnityTest]
        public IEnumerator MenuWindow_ShowsTheTree_SelectsAndSearches()
        {
            var first = NewAsset<InlineTestAsset>();
            var second = NewAsset<InlineTestAsset>();
            var third = NewAsset<ValidatedTestAsset>();
            TestMenuWindow.Items.Add(("Assets/First", first));
            TestMenuWindow.Items.Add(("Assets/Second", second));
            TestMenuWindow.Items.Add(("Other/Third", third));

            var window = ScriptableObject.CreateInstance<TestMenuWindow>();
            window.Show();
            try
            {
                yield return WaitUntil(() => window.Tree != null);
                Assert.AreEqual(3, window.VisibleCount);

                Assert.IsTrue(window.Select("Assets/Second"));
                Assert.AreSame(second, window.Selected.Target);
                var inspector = window.rootVisualElement.Q<InspectorElement>("sm-menu-inspector");
                Assert.IsNotNull(inspector);

                window.Search("thi");
                Assert.AreEqual(1, window.VisibleCount);
                window.Search(string.Empty);
                Assert.AreEqual(3, window.VisibleCount);
                Assert.IsFalse(window.Select("Nope"));
            }
            finally
            {
                window.Close();
            }
        }
    }
}
