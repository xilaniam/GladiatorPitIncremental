using UnityEngine;

namespace SMPro.SkillTree.UI
{
    [CreateAssetMenu(menuName = "SkillTree/Node State Style", fileName = "NodeStateStyle")]
    public class NodeStateStyle : ScriptableObject
    {
        [Header("Mystery")]
        public Sprite mysteryIcon;
        public string mysteryTitle = "???";
        [TextArea] public string mysteryText = "A special power lies down this path.\nKeep upgrading this branch to reveal it.";

        [Header("Node Tint")]
        public Color mysteryTint = Color.white;
        public Color lockedTint = new(0.55f, 0.55f, 0.55f, 1f);
        public Color availableTint = Color.white;
        public Color maxedTint = new(1f, 0.85f, 0.35f, 1f);

        [Header("Connectors")]
        public Color connectorInactive = new(1f, 1f, 1f, 0.25f);
        public Color connectorActive = Color.white;

        [Header("Highlight")]
        [Tooltip("Nodes with this tag show their highlight until they are bought")]
        public string highlightTag = "keystone";

        static NodeStateStyle fallback;

        public static NodeStateStyle Fallback
        {
            get
            {
                if (fallback == null)
                {
                    fallback = CreateInstance<NodeStateStyle>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }
                return fallback;
            }
        }

        public Color TintFor(NodeState state) => state switch
        {
            NodeState.Mystery => mysteryTint,
            NodeState.Locked => lockedTint,
            NodeState.Maxed => maxedTint,
            _ => availableTint
        };
    }
}
