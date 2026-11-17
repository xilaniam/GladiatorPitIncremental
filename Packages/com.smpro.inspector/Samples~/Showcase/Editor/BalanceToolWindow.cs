using System.Collections.Generic;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine;

namespace SMPro.Inspector.Samples.EditorTools
{
    public class BalanceToolWindow : SMEditorWindow
    {
        [Title("Balance", "Change every enemy profile at once")]
        [ListSettings(ShowAddRemove = true)] public List<EnemyProfile> profiles = new();
        [MinValue(0.1), MaxValue(10), Suffix("x")] public float healthScale = 1.5f;
        [ShowInInspector, ReadOnly] int ProfileCount => profiles.Count;

        [MenuItem("Window/SMPro/Showcase/Balance Tool")]
        static void Open() => GetWindow<BalanceToolWindow>().titleContent = new GUIContent("Balance Tool");

        [Button]
        void FindAllProfiles()
        {
            profiles.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(EnemyProfile)))
                profiles.Add(AssetDatabase.LoadAssetAtPath<EnemyProfile>(AssetDatabase.GUIDToAssetPath(guid)));
            RebuildInspector();
        }

        [Button(ButtonSize.Large), EnableIf(nameof(HasProfiles))]
        void ScaleHealth()
        {
            Undo.RecordObjects(profiles.ToArray(), "Scale health");
            foreach (EnemyProfile profile in profiles)
            {
                if (profile == null) continue;
                profile.maxHealth = Mathf.Max(1, Mathf.RoundToInt(profile.maxHealth * healthScale));
                profile.startingHealth = profile.maxHealth;
                EditorUtility.SetDirty(profile);
            }
        }

        bool HasProfiles => profiles.Count > 0;
    }
}
