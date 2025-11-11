namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StandardEnumUserDataDescriptorTests
    {
        [Test]
        public void ConstructorRejectsNonEnumTypes()
        {
            Assert.That(
                () => new StandardEnumUserDataDescriptor(typeof(string)),
                Throws.ArgumentException.With.Message.Contains("enumType must be an enum")
            );
        }

        [Test]
        public void FlagsEnumPopulatesMembersAndMetadata()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.UnderlyingType, Is.EqualTo(typeof(byte)));
                Assert.That(descriptor.IsUnsigned, Is.True);
                Assert.That(descriptor.IsFlags, Is.True);
                Assert.That(descriptor.HasMember("flagsOr"), Is.True);
                Assert.That(descriptor.HasMember("__flagsOr"), Is.True);
                Assert.That(descriptor.HasMember(nameof(SampleFlags.Fast)), Is.True);
            });
        }

        [Test]
        public void NonFlagsEnumDoesNotExposeFlagHelpers()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleEnum));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsFlags, Is.False);
                Assert.That(descriptor.HasMember("flagsOr"), Is.False);
                Assert.That(descriptor.HasMember("__flagsOr"), Is.False);
            });
        }

        [Test]
        public void CallbackOrCombinesUnsignedValues()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue left = UserData.Create(SampleFlags.Fast, descriptor);
            DynValue right = UserData.Create(SampleFlags.Safe, descriptor);

            DynValue result = descriptor.Callback_Or(
                context,
                TestHelpers.CreateArguments(left, right)
            );

            SampleFlags combined = (SampleFlags)result.UserData.Object;
            Assert.That(combined, Is.EqualTo(SampleFlags.Fast | SampleFlags.Safe));
        }

        [Test]
        public void CallbackOrAcceptsNumericArguments()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = descriptor.Callback_Or(
                context,
                TestHelpers.CreateArguments(DynValue.NewNumber(1), DynValue.NewNumber(4))
            );

            SampleFlags combined = (SampleFlags)result.UserData.Object;
            Assert.That(combined, Is.EqualTo(SampleFlags.Fast | SampleFlags.Light));
        }

        [Test]
        public void CallbackHasAllReturnsBooleanResult()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue value = UserData.Create(SampleFlags.Fast | SampleFlags.Safe, descriptor);
            DynValue mask = UserData.Create(SampleFlags.Safe, descriptor);

            DynValue hasAll = descriptor.Callback_HasAll(
                context,
                TestHelpers.CreateArguments(value, mask)
            );

            Assert.That(hasAll.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(hasAll.Boolean, Is.True);
        }

        [Test]
        public void SignedEnumsUseSignedArithmetic()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleSignedEnum));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue value = UserData.Create(SampleSignedEnum.NegativeOne, descriptor);

            DynValue result = descriptor.Callback_BwNot(
                context,
                TestHelpers.CreateArguments(value)
            );

            SampleSignedEnum negated = (SampleSignedEnum)result.UserData.Object;
            Assert.That(negated, Is.EqualTo((SampleSignedEnum)0));
            Assert.That(descriptor.IsUnsigned, Is.False);
        }

        private enum SampleEnum
        {
            One,
            Two,
        }

        [Flags]
        private enum SampleFlags : byte
        {
            None = 0,
            Fast = 1,
            Safe = 2,
            Light = 4,
        }

        private enum SampleSignedEnum : int
        {
            Zero = 0,
            NegativeOne = -1,
        }
    }
}
