using System;
using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEngine;

#pragma warning disable CS0169, CS0414, CS0649

namespace SMPro.Inspector.Tests
{
    public class TypeModelTests
    {
        class MarkerAttribute : InspectorAttribute
        {
        }

        class OtherMarkerAttribute : InspectorAttribute
        {
        }

        class BaseSample
        {
            public int baseField;
            [SerializeField] int basePrivate;
        }

        class DerivedSample : BaseSample
        {
            public int first;
            [SerializeField] int second;
            [SerializeReference] object third;
            int notSerialized;
            public static int staticField;
            public const int Constant = 1;
            public readonly int readOnly;
            [NonSerialized] public int nonSerialized;
            [Marker] int markedPrivate;

            [Marker] public int MarkedProperty => 1;
            public int UnmarkedProperty => 1;

            [Marker] void MarkedMethod() { }
            void UnmarkedMethod() { }
        }

        class PlainSample
        {
            public int value;
        }

        [Marker]
        class MarkedClass
        {
            public int value;
        }

        class SampleBehaviour : MonoBehaviour
        {
            public float speed;
        }

        static string[] Names(Type type) => TypeModel.For(type).Members.Select(member => member.Name).ToArray();

        [Test]
        public void Members_ListBaseClassFirst_ThenFieldsPropertiesMethods()
        {
            CollectionAssert.AreEqual(
                new[] { "baseField", "basePrivate", "first", "second", "third", "markedPrivate", "MarkedProperty", "MarkedMethod" },
                Names(typeof(DerivedSample)));
        }

        [Test]
        public void SerializedFields_FollowUnityRules()
        {
            TypeModel model = TypeModel.For(typeof(DerivedSample));

            Assert.AreEqual(MemberKind.SerializedField, model.Find("first").Kind);
            Assert.AreEqual(MemberKind.SerializedField, model.Find("second").Kind);
            Assert.AreEqual(MemberKind.SerializedField, model.Find("third").Kind);
            Assert.AreEqual(MemberKind.SerializedField, model.Find("basePrivate").Kind);
            Assert.IsNull(model.Find("notSerialized"));
            Assert.IsNull(model.Find("staticField"));
            Assert.IsNull(model.Find("Constant"));
            Assert.IsNull(model.Find("readOnly"));
            Assert.IsNull(model.Find("nonSerialized"));
        }

        [Test]
        public void AttributedMembers_AreCollectedWithTheirKind()
        {
            TypeModel model = TypeModel.For(typeof(DerivedSample));

            Assert.AreEqual(MemberKind.Field, model.Find("markedPrivate").Kind);
            Assert.AreEqual(MemberKind.Property, model.Find("MarkedProperty").Kind);
            Assert.AreEqual(MemberKind.Method, model.Find("MarkedMethod").Kind);
            Assert.IsNull(model.Find("UnmarkedProperty"));
            Assert.IsNull(model.Find("UnmarkedMethod"));
        }

        [Test]
        public void ValueType_IsTheFieldPropertyOrReturnType()
        {
            TypeModel model = TypeModel.For(typeof(DerivedSample));

            Assert.AreEqual(typeof(int), model.Find("first").ValueType);
            Assert.AreEqual(typeof(object), model.Find("third").ValueType);
            Assert.AreEqual(typeof(int), model.Find("MarkedProperty").ValueType);
            Assert.AreEqual(typeof(void), model.Find("MarkedMethod").ValueType);
        }

        [Test]
        public void Attributes_AreFoundByType()
        {
            MemberModel member = TypeModel.For(typeof(DerivedSample)).Find("markedPrivate");

            Assert.IsTrue(member.Has<MarkerAttribute>());
            Assert.IsNotNull(member.Get<MarkerAttribute>());
            Assert.IsFalse(member.Has<OtherMarkerAttribute>());
            Assert.AreEqual(0, TypeModel.For(typeof(DerivedSample)).Find("first").Attributes.Count);
        }

        [Test]
        public void UsesInspectorAttributes_OnlyWhenTheTypeOrAMemberHasOne()
        {
            Assert.IsFalse(TypeModel.For(typeof(PlainSample)).UsesInspectorAttributes);
            Assert.IsTrue(TypeModel.For(typeof(DerivedSample)).UsesInspectorAttributes);
            Assert.IsTrue(TypeModel.For(typeof(MarkedClass)).UsesInspectorAttributes);
        }

        [Test]
        public void UnityBaseClasses_AreNotWalked()
        {
            CollectionAssert.AreEqual(new[] { "speed" }, Names(typeof(SampleBehaviour)));
        }

        [Test]
        public void Models_AreCachedPerType()
        {
            Assert.AreSame(TypeModel.For(typeof(DerivedSample)), TypeModel.For(typeof(DerivedSample)));
        }
    }
}
