using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class StressTestComponent : MonoBehaviour
    {
        [Serializable]
        public class Item
        {
            public bool active;
            [ShowIf(nameof(active))] public int power;
            [MinValue(0)] public float weight;
        }

        [Serializable]
        public class Level0
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level1 next = new();
        }

        [Serializable]
        public class Level1
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level2 next = new();
        }

        [Serializable]
        public class Level2
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level3 next = new();
        }

        [Serializable]
        public class Level3
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level4 next = new();
        }

        [Serializable]
        public class Level4
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level5 next = new();
        }

        [Serializable]
        public class Level5
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level6 next = new();
        }

        [Serializable]
        public class Level6
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level7 next = new();
        }

        [Serializable]
        public class Level7
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level8 next = new();
        }

        [Serializable]
        public class Level8
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
            public Level9 next = new();
        }

        [Serializable]
        public class Level9
        {
            [BoxGroup("Values")] public int value;
            [BoxGroup("Values"), ShowIf(nameof(value), 1)] public float detail;
        }

        public bool toggle;
        [BoxGroup("Group0"), ShowIf(nameof(toggle))] public int field000;
        [BoxGroup("Group0")] public float field001;
        [BoxGroup("Group0"), Title("Title 002")] public string field002;
        [FoldoutGroup("Fold0")] public bool field003;
        [BoxGroup("Group0"), ShowIf(nameof(toggle))] public Vector3 field004;
        [BoxGroup("Group0")] public int field005;
        [BoxGroup("Group0"), Title("Title 006")] public float field006;
        [FoldoutGroup("Fold0")] public string field007;
        [BoxGroup("Group0"), ShowIf(nameof(toggle))] public bool field008;
        [BoxGroup("Group0")] public Vector3 field009;
        [BoxGroup("Group0"), Title("Title 010")] public int field010;
        [FoldoutGroup("Fold0")] public float field011;
        [BoxGroup("Group0"), ShowIf(nameof(toggle))] public string field012;
        [BoxGroup("Group0")] public bool field013;
        [BoxGroup("Group0"), Title("Title 014")] public Vector3 field014;
        [FoldoutGroup("Fold0")] public int field015;
        [BoxGroup("Group0"), ShowIf(nameof(toggle))] public float field016;
        [BoxGroup("Group0")] public string field017;
        [BoxGroup("Group0"), Title("Title 018")] public bool field018;
        [FoldoutGroup("Fold0")] public Vector3 field019;
        [BoxGroup("Group1"), ShowIf(nameof(toggle))] public int field020;
        [BoxGroup("Group1")] public float field021;
        [BoxGroup("Group1"), Title("Title 022")] public string field022;
        [FoldoutGroup("Fold1")] public bool field023;
        [BoxGroup("Group1"), ShowIf(nameof(toggle))] public Vector3 field024;
        [BoxGroup("Group1")] public int field025;
        [BoxGroup("Group1"), Title("Title 026")] public float field026;
        [FoldoutGroup("Fold1")] public string field027;
        [BoxGroup("Group1"), ShowIf(nameof(toggle))] public bool field028;
        [BoxGroup("Group1")] public Vector3 field029;
        [BoxGroup("Group1"), Title("Title 030")] public int field030;
        [FoldoutGroup("Fold1")] public float field031;
        [BoxGroup("Group1"), ShowIf(nameof(toggle))] public string field032;
        [BoxGroup("Group1")] public bool field033;
        [BoxGroup("Group1"), Title("Title 034")] public Vector3 field034;
        [FoldoutGroup("Fold1")] public int field035;
        [BoxGroup("Group1"), ShowIf(nameof(toggle))] public float field036;
        [BoxGroup("Group1")] public string field037;
        [BoxGroup("Group1"), Title("Title 038")] public bool field038;
        [FoldoutGroup("Fold1")] public Vector3 field039;
        [BoxGroup("Group2"), ShowIf(nameof(toggle))] public int field040;
        [BoxGroup("Group2")] public float field041;
        [BoxGroup("Group2"), Title("Title 042")] public string field042;
        [FoldoutGroup("Fold2")] public bool field043;
        [BoxGroup("Group2"), ShowIf(nameof(toggle))] public Vector3 field044;
        [BoxGroup("Group2")] public int field045;
        [BoxGroup("Group2"), Title("Title 046")] public float field046;
        [FoldoutGroup("Fold2")] public string field047;
        [BoxGroup("Group2"), ShowIf(nameof(toggle))] public bool field048;
        [BoxGroup("Group2")] public Vector3 field049;
        [BoxGroup("Group2"), Title("Title 050")] public int field050;
        [FoldoutGroup("Fold2")] public float field051;
        [BoxGroup("Group2"), ShowIf(nameof(toggle))] public string field052;
        [BoxGroup("Group2")] public bool field053;
        [BoxGroup("Group2"), Title("Title 054")] public Vector3 field054;
        [FoldoutGroup("Fold2")] public int field055;
        [BoxGroup("Group2"), ShowIf(nameof(toggle))] public float field056;
        [BoxGroup("Group2")] public string field057;
        [BoxGroup("Group2"), Title("Title 058")] public bool field058;
        [FoldoutGroup("Fold2")] public Vector3 field059;
        [BoxGroup("Group3"), ShowIf(nameof(toggle))] public int field060;
        [BoxGroup("Group3")] public float field061;
        [BoxGroup("Group3"), Title("Title 062")] public string field062;
        [FoldoutGroup("Fold3")] public bool field063;
        [BoxGroup("Group3"), ShowIf(nameof(toggle))] public Vector3 field064;
        [BoxGroup("Group3")] public int field065;
        [BoxGroup("Group3"), Title("Title 066")] public float field066;
        [FoldoutGroup("Fold3")] public string field067;
        [BoxGroup("Group3"), ShowIf(nameof(toggle))] public bool field068;
        [BoxGroup("Group3")] public Vector3 field069;
        [BoxGroup("Group3"), Title("Title 070")] public int field070;
        [FoldoutGroup("Fold3")] public float field071;
        [BoxGroup("Group3"), ShowIf(nameof(toggle))] public string field072;
        [BoxGroup("Group3")] public bool field073;
        [BoxGroup("Group3"), Title("Title 074")] public Vector3 field074;
        [FoldoutGroup("Fold3")] public int field075;
        [BoxGroup("Group3"), ShowIf(nameof(toggle))] public float field076;
        [BoxGroup("Group3")] public string field077;
        [BoxGroup("Group3"), Title("Title 078")] public bool field078;
        [FoldoutGroup("Fold3")] public Vector3 field079;
        [BoxGroup("Group4"), ShowIf(nameof(toggle))] public int field080;
        [BoxGroup("Group4")] public float field081;
        [BoxGroup("Group4"), Title("Title 082")] public string field082;
        [FoldoutGroup("Fold4")] public bool field083;
        [BoxGroup("Group4"), ShowIf(nameof(toggle))] public Vector3 field084;
        [BoxGroup("Group4")] public int field085;
        [BoxGroup("Group4"), Title("Title 086")] public float field086;
        [FoldoutGroup("Fold4")] public string field087;
        [BoxGroup("Group4"), ShowIf(nameof(toggle))] public bool field088;
        [BoxGroup("Group4")] public Vector3 field089;
        [BoxGroup("Group4"), Title("Title 090")] public int field090;
        [FoldoutGroup("Fold4")] public float field091;
        [BoxGroup("Group4"), ShowIf(nameof(toggle))] public string field092;
        [BoxGroup("Group4")] public bool field093;
        [BoxGroup("Group4"), Title("Title 094")] public Vector3 field094;
        [FoldoutGroup("Fold4")] public int field095;
        [BoxGroup("Group4"), ShowIf(nameof(toggle))] public float field096;
        [BoxGroup("Group4")] public string field097;
        [BoxGroup("Group4"), Title("Title 098")] public bool field098;
        [FoldoutGroup("Fold4")] public Vector3 field099;
        [BoxGroup("Group5"), ShowIf(nameof(toggle))] public int field100;
        [BoxGroup("Group5")] public float field101;
        [BoxGroup("Group5"), Title("Title 102")] public string field102;
        [FoldoutGroup("Fold5")] public bool field103;
        [BoxGroup("Group5"), ShowIf(nameof(toggle))] public Vector3 field104;
        [BoxGroup("Group5")] public int field105;
        [BoxGroup("Group5"), Title("Title 106")] public float field106;
        [FoldoutGroup("Fold5")] public string field107;
        [BoxGroup("Group5"), ShowIf(nameof(toggle))] public bool field108;
        [BoxGroup("Group5")] public Vector3 field109;
        [BoxGroup("Group5"), Title("Title 110")] public int field110;
        [FoldoutGroup("Fold5")] public float field111;
        [BoxGroup("Group5"), ShowIf(nameof(toggle))] public string field112;
        [BoxGroup("Group5")] public bool field113;
        [BoxGroup("Group5"), Title("Title 114")] public Vector3 field114;
        [FoldoutGroup("Fold5")] public int field115;
        [BoxGroup("Group5"), ShowIf(nameof(toggle))] public float field116;
        [BoxGroup("Group5")] public string field117;
        [BoxGroup("Group5"), Title("Title 118")] public bool field118;
        [FoldoutGroup("Fold5")] public Vector3 field119;
        [BoxGroup("Group6"), ShowIf(nameof(toggle))] public int field120;
        [BoxGroup("Group6")] public float field121;
        [BoxGroup("Group6"), Title("Title 122")] public string field122;
        [FoldoutGroup("Fold6")] public bool field123;
        [BoxGroup("Group6"), ShowIf(nameof(toggle))] public Vector3 field124;
        [BoxGroup("Group6")] public int field125;
        [BoxGroup("Group6"), Title("Title 126")] public float field126;
        [FoldoutGroup("Fold6")] public string field127;
        [BoxGroup("Group6"), ShowIf(nameof(toggle))] public bool field128;
        [BoxGroup("Group6")] public Vector3 field129;
        [BoxGroup("Group6"), Title("Title 130")] public int field130;
        [FoldoutGroup("Fold6")] public float field131;
        [BoxGroup("Group6"), ShowIf(nameof(toggle))] public string field132;
        [BoxGroup("Group6")] public bool field133;
        [BoxGroup("Group6"), Title("Title 134")] public Vector3 field134;
        [FoldoutGroup("Fold6")] public int field135;
        [BoxGroup("Group6"), ShowIf(nameof(toggle))] public float field136;
        [BoxGroup("Group6")] public string field137;
        [BoxGroup("Group6"), Title("Title 138")] public bool field138;
        [FoldoutGroup("Fold6")] public Vector3 field139;
        [BoxGroup("Group7"), ShowIf(nameof(toggle))] public int field140;
        [BoxGroup("Group7")] public float field141;
        [BoxGroup("Group7"), Title("Title 142")] public string field142;
        [FoldoutGroup("Fold7")] public bool field143;
        [BoxGroup("Group7"), ShowIf(nameof(toggle))] public Vector3 field144;
        [BoxGroup("Group7")] public int field145;
        [BoxGroup("Group7"), Title("Title 146")] public float field146;
        [FoldoutGroup("Fold7")] public string field147;
        [BoxGroup("Group7"), ShowIf(nameof(toggle))] public bool field148;
        [BoxGroup("Group7")] public Vector3 field149;
        [BoxGroup("Group7"), Title("Title 150")] public int field150;
        [FoldoutGroup("Fold7")] public float field151;
        [BoxGroup("Group7"), ShowIf(nameof(toggle))] public string field152;
        [BoxGroup("Group7")] public bool field153;
        [BoxGroup("Group7"), Title("Title 154")] public Vector3 field154;
        [FoldoutGroup("Fold7")] public int field155;
        [BoxGroup("Group7"), ShowIf(nameof(toggle))] public float field156;
        [BoxGroup("Group7")] public string field157;
        [BoxGroup("Group7"), Title("Title 158")] public bool field158;
        [FoldoutGroup("Fold7")] public Vector3 field159;
        [BoxGroup("Group8"), ShowIf(nameof(toggle))] public int field160;
        [BoxGroup("Group8")] public float field161;
        [BoxGroup("Group8"), Title("Title 162")] public string field162;
        [FoldoutGroup("Fold8")] public bool field163;
        [BoxGroup("Group8"), ShowIf(nameof(toggle))] public Vector3 field164;
        [BoxGroup("Group8")] public int field165;
        [BoxGroup("Group8"), Title("Title 166")] public float field166;
        [FoldoutGroup("Fold8")] public string field167;
        [BoxGroup("Group8"), ShowIf(nameof(toggle))] public bool field168;
        [BoxGroup("Group8")] public Vector3 field169;
        [BoxGroup("Group8"), Title("Title 170")] public int field170;
        [FoldoutGroup("Fold8")] public float field171;
        [BoxGroup("Group8"), ShowIf(nameof(toggle))] public string field172;
        [BoxGroup("Group8")] public bool field173;
        [BoxGroup("Group8"), Title("Title 174")] public Vector3 field174;
        [FoldoutGroup("Fold8")] public int field175;
        [BoxGroup("Group8"), ShowIf(nameof(toggle))] public float field176;
        [BoxGroup("Group8")] public string field177;
        [BoxGroup("Group8"), Title("Title 178")] public bool field178;
        [FoldoutGroup("Fold8")] public Vector3 field179;
        [BoxGroup("Group9"), ShowIf(nameof(toggle))] public int field180;
        [BoxGroup("Group9")] public float field181;
        [BoxGroup("Group9"), Title("Title 182")] public string field182;
        [FoldoutGroup("Fold9")] public bool field183;
        [BoxGroup("Group9"), ShowIf(nameof(toggle))] public Vector3 field184;
        [BoxGroup("Group9")] public int field185;
        [BoxGroup("Group9"), Title("Title 186")] public float field186;
        [FoldoutGroup("Fold9")] public string field187;
        [BoxGroup("Group9"), ShowIf(nameof(toggle))] public bool field188;
        [BoxGroup("Group9")] public Vector3 field189;
        [BoxGroup("Group9"), Title("Title 190")] public int field190;
        [FoldoutGroup("Fold9")] public float field191;
        [BoxGroup("Group9"), ShowIf(nameof(toggle))] public string field192;
        [BoxGroup("Group9")] public bool field193;
        [BoxGroup("Group9"), Title("Title 194")] public Vector3 field194;
        [FoldoutGroup("Fold9")] public int field195;
        [BoxGroup("Group9"), ShowIf(nameof(toggle))] public float field196;
        [BoxGroup("Group9")] public string field197;
        [BoxGroup("Group9"), Title("Title 198")] public bool field198;
        [FoldoutGroup("Fold9")] public Vector3 field199;
        public Level0 nested = new();
        public List<Item> items = new();
    }
}
