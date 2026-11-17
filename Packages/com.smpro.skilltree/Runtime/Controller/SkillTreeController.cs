using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SMPro.SkillTree
{
    public enum NodeState
    {
        Hidden,
        Mystery,
        Locked,
        Available,
        Maxed
    }

    public enum PurchaseCheck
    {
        Ok,
        UnknownNode,
        Maxed,
        Requirements,
        Conditions,
        Funds
    }

    [DefaultExecutionOrder(-100)]
    public class SkillTreeController : MonoBehaviour
    {
        [SerializeField] SkillTreeDefinition definition;
        [SerializeField] WalletProvider wallet;
        [SerializeReference, TypePicker] ISaveProvider saveProvider;

        readonly Dictionary<string, int> levels = new();
        readonly SourceSet tags = new();
        readonly Dictionary<Type, object> services = new();
        bool initialized;

        public SkillTreeDefinition Definition => definition;
        public StatCollection Stats => definition.Stats;
        public ICurrencyWallet Wallet { get; set; }
        public int TotalLevels { get; private set; }

        public event Action<string, int> NodeLevelChanged;
        public event Action TreeChanged;

        void Awake()
        {
            if (!initialized && definition != null) Initialize(definition, wallet);
        }

        void Start()
        {
            if (!initialized) Debug.LogError($"{name}: SkillTreeController has no definition.", this);
        }

        void Update()
        {
            if (initialized) Stats.Tick(Time.time);
        }

        void OnDestroy()
        {
            if (initialized) RemoveAllLevels();
        }

        public void Initialize(SkillTreeDefinition tree, ICurrencyWallet currencyWallet, ISaveProvider save = null)
        {
            if (initialized) RemoveAllLevels();

            definition = tree;
            Wallet = currencyWallet;
            if (save != null) saveProvider = save;
            initialized = true;

            tags.Clear();
            Stats.ClearModifiers();

            if (Wallet == null) Debug.LogWarning($"{name}: SkillTreeController has no wallet; nodes with a cost cannot be bought.", this);

            if (saveProvider != null && saveProvider.TryLoad(definition.treeId, out string json))
                RestoreState(SkillTreeState.FromJson(json));
        }

        public int GetLevel(string nodeId) => nodeId != null && levels.TryGetValue(nodeId, out int level) ? level : 0;

        public long GetCost(string nodeId) => definition.TryGetNode(nodeId, out SkillNodeDefinition node) ? node.GetCost(GetLevel(nodeId)) : 0;

        public bool RequirementsMet(SkillNodeDefinition node) => node.requirements.All(r => GetLevel(r.nodeId) >= r.level);

        public bool ConditionsMet(SkillNodeDefinition node) => node.conditions.All(c => c == null || c.IsMet(this));

        public NodeState GetState(string nodeId)
        {
            if (!definition.TryGetNode(nodeId, out SkillNodeDefinition node)) return NodeState.Hidden;

            int level = GetLevel(nodeId);
            if (level >= node.maxLevel) return NodeState.Maxed;
            if (RequirementsMet(node) && ConditionsMet(node)) return NodeState.Available;
            if (level > 0) return NodeState.Locked;

            return node.reveal switch
            {
                RevealRule.Always => NodeState.Locked,
                RevealRule.WhenParentStarted => ParentStarted(node) ? NodeState.Locked : NodeState.Hidden,
                RevealRule.MysteryUntilAvailable => NodeState.Mystery,
                _ => NodeState.Hidden
            };
        }

        public PurchaseCheck CanPurchase(string nodeId)
        {
            if (!definition.TryGetNode(nodeId, out SkillNodeDefinition node)) return PurchaseCheck.UnknownNode;
            if (GetLevel(nodeId) >= node.maxLevel) return PurchaseCheck.Maxed;
            if (!RequirementsMet(node)) return PurchaseCheck.Requirements;
            if (!ConditionsMet(node)) return PurchaseCheck.Conditions;

            long cost = node.GetCost(GetLevel(nodeId));
            if (cost > 0 && (Wallet == null || !Wallet.CanAfford(node.currencyId, cost))) return PurchaseCheck.Funds;

            return PurchaseCheck.Ok;
        }

        public bool TryPurchase(string nodeId)
        {
            if (CanPurchase(nodeId) != PurchaseCheck.Ok) return false;

            definition.TryGetNode(nodeId, out SkillNodeDefinition node);
            long cost = node.GetCost(GetLevel(nodeId));
            if (cost > 0 && !Wallet.TrySpend(node.currencyId, cost)) return false;

            SetLevel(node, GetLevel(nodeId) + 1);
            OnTreeChanged();
            return true;
        }

        public bool CanRefund(string nodeId)
        {
            if (GetLevel(nodeId) == 0) return false;
            return !definition.nodes.Any(n => GetLevel(n.id) > 0 && n.requirements.Any(r => r.nodeId == nodeId));
        }

        public bool TryRefund(string nodeId, float refundPercent = 1f)
        {
            if (!CanRefund(nodeId) || !definition.TryGetNode(nodeId, out SkillNodeDefinition node)) return false;

            Refund(node, refundPercent);
            SetLevel(node, 0);
            OnTreeChanged();
            return true;
        }

        public void ResetTree(float refundPercent = 1f)
        {
            foreach (SkillNodeDefinition node in LevelledNodes()) Refund(node, refundPercent);
            RemoveAllLevels();
            OnTreeChanged();
        }

        public SkillTreeState CaptureState()
        {
            var state = new SkillTreeState { treeId = definition.treeId };
            foreach (SkillNodeDefinition node in definition.nodes)
            {
                int level = GetLevel(node.id);
                if (level > 0) state.levels.Add(new SkillTreeState.NodeLevel { nodeId = node.id, level = level });
            }
            return state;
        }

        public void RestoreState(SkillTreeState state)
        {
            if (state == null) return;
            if (state.treeId != definition.treeId)
            {
                Debug.LogError($"{name}: save is for tree '{state.treeId}', not '{definition.treeId}'.", this);
                return;
            }

            RemoveAllLevels();
            foreach (SkillTreeState.NodeLevel saved in state.levels)
            {
                if (!definition.TryGetNode(saved.nodeId, out SkillNodeDefinition node))
                {
                    Debug.LogWarning($"{name}: save has unknown node '{saved.nodeId}'; it was ignored.", this);
                    continue;
                }
                SetLevel(node, Mathf.Clamp(saved.level, 0, node.maxLevel));
            }
            OnTreeChanged();
        }

        public bool HasTag(string tag) => tags.Contains(tag);

        public void AddTag(string tag, object source) => tags.Add(tag, source);

        public void RemoveTag(string tag, object source) => tags.Remove(tag, source);

        public void RegisterService<T>(T service) where T : class => services[typeof(T)] = service;

        public T GetService<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out object service)) return (T)service;

            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) &&
                FindFirstObjectByType(typeof(T)) is T found)
            {
                services[typeof(T)] = found;
                return found;
            }

            throw new InvalidOperationException($"No service of type {typeof(T).Name}. Register it with RegisterService.");
        }

        bool ParentStarted(SkillNodeDefinition node) =>
            node.requirements.Count == 0 || node.requirements.Any(r => GetLevel(r.nodeId) > 0);

        IEnumerable<SkillNodeDefinition> LevelledNodes() => definition.nodes.Where(n => GetLevel(n.id) > 0).ToList();

        void Refund(SkillNodeDefinition node, float refundPercent)
        {
            long amount = (long)Math.Floor(node.GetTotalCost(GetLevel(node.id)) * (double)refundPercent);
            if (amount > 0) Wallet?.Add(node.currencyId, amount);
        }

        void RemoveAllLevels()
        {
            foreach (SkillNodeDefinition node in LevelledNodes()) SetLevel(node, 0);
        }

        void SetLevel(SkillNodeDefinition node, int level)
        {
            int old = GetLevel(node.id);
            if (old == level) return;

            if (level > 0) levels[node.id] = level;
            else levels.Remove(node.id);
            TotalLevels += level - old;

            var context = new SkillEffectContext(this, node);
            foreach (ISkillEffect effect in node.effects)
            {
                if (effect == null) continue;
                if (level > 0) effect.Apply(context, level);
                else effect.Remove(context);
            }

            NodeLevelChanged?.Invoke(node.id, level);
        }

        void OnTreeChanged()
        {
            TreeChanged?.Invoke();
            saveProvider?.Save(definition.treeId, CaptureState().ToJson());
        }
    }
}
