using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SMPro.SkillTree
{
    [Serializable, SkillEffectMenu("Unlock Feature")]
    public class UnlockFeatureEffect : ISkillEffect
    {
        public string featureId;
        [Tooltip("Name shown in tooltips; defaults to the feature id")] public string label;

        public void Apply(SkillEffectContext context, int level) => SkillFeatures.Unlock(featureId, this);

        public void Remove(SkillEffectContext context) => SkillFeatures.Lock(featureId, this);

        public string Describe(SkillTreeDefinition tree, int level) => $"Unlocks {(string.IsNullOrEmpty(label) ? Readable(featureId) : label)}";

        static string Readable(string id) =>
            string.IsNullOrEmpty(id) ? "nothing" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' ').Replace('.', ' '));

        public IEnumerable<string> Validate(SkillTreeDefinition tree)
        {
            if (string.IsNullOrEmpty(featureId)) yield return "Unlock Feature has no feature id.";
        }
    }
}
