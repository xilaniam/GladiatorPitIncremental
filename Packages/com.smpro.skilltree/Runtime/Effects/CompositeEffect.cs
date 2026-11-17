using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SMPro.SkillTree
{
    [Serializable, SkillEffectMenu("Composite")]
    public class CompositeEffect : ISkillEffect
    {
        [SerializeReference, TypePicker] public List<ISkillEffect> effects = new();

        public void Apply(SkillEffectContext context, int level)
        {
            foreach (ISkillEffect effect in effects) effect?.Apply(context, level);
        }

        public void Remove(SkillEffectContext context)
        {
            foreach (ISkillEffect effect in effects) effect?.Remove(context);
        }

        public string Describe(SkillTreeDefinition tree, int level) =>
            string.Join("\n", effects.Where(e => e != null)
                .Select(e => e.Describe(tree, level))
                .Where(line => !string.IsNullOrEmpty(line)));

        public IEnumerable<string> Validate(SkillTreeDefinition tree) =>
            effects.Where(e => e != null).SelectMany(e => e.Validate(tree));
    }
}
