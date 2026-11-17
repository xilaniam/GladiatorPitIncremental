# SMPro Skill Tree

Data-driven skill, talent, upgrade and research trees for Unity 6.

Full documentation, with measured performance: `Documentation~/index.html` (open it in a browser).

One asset holds the whole tree. Game code reads the results as stats. Adding or changing a node needs no new script, asset or scene object.

## Install

1. Install **PrimeTween** if you want the ready-made UI (`SMPro.SkillTree.UI`). The runtime and editor tools work without it. The UI assembly only compiles when PrimeTween is present.
2. Window ▸ Package Manager ▸ **+** ▸ **Add package from git URL** and paste:

   ```
   https://github.com/xilaniam/GladiatorPitIncremental.git?path=/Packages/com.smpro.skilltree#v1.0.1
   ```

   If the package later moves to its own repository, use that repository's URL and leave out `?path=...`.
3. Optional: Package Manager ▸ SMPro Skill Tree ▸ Samples ▸ **Starter Tree** ▸ Import. The sample uses TextMesh Pro, so import TMP Essentials if Unity asks.

## Quick start

1. **Create the tree.** Create ▸ SkillTree ▸ Skill Tree Definition. Double-click it to open the graph. Add stats in the Inspector, then right-click the graph ▸ Add Skill Node and drag **Unlocks → Requires** to link nodes.
2. **Add the controller.** Put `SkillTreeController` on any GameObject. Assign the definition and a wallet: `SimpleWallet`, or any component that implements `ICurrencyWallet`.
3. **Show it (optional).** Add `SkillTreeView` to a RectTransform inside a Canvas. Give it a node prefab (`SkillNodeView`), a connector prefab (`ConnectorView`), a node layer and a connector layer. The Starter Tree sample has ready-made prefabs to copy.

Read results from game code:

```csharp
using SMPro.SkillTree;

public class Spear : MonoBehaviour
{
    [SerializeField] StatRef damage;   // pick tree + stat in the Inspector

    float Hit() => damage.Value;
}
```

## Concepts

| Concept | What it is |
|---|---|
| `SkillTreeDefinition` | ScriptableObject with `stats` and `nodes`. Never changed at runtime. |
| Node | Id, name, icon, position, max level, currency, cost curve, requirements, conditions, effects, reveal rule, tags. |
| Stat | `value = (base + Σ Flat) × (1 + Σ PercentAdd) × Π Multiply`, then clamped; an Override wins. Rebuilt from node levels, never saved. |
| Effect | Inline `ISkillEffect`. `Apply(ctx, level)` sets the result for that level, so purchase and load give the same answer. |
| Condition | Inline `ISkillCondition` checked on top of node requirements. |
| Feature | `SkillFeature` component switched on by an Unlock Feature effect. It reads its numbers from `StatRef`s, so upgrading it is just Modify Stat. |
| Reveal rule | `Always`, `WhenParentStarted`, `WhenRequirementsMet`, `MysteryUntilAvailable`. |
| Node state | `Hidden`, `Mystery`, `Locked`, `Available`, `Maxed`, from `controller.GetState(id)`. |

### Level scaling

Modify Stat scales with level: Flat and PercentAdd use value × level, Multiply uses value ^ level, and Override uses the value as is. Fill **Value By Level** to set exact numbers per level instead.

## Controller API

```csharp
controller.GetLevel(id);  controller.GetCost(id);  controller.GetState(id);
controller.CanPurchase(id);   // Ok, UnknownNode, Maxed, Requirements, Conditions, Funds
controller.TryPurchase(id);
controller.TryRefund(id, refundPercent);       // only when no owned node requires it
controller.ResetTree(refundPercent);           // full respec
controller.NodeLevelChanged += (id, level) => { };
controller.TreeChanged += () => { };

string json = controller.CaptureState().ToJson();               // {treeId, levels}
controller.RestoreState(SkillTreeState.FromJson(json));          // unknown ids ignored, levels clamped
```

Set a **Save Provider** (PlayerPrefs or File) on the controller to load on start and save after every change.

Temporary buffs go through stats, too:

```csharp
attackInterval.AddTemporary(ModifierKind.Multiply, 0.5f, duration: 5f, source: this);
attackInterval.RemoveModifiers(this);
```

## Extending

Every extension is one `[Serializable]` class. It shows up in the Inspector's type menu after compiling.

```csharp
[Serializable, SkillEffectMenu("My Game/Extra Spawn Chance")]
public class ExtraSpawnChanceEffect : ISkillEffect
{
    public float chancePerLevel = 0.1f;

    public void Apply(SkillEffectContext ctx, int level) => ctx.Get<EnemyManager>().ExtraSpawnChance = chancePerLevel * level;
    public void Remove(SkillEffectContext ctx) => ctx.Get<EnemyManager>().ExtraSpawnChance = 0f;
    public string Describe(SkillTreeDefinition tree, int level) => $"{chancePerLevel * level:P0} chance to spawn an extra enemy";
}

[Serializable]
public class PlayerLevelCondition : ISkillCondition
{
    public int level = 10;
    public bool IsMet(SkillTreeController tree) => tree.GetService<Player>().Level >= level;
    public string Describe(SkillTreeDefinition tree) => $"Requires player level {level}";
}

[Serializable]
public class DoublingCost : CostCurve
{
    public long first = 1;
    public override long GetCost(int currentLevel) => first << currentLevel;
}
```

`ctx.Get<T>()` and `controller.GetService<T>()` return a registered service (`controller.RegisterService(x)`), or find the first scene object of that type.

Most needs don't require a custom effect at all: use **Modify Stat** for numbers and **Unlock Feature** + a `SkillFeature` for behaviour.

A wallet is any component that implements `ICurrencyWallet` (`CanAfford`, `TrySpend`, `Add`, `BalanceChanged`). Nodes use the `"default"` currency unless you set another `currencyId`.

## UI module

`SkillTreeView` builds the tree at runtime from the definition, using `node.position × spacing`:

- **`SkillNodeView`** has the icon, level fill, level text, state tint, highlight, affordable indicator and button. Every part is optional. It also animates hover and purchase.
- **`ConnectorView`** is a UI-image line, shown once both of its ends are visible.
- **`NodeStateStyle`** sets the mystery icon and text, the tint per state, the connector colours, and the tag that gets a highlight (default `keystone`).
- **`SkillTooltip`** is one shared, constant-size tooltip. **`PanZoomView`** zooms toward the cursor and drags with inertia. **`PulseGlow`** pulses the highlight.

To restyle, swap the node prefab or the style asset. Tooltip text is generated from the name, flavour text, effect lines (current → next level), cost and anything still required.

## Editor tools

- **Graph** (double-click the tree asset, or Window ▸ SMPro ▸ Skill Tree Graph). Add, duplicate, delete and link nodes, then edit the selected node in the side panel. Cycles and duplicate links are refused. Undo works throughout.
- **Table** (Window ▸ SMPro ▸ Skill Tree Table). Edit max levels and cost values for every node in one view, see the cost to max everything, and export to CSV.
- **Validation** shows in the tree's Inspector, in the graph's status bar, and as console errors when the asset is imported.
- **Play-mode debugger** on `SkillTreeController`. Grant currency, buy or refund nodes, max or reset everything, copy or load save JSON, and inspect each stat with its modifiers and their source nodes.

## Notes

- Effects, conditions and cost curves are stored with `[SerializeReference]`. Before renaming or moving one of these classes, add `[UnityEngine.Scripting.APIUpdating.MovedFrom(...)]`, or saved trees lose that entry. The validator reports any entry whose class went missing.
- Stats live with the tree (`tree.Stats`). They exist before any controller starts, and they reset when entering Play mode, even with domain reload turned off.
