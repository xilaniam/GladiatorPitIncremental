using NUnit.Framework;
using SMPro.Inspector.EditorTools;

namespace SMPro.Inspector.Tests
{
    public class HandlerRegistryTests
    {
        [Test]
        public void HandlerIsFound_ForItsAttribute()
        {
            Assert.AreEqual(typeof(TestFieldHandler), HandlerRegistry.HandlerTypeFor(typeof(TestFieldAttribute)));
            Assert.AreEqual(typeof(TestPriorityHandler), HandlerRegistry.HandlerTypeFor(typeof(TestPriorityAttribute)));
        }

        [Test]
        public void AttributeWithoutAHandler_HasNone()
        {
            Assert.IsNull(HandlerRegistry.HandlerTypeFor(typeof(TestUnhandledAttribute)));
        }

        [Test]
        public void Create_GivesAHandlerBoundToItsPropertyAndAttribute()
        {
            var property = new InspectorProperty(null, null, null, null);
            var attribute = new TestFieldAttribute();

            var handler = HandlerRegistry.Create(property, attribute) as TestFieldHandler;

            Assert.IsNotNull(handler);
            Assert.AreSame(property, handler.Property);
            Assert.AreSame(attribute, handler.Attribute);
        }
    }
}
