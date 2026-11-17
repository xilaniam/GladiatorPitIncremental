using System;
using System.Collections.Generic;
using SMPro.Inspector;
using UnityEngine;

namespace SMPro.Inspector.Samples
{
    public class ShowcaseSpawner : MonoBehaviour
    {
        public enum SpawnShape
        {
            Point,
            Circle,
            Line
        }

        [Serializable]
        public class Wave
        {
            public string name = "Wave";
            [Required] public EnemyProfile enemy;
            [MinValue(1)] public int count = 5;
            public bool hasBoss;
            [ShowIf(nameof(hasBoss)), Required] public EnemyProfile boss;
        }

        [Serializable]
        public struct Drop
        {
            public string item;
            [MinValue(0), MaxValue(1)] public float chance;
            [MinValue(1)] public int amount;
        }

        [Title("Spawner", "Every attribute in this component is from SMInspectorPro")]
        [BoxGroup("Setup"), InlineEditor] public EnemyProfile defaultEnemy;
        [BoxGroup("Setup"), EnumToggleButtons] public SpawnShape shape;
        [BoxGroup("Setup"), ShowIf(nameof(shape), SpawnShape.Circle), MinValue(0.5), Suffix("m")] public float radius = 5f;
        [BoxGroup("Setup"), ShowIf(nameof(shape), SpawnShape.Line), HorizontalGroup("Setup/Line")] public Vector3 lineStart;
        [BoxGroup("Setup"), ShowIf(nameof(shape), SpawnShape.Line), HorizontalGroup("Setup/Line")] public Vector3 lineEnd = Vector3.right * 10f;

        [FoldoutGroup("Waves", true), ListSettings(ElementLabel = nameof(Wave.name))] public List<Wave> waves = new();
        [FoldoutGroup("Loot"), TableList] public List<Drop> drops = new();
        [FoldoutGroup("Loot")] public SerializableDictionary<string, int> scoreTable = new();

        [FoldoutGroup("Effects"), SerializeReference, TypePicker] public IShowcaseEffect onHitEffect;

        [FoldoutGroup("Advanced"), OnValueChanged(nameof(ClampSeed)), InfoBox("Seed 0 picks a random seed on start", InfoMessageType.Info, nameof(UsesRandomSeed))]
        public int seed;
        [FoldoutGroup("Advanced"), FilePath(Extensions = "json")] public string exportPath;
        [FoldoutGroup("Advanced"), ReadOnly] public int timesSpawned;

        [ShowInInspector, BoxGroup("Runtime")] int AliveEnemies => alive;
        [ShowInInspector, BoxGroup("Runtime")] string Effect => onHitEffect?.Describe() ?? "None";

        int alive;

        bool UsesRandomSeed => seed == 0;

        void ClampSeed()
        {
            if (seed < 0) seed = 0;
        }

        [Button(ButtonSize.Large), BoxGroup("Runtime")]
        void SpawnWave()
        {
            timesSpawned++;
            alive += waves.Count > 0 ? waves[0].count : 1;
        }

        [Button, BoxGroup("Runtime")]
        void ClearEnemies(bool resetCounter = false)
        {
            alive = 0;
            if (resetCounter) timesSpawned = 0;
        }
    }
}
