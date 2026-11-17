using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace SMPro.Inspector.Tests
{
    public class ProjectValidationTests : InspectorTestBase
    {
        const string Folder = "Assets/SMInspectorProValidationTemp";

        [SetUp]
        public void CreateFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "SMInspectorProValidationTemp");
        }

        [TearDown]
        public void DeleteFolder() => AssetDatabase.DeleteAsset(Folder);

        static ValidationOptions TempFolderOnly => new()
        {
            includeOpenScenes = false,
            includeBuildScenes = false,
            folders = new[] { Folder }
        };

        static string[] Messages(ValidationReport report) => report.Issues.Select(issue => issue.Message).ToArray();


        [Test]
        public void Validate_ReportsRules_WithoutFixingValues()
        {
            var component = NewComponent<ValidationTestComponent>();
            component.atLeastZero = -5;

            ValidationReport report = SMValidator.Validate(component);

            CollectionAssert.Contains(Messages(report), "Required Object is required");
            CollectionAssert.Contains(Messages(report), "At Least Zero must be at least 0");
            Assert.AreEqual(-5, component.atLeastZero);
            ValidationIssue required = report.Issues.First(issue => issue.Message == "Required Object is required");
            Assert.AreEqual("requiredObject", required.PropertyPath);
            Assert.AreSame(component, required.Context);
        }

        [Test]
        public void Validate_ReportsAttributeMisuse()
        {
            ValidationReport report = SMValidator.Validate(NewComponent<FieldConditionTestComponent>());

            ValidationIssue issue = report.Issues.Single();
            Assert.AreEqual("ShowIf: No member 'missingMember' on FieldConditionTestComponent", issue.Message);
            Assert.AreEqual("broken", issue.PropertyPath);
        }

        [Test]
        public void Validate_PlainComponent_HasNoIssues()
        {
            Assert.IsEmpty(SMValidator.Validate(NewComponent<PlainTestComponent>()).Issues);
        }

        [Test]
        public void Validate_GameObject_CoversItsChildren()
        {
            var parent = NewComponent<ValidationTestComponent>();
            var child = new GameObject("Child", typeof(FieldConditionTestComponent)) { hideFlags = HideFlags.HideAndDontSave };
            child.transform.SetParent(parent.transform);

            ValidationReport report = SMValidator.Validate(parent.gameObject);

            CollectionAssert.Contains(Messages(report), "Required Object is required");
            CollectionAssert.Contains(Messages(report), "ShowIf: No member 'missingMember' on FieldConditionTestComponent");
        }

        [Test]
        public void ValidateProject_FindsProblemsInAssetsAndPrefabs()
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ValidatedTestAsset>(), Folder + "/Data.asset");
            var broken = new GameObject("Broken", typeof(ValidationTestComponent));
            try
            {
                PrefabUtility.SaveAsPrefabAsset(broken, Folder + "/Broken.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(broken);
            }

            ValidationResult result = SMValidator.ValidateProject(TempFolderOnly);

            Assert.GreaterOrEqual(result.ObjectsChecked, 2);
            Assert.IsTrue(result.Records.Any(record => record.Location == Folder + "/Data.asset" && record.Message == "Reference is required"));
            Assert.IsTrue(result.Records.Any(record =>
                record.Location == Folder + "/Broken.prefab: Broken (ValidationTestComponent)" && record.Message == "Required Object is required"));
            Assert.Greater(result.ErrorCount, 1);
        }

        [Test]
        public void ValidateScene_FindsProblemsInSceneObjects()
        {
            Scene scene = SceneManager.GetActiveScene();
            var broken = new GameObject("Broken", typeof(ValidationTestComponent));
            try
            {
                var result = new ValidationResult();

                SMValidator.ValidateScene(scene, new ValidationReport(), result);

                string sceneName = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
                Assert.IsTrue(result.Records.Any(record => record.Location == $"{sceneName}: Broken (ValidationTestComponent)"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(broken);
            }
        }

        [Test]
        public void BuildCheck_FailsOnlyWhenEnabledAndThereAreErrors()
        {
            SMInspectorSettings settings = SMInspectorSettings.instance;
            bool previous = settings.failBuildOnErrors;
            try
            {
                var clean = new ValidationResult();
                var failing = new ValidationResult();
                failing.Add(new ValidationRecord(new ValidationIssue(ValidationSeverity.Error, "broken", null, null), "here"));

                settings.failBuildOnErrors = true;
                Assert.IsTrue(BuildValidation.ShouldFail(failing, settings));
                Assert.IsFalse(BuildValidation.ShouldFail(clean, settings));

                settings.failBuildOnErrors = false;
                Assert.IsFalse(BuildValidation.ShouldFail(failing, settings));
            }
            finally
            {
                settings.failBuildOnErrors = previous;
            }
        }

        [Test]
        public void ValidatorWindow_RunsAndFiltersResults()
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ValidatedTestAsset>(), Folder + "/Data.asset");
            ValidatorWindow window = ValidatorWindow.Open();
            try
            {
                window.Options.includeOpenScenes = false;
                window.Options.includeBuildScenes = false;
                window.Options.folders = new[] { Folder };

                window.Run();

                Assert.IsTrue(window.Visible.Any(record => record.Message == "Reference is required"));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Validate_IsFast()
        {
            const int runs = 50;
            var component = NewComponent<ValidationTestComponent>();
            SMValidator.Validate(component);

            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < runs; i++) SMValidator.Validate(component);
            watch.Stop();

            double milliseconds = watch.Elapsed.TotalMilliseconds / runs;
            Debug.Log($"SMValidator.Validate, 15-field component: {milliseconds:0.000} ms");
            Assert.Less(milliseconds, 5.0);
        }
    }
}
