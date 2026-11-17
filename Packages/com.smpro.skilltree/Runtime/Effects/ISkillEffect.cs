using System;
using System.Collections.Generic;

namespace SMPro.SkillTree
{
    public interface ISkillEffect
    {
        void Apply(SkillEffectContext context, int level);
        void Remove(SkillEffectContext context);
        string Describe(SkillTreeDefinition tree, int level);
        IEnumerable<string> Validate(SkillTreeDefinition tree) => Array.Empty<string>();
    }

    public class SkillEffectContext
    {
        public SkillTreeController Controller { get; }
        public SkillNodeDefinition Node { get; }
        public SkillTreeDefinition Tree => Controller.Definition;
        public StatCollection Stats => Controller.Stats;

        public SkillEffectContext(SkillTreeController controller, SkillNodeDefinition node)
        {
            Controller = controller;
            Node = node;
        }

        public T Get<T>() where T : class => Controller.GetService<T>();
    }
}
