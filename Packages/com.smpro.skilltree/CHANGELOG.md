# Changelog

## [1.0.0] - 2026-11-17

First release.

### Runtime
- `SkillTreeDefinition` asset holding nodes, stats, requirements, inline effects, conditions and cost curves.
- Computed stats with Flat, PercentAdd, Multiply and Override modifiers, clamping, integer rounding, change events and temporary modifiers. `StatRef` reads a stat from any component or prefab.
- Effects: Modify Stat, Unlock Feature, Grant Tag, Unity Event, Composite. Conditions: Total Points Spent, Tag Owned, Stat At Least. Cost curves: Flat, Linear, Exponential (exact decimal maths), Table.
- `SkillTreeController`: purchase checks, purchase, single-node refund, full respec with partial refunds, node states and reveal rules, tags, services for custom effects, capture and restore of `{nodeId: level}` saves, optional PlayerPrefs or file save provider.
- `ICurrencyWallet` (any component can be the wallet) and `SimpleWallet`.
- `SkillFeature` base class for behaviour switched on by nodes, including features spawned after the unlock.
- `SkillTreeValidation` for ids, requirements, cycles, missing classes, stats and icons.

### UI (needs PrimeTween)
- `SkillTreeView` builds node views and connectors from the definition, with tooltip, pan and zoom, highlight pulse and a `NodeStateStyle` asset for the look.

### Editor
- Graph window, balance table with CSV export, tree inspector with validation, type pickers for effects, conditions, costs and save providers, stat and node id dropdowns, and a play-mode controller inspector.

### Samples
- Starter Tree.
