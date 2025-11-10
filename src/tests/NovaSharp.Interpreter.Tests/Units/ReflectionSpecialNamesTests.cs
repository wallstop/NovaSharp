namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ReflectionSpecialNamesTests
    {
        [Test]
        public void ConstructorThrowsOnNullOrEmpty()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    () => new ReflectionSpecialName((string)null),
                    Throws.TypeOf<ArgumentException>()
                );
                Assert.That(
                    () => new ReflectionSpecialName(string.Empty),
                    Throws.TypeOf<ArgumentException>()
                );
            });
        }

        [Test]
        public void RecognizesExplicitCast()
        {
            ReflectionSpecialName name = new ReflectionSpecialName("op_Explicit");

            Assert.That(name.Type, Is.EqualTo(ReflectionSpecialNameType.ExplicitCast));
            Assert.That(name.Argument, Is.Null);
        }

        [Test]
        public void RecognizesAdditionOperatorWhenQualified()
        {
            ReflectionSpecialName name = new ReflectionSpecialName("System.Int32.op_Addition");

            Assert.Multiple(() =>
            {
                Assert.That(name.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorAdd));
                Assert.That(name.Argument, Is.EqualTo("+"));
            });
        }

        [Test]
        public void RecognizesPropertyAccessors()
        {
            ReflectionSpecialName getter = new ReflectionSpecialName("get_Length");
            ReflectionSpecialName setter = new ReflectionSpecialName("set_Length");

            Assert.Multiple(() =>
            {
                Assert.That(getter.Type, Is.EqualTo(ReflectionSpecialNameType.PropertyGetter));
                Assert.That(getter.Argument, Is.EqualTo("Length"));
                Assert.That(setter.Type, Is.EqualTo(ReflectionSpecialNameType.PropertySetter));
                Assert.That(setter.Argument, Is.EqualTo("Length"));
            });
        }

        [Test]
        public void RecognizesEventAccessors()
        {
            ReflectionSpecialName add = new ReflectionSpecialName("add_Click");
            ReflectionSpecialName remove = new ReflectionSpecialName("remove_Click");

            Assert.Multiple(() =>
            {
                Assert.That(add.Type, Is.EqualTo(ReflectionSpecialNameType.AddEvent));
                Assert.That(add.Argument, Is.EqualTo("Click"));
                Assert.That(remove.Type, Is.EqualTo(ReflectionSpecialNameType.RemoveEvent));
                Assert.That(remove.Argument, Is.EqualTo("Click"));
            });
        }

        [Test]
        public void UnknownNamesLeaveTypeAtDefault()
        {
            ReflectionSpecialName unknown = new ReflectionSpecialName("CustomMethod");

            Assert.Multiple(() =>
            {
                Assert.That(unknown.Type, Is.EqualTo(default(ReflectionSpecialNameType)));
                Assert.That(unknown.Argument, Is.Null);
            });
        }
    }
}
