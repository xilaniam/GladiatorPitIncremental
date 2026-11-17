using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    public static class SkillFeatures
    {
        static readonly List<SkillFeature> features = new();
        static readonly SourceSet unlocked = new();

        public static bool IsUnlocked(string featureId) => unlocked.Contains(featureId);

        public static void Unlock(string featureId, object source)
        {
            if (unlocked.Add(featureId, source)) SetUnlocked(featureId, true);
        }

        public static void Lock(string featureId, object source)
        {
            if (unlocked.Remove(featureId, source)) SetUnlocked(featureId, false);
        }

        internal static void Register(SkillFeature feature)
        {
            if (features.Contains(feature)) return;
            features.Add(feature);
            if (IsUnlocked(feature.FeatureId)) feature.SetUnlocked(true);
        }

        internal static void Unregister(SkillFeature feature)
        {
            features.Remove(feature);
            feature.SetUnlocked(false);
        }

        static void SetUnlocked(string featureId, bool value)
        {
            foreach (SkillFeature feature in features.ToArray())
            {
                if (feature != null && feature.FeatureId == featureId) feature.SetUnlocked(value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Clear()
        {
            features.Clear();
            unlocked.Clear();
        }
    }
}
