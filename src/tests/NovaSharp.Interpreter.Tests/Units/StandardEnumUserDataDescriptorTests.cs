namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
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
        public void CallbackXorComputesExclusiveBits()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.Callback_Xor(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SampleFlags.Fast | SampleFlags.Safe, descriptor),
                    UserData.Create(SampleFlags.Safe, descriptor)
                )
            );

            Assert.That((SampleFlags)result.UserData.Object, Is.EqualTo(SampleFlags.Fast));
        }

        [Test]
        public void CallbackNotOnUnsignedFlagsReturnsComplement()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.Callback_BwNot(
                context,
                TestHelpers.CreateArguments(UserData.Create(SampleFlags.Safe, descriptor))
            );

            Assert.That(
                (SampleFlags)result.UserData.Object,
                Is.EqualTo(~SampleFlags.Safe)
            );
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
        public void CallbackHasAnyReturnsBoolean()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue value = UserData.Create(SampleFlags.Fast, descriptor);
            DynValue hasAny = descriptor.Callback_HasAny(
                context,
                TestHelpers.CreateArguments(value, UserData.Create(SampleFlags.Safe, descriptor))
            );

            Assert.Multiple(() =>
            {
                Assert.That(hasAny.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(hasAny.Boolean, Is.False);
            });
        }

        [Test]
        public void CallbackAndUsesSignedArithmeticWhenEnumIsSigned()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());
            SignedIntFlags leftValue = SignedIntFlags.Left | SignedIntFlags.Right;
            SignedIntFlags rightValue = SignedIntFlags.Left;

            DynValue result = descriptor.Callback_And(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(leftValue, descriptor),
                    UserData.Create(rightValue, descriptor)
                )
            );

            Assert.That((SignedIntFlags)result.UserData.Object, Is.EqualTo(SignedIntFlags.Left));
        }

        [Test]
        public void CallbackXorUsesSignedArithmeticWhenEnumIsSigned()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.Callback_Xor(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left | SignedIntFlags.Right, descriptor),
                    UserData.Create(SignedIntFlags.Right, descriptor)
                )
            );

            Assert.That((SignedIntFlags)result.UserData.Object, Is.EqualTo(SignedIntFlags.Left));
        }

        [Test]
        public void SignedFlagsHasAllReturnsBoolean()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue hasAll = descriptor.Callback_HasAll(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left | SignedIntFlags.Right, descriptor),
                    UserData.Create(SignedIntFlags.Left, descriptor)
                )
            );

            Assert.Multiple(() =>
            {
                Assert.That(hasAll.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(hasAll.Boolean, Is.True);
            });
        }

        [Test]
        public void SignedFlagsHasAnyReturnsBoolean()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue hasAny = descriptor.Callback_HasAny(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left, descriptor),
                    UserData.Create(SignedIntFlags.Right, descriptor)
                )
            );

            Assert.Multiple(() =>
            {
                Assert.That(hasAny.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(hasAny.Boolean, Is.False);
            });
        }

        [Test]
        public void SignedBinaryOperationThrowsWhenArgumentCountInvalid()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            Assert.That(
                () =>
                    descriptor.Callback_And(
                        context,
                        TestHelpers.CreateArguments(UserData.Create(SignedIntFlags.Left, descriptor))
                    ),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("expects two arguments")
            );
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
            Assert.That((int)negated, Is.EqualTo(0));
            Assert.That(descriptor.IsUnsigned, Is.False);
        }

        [TestCaseSource(nameof(SignedEnumCases))]
        public void SignedConversionSupportsAllUnderlyingTypes(Type enumType, object flagValue)
        {
            StandardEnumUserDataDescriptor descriptor = new(enumType);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue left = UserData.Create(flagValue, descriptor);
            DynValue result = descriptor.Callback_And(
                context,
                TestHelpers.CreateArguments(left, left)
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsUnsigned, Is.False);
                Assert.That(
                    Convert.ToInt64(result.UserData.Object),
                    Is.EqualTo(Convert.ToInt64(flagValue))
                );
            });
        }

        [TestCaseSource(nameof(UnsignedEnumCases))]
        public void UnsignedConversionSupportsAllUnderlyingTypes(Type enumType, object flagValue)
        {
            StandardEnumUserDataDescriptor descriptor = new(enumType);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue left = UserData.Create(flagValue, descriptor);
            DynValue result = descriptor.Callback_And(
                context,
                TestHelpers.CreateArguments(left, left)
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsUnsigned, Is.True);
                Assert.That(
                    Convert.ToUInt64(result.UserData.Object),
                    Is.EqualTo(Convert.ToUInt64(flagValue))
                );
            });
        }

        [Test]
        public void BinaryOperationSignedAcceptsNumericArguments()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleSignedEnum));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.Callback_Or(
                context,
                TestHelpers.CreateArguments(DynValue.NewNumber(1), DynValue.NewNumber(2))
            );

            Assert.That((int)result.UserData.Object, Is.EqualTo(3));
        }

        [Test]
        public void BinaryOperationRejectsInvalidOperandType()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            Assert.That(
                () =>
                    descriptor.Callback_Or(
                        context,
                        TestHelpers.CreateArguments(
                            DynValue.NewString("bad"),
                            DynValue.NewNumber(1)
                        )
                    ),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("Enum userdata or number expected")
            );
        }

        [Test]
        public void BinaryOperationRejectsForeignDescriptor()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            StandardEnumUserDataDescriptor foreignDescriptor = new(typeof(SampleEnum));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue foreignValue = UserData.Create(SampleEnum.One, foreignDescriptor);

            Assert.That(
                () =>
                    descriptor.Callback_Or(
                        context,
                        TestHelpers.CreateArguments(foreignValue, DynValue.NewNumber(1))
                    ),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("Enum userdata or number expected")
            );
        }

        [Test]
        public void SignedBinaryOperationRejectsForeignDescriptor()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            StandardEnumUserDataDescriptor foreignDescriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue foreignValue = UserData.Create(SampleFlags.Fast, foreignDescriptor);

            Assert.That(
                () =>
                    descriptor.Callback_Or(
                        context,
                        TestHelpers.CreateArguments(
                            foreignValue,
                            UserData.Create(SignedIntFlags.Left, descriptor)
                        )
                    ),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("Enum userdata or number expected")
            );
        }

        [Test]
        public void BinaryOperationThrowsWhenArgumentCountInvalid()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());
            DynValue value = UserData.Create(SampleFlags.Fast, descriptor);

            Assert.That(
                () => descriptor.Callback_Or(context, TestHelpers.CreateArguments(value)),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("expects two arguments")
            );
        }

        [Test]
        public void UnaryOperationThrowsWhenArgumentCountInvalid()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            Assert.That(
                () => descriptor.Callback_BwNot(context, TestHelpers.CreateArguments()),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("expects one argument")
            );
        }

        [Test]
        public void MetaIndexReturnsConcatForFlags()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            DynValue meta = descriptor.MetaIndex(new Script(), SampleFlags.Fast, "__concat");

            Assert.That(meta.Type, Is.EqualTo(DataType.ClrFunction));
        }

        [Test]
        public void MetaIndexReturnsNullForNonFlags()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleEnum));

            DynValue meta = descriptor.MetaIndex(new Script(), SampleEnum.One, "__concat");

            Assert.That(meta, Is.Null);
        }

        [Test]
        public void IsTypeCompatibleRespectsDescriptorType()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleEnum));

            Assert.Multiple(() =>
            {
                Assert.That(
                    descriptor.IsTypeCompatible(typeof(SampleEnum), SampleEnum.One),
                    Is.True
                );
                Assert.That(descriptor.IsTypeCompatible(typeof(SampleEnum), null), Is.False);
            });
        }

        private static IEnumerable<TestCaseData> SignedEnumCases()
        {
            yield return new TestCaseData(typeof(SByteEnum), SByteEnum.Value);
            yield return new TestCaseData(typeof(ShortEnum), ShortEnum.Value);
            yield return new TestCaseData(typeof(IntEnum), IntEnum.Value);
            yield return new TestCaseData(typeof(LongEnum), LongEnum.Value);
        }

        private static IEnumerable<TestCaseData> UnsignedEnumCases()
        {
            yield return new TestCaseData(typeof(ByteFlags), ByteFlags.Flag);
            yield return new TestCaseData(typeof(UShortFlags), UShortFlags.Flag);
            yield return new TestCaseData(typeof(UIntFlags), UIntFlags.Flag);
            yield return new TestCaseData(typeof(ULongFlags), ULongFlags.Flag);
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

        [Flags]
        private enum SignedIntFlags : int
        {
            None = 0,
            Left = 1 << 30,
            Right = 1 << 29,
        }

        private enum SampleSignedEnum : int
        {
            Zero = 0,
            NegativeOne = -1,
        }

        private enum SByteEnum : sbyte
        {
            Zero = 0,
            Value = 1,
        }

        private enum ShortEnum : short
        {
            Zero = 0,
            Value = 1,
        }

        private enum IntEnum : int
        {
            Zero = 0,
            Value = 1,
        }

        private enum LongEnum : long
        {
            Zero = 0,
            Value = 1,
        }

        [Flags]
        private enum ByteFlags : byte
        {
            None = 0,
            Flag = 1,
        }

        [Flags]
        private enum UShortFlags : ushort
        {
            None = 0,
            Flag = 1,
        }

        [Flags]
        private enum UIntFlags : uint
        {
            None = 0,
            Flag = 1,
        }

        [Flags]
        private enum ULongFlags : ulong
        {
            None = 0,
            Flag = 1,
        }
    }
}
