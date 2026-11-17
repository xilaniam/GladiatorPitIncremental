using System;
using System.Collections.Generic;

namespace SMPro.SkillTree
{
    [Serializable, SkillEffectMenu("Unlock Feature")]
    public class UnlockFeatureEffect : ISkillEffect
    {
        public string featureId;

        public void Apply(SkillEffectContext context, int level) => SkillFeatures.Unlock(featureId, this);

        public void Remove(SkillEffectContext context) => SkillFeatures.Lock(featureId, this);

        public string Describe(SkillTreeDefinition tree, int level) => $"Unlocks {featureId}";

        public IEnumerable<string> Validate(SkillTreeDefinition tree)
        {
            if (string.IsNullOrEmpty(featureId)) yield return "Unlock Feature has no feature id.";
        }
    }
}
