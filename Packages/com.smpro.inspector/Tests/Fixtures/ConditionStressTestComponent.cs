using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class ConditionStressTestComponent : MonoBehaviour
    {
        public bool toggle;
        public int mode;
        [ShowIf(nameof(toggle))] public int field00;
        [EnableIf(nameof(mode), 2)] public float field01;
        [ShowIf(nameof(toggle))] public int field02;
        [EnableIf(nameof(mode), 2)] public float field03;
        [ShowIf(nameof(toggle))] public int field04;
        [EnableIf(nameof(mode), 2)] public float field05;
        [ShowIf(nameof(toggle))] public int field06;
        [EnableIf(nameof(mode), 2)] public float field07;
        [ShowIf(nameof(toggle))] public int field08;
        [EnableIf(nameof(mode), 2)] public float field09;
        [ShowIf(nameof(toggle))] public int field10;
        [EnableIf(nameof(mode), 2)] public float field11;
        [ShowIf(nameof(toggle))] public int field12;
        [EnableIf(nameof(mode), 2)] public float field13;
        [ShowIf(nameof(toggle))] public int field14;
        [EnableIf(nameof(mode), 2)] public float field15;
        [ShowIf(nameof(toggle))] public int field16;
        [EnableIf(nameof(mode), 2)] public float field17;
        [ShowIf(nameof(toggle))] public int field18;
        [EnableIf(nameof(mode), 2)] public float field19;
        [ShowIf(nameof(toggle))] public int field20;
        [EnableIf(nameof(mode), 2)] public float field21;
        [ShowIf(nameof(toggle))] public int field22;
        [EnableIf(nameof(mode), 2)] public float field23;
        [ShowIf(nameof(toggle))] public int field24;
        [EnableIf(nameof(mode), 2)] public float field25;
        [ShowIf(nameof(toggle))] public int field26;
        [EnableIf(nameof(mode), 2)] public float field27;
        [ShowIf(nameof(toggle))] public int field28;
        [EnableIf(nameof(mode), 2)] public float field29;
        [ShowIf(nameof(toggle))] public int field30;
        [EnableIf(nameof(mode), 2)] public float field31;
        [ShowIf(nameof(toggle))] public int field32;
        [EnableIf(nameof(mode), 2)] public float field33;
        [ShowIf(nameof(toggle))] public int field34;
        [EnableIf(nameof(mode), 2)] public float field35;
        [ShowIf(nameof(toggle))] public int field36;
        [EnableIf(nameof(mode), 2)] public float field37;
        [ShowIf(nameof(toggle))] public int field38;
        [EnableIf(nameof(mode), 2)] public float field39;
        [ShowIf(nameof(toggle))] public int field40;
        [EnableIf(nameof(mode), 2)] public float field41;
        [ShowIf(nameof(toggle))] public int field42;
        [EnableIf(nameof(mode), 2)] public float field43;
        [ShowIf(nameof(toggle))] public int field44;
        [EnableIf(nameof(mode), 2)] public float field45;
        [ShowIf(nameof(toggle))] public int field46;
        [EnableIf(nameof(mode), 2)] public float field47;
        [ShowIf(nameof(toggle))] public int field48;
        [EnableIf(nameof(mode), 2)] public float field49;
    }
}
