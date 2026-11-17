using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    [Serializable]
    public class SkillTreeState
    {
        [Serializable]
        public struct NodeLevel
        {
            public string nodeId;
            public int level;
        }

        public string treeId;
        public List<NodeLevel> levels = new();

        public string ToJson() => JsonUtility.ToJson(this);

        public static SkillTreeState FromJson(string json) => JsonUtility.FromJson<SkillTreeState>(json);
    }
}
