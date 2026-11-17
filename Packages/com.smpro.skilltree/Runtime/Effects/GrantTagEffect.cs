using System;
using System.Collections.Generic;

namespace SMPro.SkillTree
{
    [Serializable, SkillEffectMenu("Grant Tag")]
    public class GrantTagEffect : ISkillEffect
    {
        public string tag;

        public void Apply(SkillEffectContext context, int level) => context.Controller.AddTag(tag, this);

        public void Remove(SkillEffectContext context) => context.Controller.RemoveTag(tag, this);

        public string Describe(SkillTreeDefinition tree, int level) => "";

        public IEnumerable<string> Validate(SkillTreeDefinition tree)
        {
            if (string.IsNullOrEmpty(tag)) yield return "Grant Tag has no tag.";
        }
    }
}
