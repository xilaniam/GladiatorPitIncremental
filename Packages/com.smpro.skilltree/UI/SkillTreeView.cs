using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree.UI
{
    public class SkillTreeView : MonoBehaviour
    {
        [SerializeField] SkillTreeController controller;

        [Header("Prefabs")]
        [SerializeField] SkillNodeView nodePrefab;
        [SerializeField] ConnectorView connectorPrefab;

        [Header("Layout")]
        [SerializeField, Tooltip("Parent for node views")] RectTransform nodeLayer;
        [SerializeField, Tooltip("Parent for connectors; drawn below the nodes")] RectTransform connectorLayer;
        [SerializeField, Min(0.01f), Tooltip("UI units per tree unit of node position")] float spacing = 1f;

        [Header("Look")]
        [SerializeField] NodeStateStyle style;
        [SerializeField] SkillTooltip tooltip;
        [SerializeField, Tooltip("Hide generated effect lines on nodes that have flavour text")] bool flavourTextReplacesEffects = true;

        [Header("Behaviour")]
        [SerializeField, Tooltip("Clicking a node tries to buy its next level")] bool purchaseOnClick = true;

        readonly Dictionary<string, SkillNodeView> views = new();
        readonly List<ConnectorView> connectors = new();
        ICurrencyWallet subscribedWallet;
        SkillNodeView hovered;
        bool refreshPending;

        public SkillTreeController Controller => controller;
        public NodeStateStyle Style => style != null ? style : NodeStateStyle.Fallback;
        public IReadOnlyDictionary<string, SkillNodeView> Views => views;
        public bool IsBuilt { get; private set; }

        public event Action<string> NodeClicked;
        public event Action<string> NodePurchased;

        void Start()
        {
            if (!IsBuilt && controller != null) Build(controller);
        }

        void OnEnable()
        {
            if (IsBuilt) Refresh();
        }

        void OnDestroy() => Unsubscribe();

        public void Build(SkillTreeController treeController)
        {
            Clear();
            controller = treeController;
            if (controller == null || controller.Definition == null)
            {
                Debug.LogError($"{name}: SkillTreeView needs a controller with a definition.", this);
                return;
            }

            foreach (SkillNodeDefinition node in controller.Definition.nodes)
            {
                SkillNodeView view = Instantiate(nodePrefab, nodeLayer);
                view.RectTransform.anchoredPosition = node.position * spacing;
                view.Bind(this, node);
                views[node.id] = view;
            }

            foreach (SkillNodeDefinition node in controller.Definition.nodes)
            {
                foreach (NodeRequirement requirement in node.requirements)
                {
                    if (!views.TryGetValue(requirement.nodeId, out SkillNodeView parent)) continue;
                    ConnectorView connector = Instantiate(connectorPrefab, connectorLayer);
                    connector.Bind(parent, views[node.id], requirement);
                    connectors.Add(connector);
                }
            }

            controller.TreeChanged += RequestRefresh;
            subscribedWallet = controller.Wallet;
            if (subscribedWallet != null) subscribedWallet.BalanceChanged += OnBalanceChanged;

            IsBuilt = true;
            Refresh();
        }

        public void Refresh()
        {
            refreshPending = false;
            if (!IsBuilt) return;

            NodeStateStyle look = Style;
            foreach (SkillNodeView view in views.Values)
            {
                string id = view.NodeId;
                view.Refresh(controller.GetState(id), controller.GetLevel(id), controller.CanPurchase(id) == PurchaseCheck.Ok, look);
            }

            foreach (ConnectorView connector in connectors)
                connector.Refresh(controller.GetLevel(connector.Requirement.nodeId) >= connector.Requirement.level, look);

            if (tooltip != null && hovered != null) tooltip.SetText(BuildTooltip(hovered.NodeId));
        }

        public string BuildTooltip(string nodeId) =>
            controller.Definition.TryGetNode(nodeId, out SkillNodeDefinition node)
                ? TooltipText.Build(controller, node, Style, flavourTextReplacesEffects)
                : "";

        internal void HandleClick(SkillNodeView view)
        {
            NodeClicked?.Invoke(view.NodeId);
            if (!purchaseOnClick || !controller.TryPurchase(view.NodeId)) return;

            view.PlayPurchasePulse();
            NodePurchased?.Invoke(view.NodeId);
        }

        internal void HandleHover(SkillNodeView view, bool entered)
        {
            if (entered)
            {
                hovered = view;
                if (tooltip != null) tooltip.Show(view.RectTransform, BuildTooltip(view.NodeId));
            }
            else if (hovered == view)
            {
                hovered = null;
                if (tooltip != null) tooltip.Hide(view.RectTransform);
            }
        }

        void RequestRefresh()
        {
            if (isActiveAndEnabled) Refresh();
            else refreshPending = true;
        }

        void OnBalanceChanged(string currencyId) => RequestRefresh();

        void LateUpdate()
        {
            if (refreshPending) Refresh();
        }

        void Clear()
        {
            Unsubscribe();
            foreach (SkillNodeView view in views.Values) if (view != null) Destroy(view.gameObject);
            foreach (ConnectorView connector in connectors) if (connector != null) Destroy(connector.gameObject);
            views.Clear();
            connectors.Clear();
            hovered = null;
            IsBuilt = false;
        }

        void Unsubscribe()
        {
            if (controller != null) controller.TreeChanged -= RequestRefresh;
            if (subscribedWallet != null) subscribedWallet.BalanceChanged -= OnBalanceChanged;
            subscribedWallet = null;
        }
    }
}
