using System;
using UnityEngine.Events;

namespace SMPro.SkillTree
{
    [Serializable, SkillEffectMenu("Unity Event")]
    public class UnityEventEffect : ISkillEffect
    {
        public string description;
        public UnityEvent<int> onLevelChanged = new();

        public void Apply(SkillEffectContext context, int level) => onLevelChanged.Invoke(level);

        public void Remove(SkillEffectContext context) => onLevelChanged.Invoke(0);

        public string Describe(SkillTreeDefinition tree, int level) => description;
    }
}
