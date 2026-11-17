using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SMPro.SkillTree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SkillTreeImporter
{
    const string AssetPath = "Assets/SO/SkillTree/GladiatorPitsTree.asset";
    const string TreeId = "gladiator_pits";

    readonly struct StatSpec
    {
        public readonly string Id, Name;
        public readonly StatFormat Format;
        public readonly float Min, Max;

        public StatSpec(string id, string name, StatFormat format, float min = float.MinValue, float max = float.MaxValue)
        {
            Id = id;
            Name = name;
            Format = format;
            Min = min;
            Max = max;
        }
    }

    readonly struct AbilityStatSpec
    {
        public readonly Type Config;
        public readonly AbilityStat Stat;
        public readonly string Field;
        public readonly StatSpec Spec;

        public AbilityStatSpec(Type config, AbilityStat stat, string field, StatSpec spec)
        {
            Config = config;
            Stat = stat;
            Field = field;
            Spec = spec;
        }
    }

    static readonly Dictionary<string, StatSpec> VariableStats = new()
    {
        ["CoinMultiplier"] = new("coins.multiplier", "Coin Value", StatFormat.Percent),
        ["CoinBonusPerTier"] = new("coins.bonus_per_tier", "Coins per Animal Type", StatFormat.Percent),
        ["BonusPercentage"] = new("coins.round_end_bonus", "Round-End Bonus (%)", StatFormat.Number),
        ["AlphaChance"] = new("enemies.alpha_chance", "Alpha Chance", StatFormat.Percent),
        ["GoldenChance"] = new("enemies.golden_chance", "Golden Animal Chance", StatFormat.Percent),
        ["EnemyCount"] = new("enemies.count", "Animals per Round", StatFormat.Number),
        ["ASPS"] = new("gladiator.attack_interval", "Seconds per Attack", StatFormat.Number),
        ["GladiatorCount"] = new("gladiator.count", "Cavemen", StatFormat.Number),
        ["WeaponThrowCount"] = new("gladiator.throw_count", "Spears per Attack", StatFormat.Number),
        ["RoundTime"] = new("round.duration", "Round Length (s)", StatFormat.Number),
        ["Circle_Radius"] = new("targeting.radius", "Target Circle Size", StatFormat.Number),
        ["Dagger_Damage"] = new("spear.damage", "Spear Damage", StatFormat.Number),
        ["Dagger_BonusDamage"] = new("spear.bonus_damage", "Bonus Damage", StatFormat.Percent),
        ["Dagger_DamageMultiplier"] = new("spear.damage_multiplier", "Damage Multiplier", StatFormat.Number),
        ["Dagger_CritChance"] = new("spear.crit_chance", "Crit Chance", StatFormat.Percent),
        ["Dagger_CritDamage"] = new("spear.crit_multiplier", "Crit Damage", StatFormat.Number),
    };

    static readonly Dictionary<Type, string> FeatureIds = new()
    {
        [typeof(BreedingSeasonConfig)] = "breeding_season",
        [typeof(ChainLightningConfig)] = "chain_lightning",
        [typeof(DoubleCoinsOnKillConfig)] = "double_coins",
        [typeof(HuntersFrenzyConfig)] = "hunters_frenzy",
        [typeof(RicochetConfig)] = "ricochet",
        [typeof(EnemySpawnIntervalConfig)] = "spawn_interval",
    };

    static readonly AbilityStatSpec[] AbilityStats =
    {
        new(typeof(BreedingSeasonConfig), AbilityStat.Chance, "chance", new("breeding_season.chance", "Breeding Chance", StatFormat.Percent, 0f, 1f)),
        new(typeof(ChainLightningConfig), AbilityStat.Chance, "chance", new("chain_lightning.chance", "Chain Lightning Chance", StatFormat.Percent, 0f, 1f)),
        new(typeof(ChainLightningConfig), AbilityStat.ChainCount, "chainCount", new("chain_lightning.jumps", "Chain Lightning Jumps", StatFormat.Number, 0f)),
        new(typeof(ChainLightningConfig), AbilityStat.Damage, "damagePercent", new("chain_lightning.damage_percent", "Chain Lightning Damage", StatFormat.Percent, 0f)),
        new(typeof(DoubleCoinsOnKillConfig), AbilityStat.Chance, "chance", new("double_coins.chance", "Double Coins Chance", StatFormat.Percent, 0f, 1f)),
        new(typeof(HuntersFrenzyConfig), AbilityStat.KillThreshold, "killsToTrigger", new("hunters_frenzy.kill_threshold", "Kills per Frenzy", StatFormat.Number)),
        new(typeof(HuntersFrenzyConfig), AbilityStat.CoinBonus, "coinBonus", new("hunters_frenzy.coin_bonus", "Frenzy Coin Bonus", StatFormat.Percent, 0f)),
        new(typeof(RicochetConfig), AbilityStat.BounceCount, "bounceCount", new("ricochet.bounces", "Ricochet Bounces", StatFormat.Number, 0f)),
        new(typeof(RicochetConfig), AbilityStat.Damage, "bounceDamagePercent", new("ricochet.damage_percent", "Ricochet Damage", StatFormat.Percent, 0f)),
        new(typeof(RicochetConfig), AbilityStat.SplinterDamage, "splinterDamagePercent", new("ricochet.splinter_percent", "Splinter Damage", StatFormat.Percent, 0f)),
        new(typeof(EnemySpawnIntervalConfig), AbilityStat.Duration, "spawnInterval", new("spawn_interval.seconds", "Seconds per Extra Animal", StatFormat.Number, 0.1f)),
    };

    static readonly string[] IntegerAbilityStats = { "chain_lightning.jumps", "hunters_frenzy.kill_threshold", "ricochet.bounces" };

    [MenuItem("Tools/Gladiator Pits/Import Skill Tree From Scene")]
    static void ImportFromMenu()
    {
        string report = Import();
        Debug.Log(report);
    }

    public static string Import()
    {
        SkillNode[] sceneNodes = UnityEngine.Object.FindObjectsByType<SkillNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(n => n.transform.GetSiblingIndex())
            .ToArray();
        if (sceneNodes.Length == 0) return "[SkillTreeImporter] No SkillNode found in the open scene.";

        var errors = new List<string>();
        Dictionary<SkillNode, string> ids = BuildIds(sceneNodes, errors);
        List<StatDefinition> stats = BuildStats(errors);
        List<SkillNodeDefinition> nodes = sceneNodes.Select(n => BuildNode(n, ids, errors)).ToList();

        if (errors.Count > 0) return "[SkillTreeImporter] Import aborted, nothing was written:\n" + string.Join("\n", errors);

        SkillTreeDefinition tree = LoadOrCreateTree();
        tree.treeId = TreeId;
        tree.stats = stats;
        tree.nodes = nodes;
        tree.InvalidateLookups();
        EditorUtility.SetDirty(tree);
        AssetDatabase.SaveAssets();

        return Report(tree, sceneNodes, ids);
    }

    static Dictionary<SkillNode, string> BuildIds(SkillNode[] sceneNodes, List<string> errors)
    {
        var ids = new Dictionary<SkillNode, string>();
        foreach (IGrouping<string, SkillNode> group in sceneNodes.GroupBy(n => Slug(n.name)))
        {
            if (group.Count() > 1) errors.Add($"Nodes {string.Join(", ", group.Select(n => n.name))} share the id '{group.Key}'.");
            foreach (SkillNode node in group) ids[node] = group.Key;
        }
        return ids;
    }

    static List<StatDefinition> BuildStats(List<string> errors)
    {
        var stats = new List<StatDefinition>();

        foreach (KeyValuePair<string, StatSpec> entry in VariableStats)
        {
            ScriptableObject variable = FindAsset<ScriptableObject>(entry.Key, "FloatVariable", "IntVariable");
            if (variable == null)
            {
                errors.Add($"Variable asset '{entry.Key}' not found.");
                continue;
            }

            bool isInteger = variable is IntVariable;
            float baseValue = isInteger ? ((IntVariable)variable).DefaultValue : ((FloatVariable)variable).DefaultValue;
            stats.Add(Stat(entry.Value, baseValue, isInteger));
        }

        foreach (AbilityStatSpec ability in AbilityStats)
        {
            ScriptableObject config = FindAsset<ScriptableObject>(null, ability.Config.Name);
            if (config == null)
            {
                errors.Add($"No {ability.Config.Name} asset found.");
                continue;
            }

            SerializedProperty field = new SerializedObject(config).FindProperty(ability.Field);
            if (field == null)
            {
                errors.Add($"{ability.Config.Name} has no field '{ability.Field}'.");
                continue;
            }

            bool isInteger = IntegerAbilityStats.Contains(ability.Spec.Id);
            float baseValue = field.propertyType == SerializedPropertyType.Integer ? field.intValue : field.floatValue;
            StatDefinition stat = Stat(ability.Spec, baseValue, isInteger);

            if (ability.Spec.Id == "hunters_frenzy.kill_threshold")
                stat.min = new SerializedObject(config).FindProperty("minKillsToTrigger").intValue;

            stats.Add(stat);
        }

        return stats;
    }

    static StatDefinition Stat(StatSpec spec, float baseValue, bool isInteger) => new()
    {
        id = spec.Id,
        displayName = spec.Name,
        baseValue = baseValue,
        min = spec.Min,
        max = spec.Max,
        isInteger = isInteger,
        format = spec.Format
    };

    static SkillNodeDefinition BuildNode(SkillNode sceneNode, Dictionary<SkillNode, string> ids, List<string> errors)
    {
        var so = new SerializedObject(sceneNode);
        bool isMystery = so.FindProperty("isMystery").boolValue;

        var node = new SkillNodeDefinition
        {
            id = ids[sceneNode],
            displayName = so.FindProperty("displayName").stringValue,
            flavourText = so.FindProperty("effectDescription").stringValue,
            icon = FindIcon(sceneNode, so),
            position = ((RectTransform)sceneNode.transform).anchoredPosition,
            maxLevel = so.FindProperty("maxLevel").intValue,
            cost = new ExponentialCost
            {
                baseCost = so.FindProperty("baseCost").intValue,
                growth = so.FindProperty("costGrowth").floatValue
            },
            tags = isMystery ? new[] { "keystone" } : Array.Empty<string>()
        };

        SerializedProperty requirements = so.FindProperty("requirements");
        for (int i = 0; i < requirements.arraySize; i++)
        {
            SerializedProperty requirement = requirements.GetArrayElementAtIndex(i);
            var parent = requirement.FindPropertyRelative("node").objectReferenceValue as SkillNode;
            if (parent == null)
            {
                errors.Add($"{sceneNode.name}: requirement {i} has no node.");
                continue;
            }
            node.requirements.Add(new NodeRequirement { nodeId = ids[parent], level = requirement.FindPropertyRelative("level").intValue });
        }

        node.reveal = isMystery ? RevealRule.MysteryUntilAvailable
            : node.requirements.Count == 0 ? RevealRule.Always
            : RevealRule.WhenParentStarted;

        SerializedProperty effects = so.FindProperty("effects");
        for (int i = 0; i < effects.arraySize; i++)
        {
            var effect = effects.GetArrayElementAtIndex(i).objectReferenceValue as SkillEffect;
            if (effect == null)
            {
                errors.Add($"{sceneNode.name}: effect {i} is empty.");
                continue;
            }
            ISkillEffect converted = Convert(effect, out string error);
            if (converted == null) errors.Add($"{sceneNode.name}: {error}");
            else node.effects.Add(converted);
        }

        return node;
    }

    static Sprite FindIcon(SkillNode sceneNode, SerializedObject so)
    {
        var image = so.FindProperty("iconImage").objectReferenceValue as Image;
        if (image == null)
        {
            Transform icon = sceneNode.transform.Find("Icon");
            if (icon != null) image = icon.GetComponent<Image>();
        }
        return image != null ? image.sprite : null;
    }

    static ISkillEffect Convert(SkillEffect effect, out string error)
    {
        error = null;
        var so = new SerializedObject(effect);

        switch (effect)
        {
            case ModifyFloatStatEffect floatEffect:
                return ModifyVariable(effect.name, floatEffect.targetStat, floatEffect.operation, floatEffect.amount, out error);

            case ModifyIntStatEffect intEffect:
                return ModifyVariable(effect.name, intEffect.targetStat, intEffect.operation, intEffect.amount, out error);

            case UnlockAbilityEffect:
            {
                var config = so.FindProperty("ability").objectReferenceValue as AbilityConfiguration;
                if (config == null || !FeatureIds.TryGetValue(config.GetType(), out string featureId))
                {
                    error = $"{effect.name} unlocks an unknown ability.";
                    return null;
                }
                return new UnlockFeatureEffect { featureId = featureId };
            }

            case ModifyAbilityEffect:
            {
                var config = so.FindProperty("ability").objectReferenceValue as AbilityConfiguration;
                var stat = (AbilityStat)so.FindProperty("stat").intValue;
                var operation = (StatOperation)so.FindProperty("operation").intValue;
                float value = so.FindProperty("value").floatValue;

                AbilityStatSpec spec = AbilityStats.FirstOrDefault(a => config != null && a.Config == config.GetType() && a.Stat == stat);
                if (spec.Spec.Id == null)
                {
                    error = $"{effect.name} modifies {stat} on {(config ? config.name : "nothing")}, which has no stat mapping.";
                    return null;
                }
                return Modify(spec.Spec.Id, operation, value, effect.name, out error);
            }

            default:
                error = $"{effect.name} is a {effect.GetType().Name}, which the importer cannot convert.";
                return null;
        }
    }

    static ISkillEffect ModifyVariable(string effectName, ScriptableObject variable, StatOperation operation, float amount, out string error)
    {
        error = null;
        if (variable == null || !VariableStats.TryGetValue(variable.name, out StatSpec spec))
        {
            error = $"{effectName} targets '{(variable ? variable.name : "nothing")}', which has no stat mapping.";
            return null;
        }
        return Modify(spec.Id, operation, amount, effectName, out error);
    }

    static ISkillEffect Modify(string statId, StatOperation operation, float value, string effectName, out string error)
    {
        error = null;
        ModifierKind? kind = operation switch
        {
            StatOperation.Add => ModifierKind.Flat,
            StatOperation.Multiply => ModifierKind.Multiply,
            StatOperation.Set => ModifierKind.Override,
            _ => null
        };

        if (kind == null)
        {
            error = $"{effectName} uses {operation}, which has no equivalent modifier.";
            return null;
        }
        return new ModifyStatEffect { statId = statId, kind = kind.Value, valuePerLevel = value };
    }

    static SkillTreeDefinition LoadOrCreateTree()
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(AssetPath);
        if (tree != null) return tree;

        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
        tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        AssetDatabase.CreateAsset(tree, AssetPath);
        return tree;
    }

    static T FindAsset<T>(string name, params string[] typeNames) where T : UnityEngine.Object
    {
        foreach (string typeName in typeNames)
        {
            string filter = string.IsNullOrEmpty(name) ? $"t:{typeName}" : $"{name} t:{typeName}";
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && (name == null || asset.name == name)) return asset;
            }
        }
        return null;
    }

    static string Slug(string name) =>
        Regex.Replace(name.ToLowerInvariant().Replace("'", ""), "[^a-z0-9]+", "_").Trim('_');

    static string Report(SkillTreeDefinition tree, SkillNode[] sceneNodes, Dictionary<SkillNode, string> ids)
    {
        var report = new StringBuilder();
        report.AppendLine($"[SkillTreeImporter] Wrote {AssetPath}: {tree.nodes.Count} nodes, {tree.stats.Count} stats, {tree.nodes.Sum(n => n.effects.Count)} effects.");

        var problems = new List<string>();
        foreach (SkillNodeDefinition node in tree.nodes)
        {
            foreach (NodeRequirement requirement in node.requirements)
                if (!tree.TryGetNode(requirement.nodeId, out _)) problems.Add($"{node.id}: requires unknown node '{requirement.nodeId}'.");

            foreach (ISkillEffect effect in node.effects)
                problems.AddRange(effect.Validate(tree).Select(p => $"{node.id}: {p}"));

            if (node.icon == null) problems.Add($"{node.id}: no icon.");
        }

        int costChecks = 0;
        foreach (SkillNode sceneNode in sceneNodes)
        {
            tree.TryGetNode(ids[sceneNode], out SkillNodeDefinition node);
            var so = new SerializedObject(sceneNode);
            int baseCost = so.FindProperty("baseCost").intValue;
            float growth = so.FindProperty("costGrowth").floatValue;
            for (int level = 0; level < node.maxLevel; level++)
            {
                long oldCost = Mathf.CeilToInt(baseCost * Mathf.Pow(growth, level));
                long newCost = node.GetCost(level);
                costChecks++;
                if (oldCost != newCost) problems.Add($"{node.id}: level {level} costs {newCost}, was {oldCost}.");
            }
        }

        report.AppendLine($"Cost checks: {costChecks}, problems: {problems.Count}");
        foreach (string problem in problems) report.AppendLine(" - " + problem);
        return report.ToString();
    }
}
