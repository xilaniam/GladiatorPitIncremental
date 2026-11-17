using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class ListTestComponent : MonoBehaviour
    {
        [Serializable]
        public class Enemy
        {
            public string name;
            public bool boss;
            [ShowIf(nameof(boss))] public int bossHealth;
            [Required] public GameObject prefab;
        }

        [Serializable]
        public class Row
        {
            public int id;
            public string label;
            [MinValue(0)] public float weight;
        }

        public List<Enemy> enemies = new() { new Enemy { name = "Goblin" }, new Enemy { name = "Orc", boss = true } };
        [ListSettings(ElementLabel = nameof(Enemy.name))] public Enemy[] named = { new Enemy { name = "Bat" } };
        [ListSettings(Draggable = false, ShowAddRemove = false)] public int[] fixedNumbers = { 1, 2, 3 };
        public List<int> plainNumbers = new() { 4, 5 };
        [TableList] public List<Row> rows = new() { new Row { id = 1, label = "a" }, new Row { id = 2, label = "b" } };
        [TableList] public List<int> notATable = new() { 1 };
    }
}
