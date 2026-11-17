using System;
using System.Diagnostics;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEngine.TestTools.Constraints;
using Debug = UnityEngine.Debug;

#pragma warning disable CS0169, CS0414, CS0649

namespace SMPro.Inspector.Tests
{
    public class MemberResolverTests
    {
        enum Mode
        {
            Simple,
            Advanced,
            Expert
        }

        [Flags]
        enum Layers
        {
            None = 0,
            Ground = 1,
            Air = 2,
            Water = 4
        }

        class Settings
        {
            public bool enabled;
        }

        class SampleBase
        {
            bool inheritedPrivate = true;
        }

        class Sample : SampleBase
        {
            public bool flag;
            bool privateFlag;
            public int count;
            public float speed;
            public Mode mode;
            public Layers layers;
            public string title;
            public Settings settings = new();
            public Settings missing;
            public UnityEngine.Object reference;
            public static bool staticFlag;

            public bool Property => count > 2;
            bool PrivateMethod() => count > 5;
            bool IsPositive(int value) => value > 0;
            bool TooMany(int a, int b) => a > b;
            void Act() { }

            public void SetPrivateFlag(bool value) => privateFlag = value;
        }

        static readonly Type T = typeof(Sample);

        [TearDown]
        public void ResetStatics() => Sample.staticFlag = false;

        [Test]
        public void Field_Property_AndMethod_AreRead()
        {
            var sample = new Sample { flag = true, count = 6 };

            Assert.IsTrue(MemberResolver.Value<bool>(T, "flag").Get(sample));
            Assert.IsTrue(MemberResolver.Value<bool>(T, "Property").Get(sample));
            Assert.IsTrue(MemberResolver.Value<bool>(T, "PrivateMethod").Get(sample));
            Assert.AreEqual(6, MemberResolver.Value<int>(T, "count").Get(sample));
        }

        [Test]
        public void PrivateInheritedAndStaticMembers_AreRead()
        {
            var sample = new Sample();
            sample.SetPrivateFlag(true);
            Sample.staticFlag = true;

            Assert.IsTrue(MemberResolver.Value<bool>(T, "privateFlag").Get(sample));
            Assert.IsTrue(MemberResolver.Value<bool>(T, "inheritedPrivate").Get(sample));
            Assert.IsTrue(MemberResolver.Value<bool>(T, "staticFlag").Get(sample));
        }

        [Test]
        public void Negation_InvertsABool()
        {
            Getter<bool> notFlag = MemberResolver.Value<bool>(T, "!flag");

            Assert.IsTrue(notFlag.Get(new Sample { flag = false }));
            Assert.IsFalse(notFlag.Get(new Sample { flag = true }));
        }

        [Test]
        public void NestedPath_IsRead_AndANullLinkGivesDefault()
        {
            var sample = new Sample();
            sample.settings.enabled = true;

            Assert.IsTrue(MemberResolver.Value<bool>(T, "settings.enabled").Get(sample));
            Assert.IsFalse(MemberResolver.Value<bool>(T, "missing.enabled").Get(sample));
        }

        [Test]
        public void MethodWithAParameter_ReceivesTheArgument()
        {
            Getter<bool> isPositive = MemberResolver.Value<bool>(T, "IsPositive");
            var sample = new Sample();

            Assert.IsTrue(isPositive.Get(sample, 3));
            Assert.IsFalse(isPositive.Get(sample, -1));
            Assert.IsFalse(isPositive.Get(sample, null));
        }

        [Test]
        public void Numbers_ConvertToTheRequestedNumericType()
        {
            Assert.AreEqual(3f, MemberResolver.Value<float>(T, "count").Get(new Sample { count = 3 }));
        }

        [Test]
        public void Condition_MatchesAnEnumValue()
        {
            Getter<bool> advanced = MemberResolver.Condition(T, "mode", Mode.Advanced);

            Assert.IsTrue(advanced.Get(new Sample { mode = Mode.Advanced }));
            Assert.IsFalse(advanced.Get(new Sample { mode = Mode.Simple }));
        }

        [Test]
        public void Condition_MatchesAnyOfSeveralValues()
        {
            Getter<bool> notSimple = MemberResolver.Condition(T, "mode", Mode.Advanced, Mode.Expert);

            Assert.IsTrue(notSimple.Get(new Sample { mode = Mode.Expert }));
            Assert.IsFalse(notSimple.Get(new Sample { mode = Mode.Simple }));
        }

        [Test]
        public void Condition_OnFlags_MatchesWhenAnyFlagIsSet()
        {
            var sample = new Sample { layers = Layers.Air | Layers.Water };

            Assert.IsTrue(MemberResolver.Condition(T, "layers", Layers.Water).Get(sample));
            Assert.IsTrue(MemberResolver.Condition(T, "layers", Layers.Ground, Layers.Air).Get(sample));
            Assert.IsFalse(MemberResolver.Condition(T, "layers", Layers.Ground).Get(sample));
            Assert.IsFalse(MemberResolver.Condition(T, "layers", Layers.None).Get(sample));
            Assert.IsTrue(MemberResolver.Condition(T, "layers", Layers.None).Get(new Sample()));
        }

        [Test]
        public void Condition_ComparesNumbersStringsAndNull()
        {
            Assert.IsTrue(MemberResolver.Condition(T, "count", 3).Get(new Sample { count = 3 }));
            Assert.IsTrue(MemberResolver.Condition(T, "speed", 2).Get(new Sample { speed = 2f }));
            Assert.IsTrue(MemberResolver.Condition(T, "title", "boss").Get(new Sample { title = "boss" }));
            Assert.IsFalse(MemberResolver.Condition(T, "title", "boss").Get(new Sample { title = "minion" }));
            Assert.IsTrue(MemberResolver.Condition(T, "reference", (object)null).Get(new Sample()));
        }

        [Test]
        public void Condition_CanBeNegated()
        {
            Getter<bool> notSimple = MemberResolver.Condition(T, "!mode", Mode.Simple);

            Assert.IsTrue(notSimple.Get(new Sample { mode = Mode.Expert }));
            Assert.IsFalse(notSimple.Get(new Sample { mode = Mode.Simple }));
        }

        [Test]
        public void UnknownMember_GivesAnError_AndGetReturnsDefault()
        {
            Getter<bool> getter = MemberResolver.Value<bool>(T, "flg");

            Assert.IsFalse(getter.IsValid);
            Assert.AreEqual("No member 'flg' on Sample", getter.Error);
            Assert.IsFalse(getter.Get(new Sample { flag = true }));
        }

        [Test]
        public void WrongType_GivesAnError()
        {
            Assert.AreEqual("'count' is int, expected bool", MemberResolver.Value<bool>(T, "count").Error);
            Assert.AreEqual("'Act' is void, expected bool", MemberResolver.Value<bool>(T, "Act").Error);
            Assert.AreEqual("'!count' needs a bool, but 'count' is int", MemberResolver.Value<bool>(T, "!count").Error);
        }

        [Test]
        public void UnsupportedShapes_GiveErrors()
        {
            Assert.AreEqual("'TooMany' takes 2 parameters; at most 1 is supported", MemberResolver.Value<bool>(T, "TooMany").Error);
            Assert.AreEqual("'IsPositive' takes a parameter, so it must be the last part of 'IsPositive.x'", MemberResolver.Value<bool>(T, "IsPositive.x").Error);
            Assert.AreEqual("Empty member name on Sample", MemberResolver.Value<bool>(T, " ").Error);
            Assert.AreEqual("No member 'nothing' on Settings", MemberResolver.Value<bool>(T, "settings.nothing").Error);
        }

        [Test]
        public void ImpossibleComparison_GivesAnError()
        {
            Assert.AreEqual("Can't compare 'count' (int) with \"abc\" (string)", MemberResolver.Condition(T, "count", "abc").Error);
            Assert.AreEqual("Can't compare 'count' (int) with null", MemberResolver.Condition(T, "count", (object)null).Error);
            Assert.AreEqual("Can't compare 'mode' (Mode) with 1 (int)", MemberResolver.Condition(T, "mode", 1).Error);
        }

        [Test]
        public void Getters_DoNotAllocate()
        {
            var sample = new Sample { mode = Mode.Advanced, layers = Layers.Air };
            Getter<bool> flag = MemberResolver.Value<bool>(T, "!flag");
            Getter<bool> nested = MemberResolver.Value<bool>(T, "settings.enabled");
            Getter<bool> mode = MemberResolver.Condition(T, "mode", Mode.Advanced, Mode.Expert);
            Getter<bool> layers = MemberResolver.Condition(T, "layers", Layers.Air);
            Getter<float> speed = MemberResolver.Value<float>(T, "count");
            bool sink = false;

            void ReadAll()
            {
                for (int i = 0; i < 10000; i++)
                    sink ^= flag.Get(sample) ^ nested.Get(sample) ^ mode.Get(sample) ^ layers.Get(sample) ^ speed.Get(sample) > 1f;
            }

            ReadAll();
            Assert.That(ReadAll, UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Getters_AreFast()
        {
            const int calls = 1000000;
            var sample = new Sample { mode = Mode.Advanced };
            Getter<bool> mode = MemberResolver.Condition(T, "mode", Mode.Advanced, Mode.Expert);
            bool sink = false;
            for (int i = 0; i < 1000; i++) sink ^= mode.Get(sample);

            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < calls; i++) sink ^= mode.Get(sample);
            watch.Stop();

            double microseconds = watch.Elapsed.TotalMilliseconds * 1000.0 / calls;
            Debug.Log($"MemberResolver condition getter: {microseconds:0.0000} µs per call ({sink})");
            Assert.Less(microseconds, 0.5);
        }
    }
}
