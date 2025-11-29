#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class StandardEnumUserDataDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorRejectsNonEnumTypes()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                _ = new StandardEnumUserDataDescriptor(typeof(string));
            });

            await Assert.That(exception.Message).Contains("enumType must be an enum");
        }

        [global::TUnit.Core.Test]
        public async Task FlagsEnumPopulatesMembersAndMetadata()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));

            await Assert.That(descriptor.UnderlyingType).IsEqualTo(typeof(byte));
            await Assert.That(descriptor.IsUnsigned).IsTrue();
            await Assert.That(descriptor.IsFlags).IsTrue();
            await Assert.That(descriptor.HasMember("flagsOr")).IsTrue();
            await Assert.That(descriptor.HasMember("__flagsOr")).IsTrue();
            await Assert.That(descriptor.HasMember(nameof(SampleFlags.Fast))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task NonFlagsEnumDoesNotExposeFlagHelpers()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleEnum));

            await Assert.That(descriptor.IsFlags).IsFalse();
            await Assert.That(descriptor.HasMember("flagsOr")).IsFalse();
            await Assert.That(descriptor.HasMember("__flagsOr")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CallbackOrCombinesUnsignedValues()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackOr(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SampleFlags.Fast, descriptor),
                    UserData.Create(SampleFlags.Safe, descriptor)
                )
            );

            await Assert
                .That((SampleFlags)result.UserData.Object)
                .IsEqualTo(SampleFlags.Fast | SampleFlags.Safe);
        }

        [global::TUnit.Core.Test]
        public async Task CallbackXorComputesExclusiveBits()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackXor(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SampleFlags.Fast | SampleFlags.Safe, descriptor),
                    UserData.Create(SampleFlags.Safe, descriptor)
                )
            );

            await Assert.That((SampleFlags)result.UserData.Object).IsEqualTo(SampleFlags.Fast);
        }

        [global::TUnit.Core.Test]
        public async Task CallbackNotOnUnsignedFlagsReturnsComplement()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackBwNot(
                context,
                TestHelpers.CreateArguments(UserData.Create(SampleFlags.Safe, descriptor))
            );

            await Assert.That((SampleFlags)result.UserData.Object).IsEqualTo(~SampleFlags.Safe);
        }

        [global::TUnit.Core.Test]
        public async Task CallbackOrAcceptsNumericArguments()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackOr(
                context,
                TestHelpers.CreateArguments(DynValue.NewNumber(1), DynValue.NewNumber(4))
            );

            await Assert
                .That((SampleFlags)result.UserData.Object)
                .IsEqualTo(SampleFlags.Fast | SampleFlags.Light);
        }

        [global::TUnit.Core.Test]
        public async Task CallbackHasAllReturnsBooleanResult()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue hasAll = descriptor.CallbackHasAll(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SampleFlags.Fast | SampleFlags.Safe, descriptor),
                    UserData.Create(SampleFlags.Safe, descriptor)
                )
            );

            await Assert.That(hasAll.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(hasAll.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CallbackHasAnyReturnsBoolean()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue hasAny = descriptor.CallbackHasAny(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SampleFlags.Fast, descriptor),
                    UserData.Create(SampleFlags.Safe, descriptor)
                )
            );

            await Assert.That(hasAny.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(hasAny.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CallbackAndUsesSignedArithmeticWhenEnumIsSigned()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackAnd(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left | SignedIntFlags.Right, descriptor),
                    UserData.Create(SignedIntFlags.Left, descriptor)
                )
            );

            await Assert
                .That((SignedIntFlags)result.UserData.Object)
                .IsEqualTo(SignedIntFlags.Left);
        }

        [global::TUnit.Core.Test]
        public async Task CallbackXorUsesSignedArithmeticWhenEnumIsSigned()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackXor(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left | SignedIntFlags.Right, descriptor),
                    UserData.Create(SignedIntFlags.Right, descriptor)
                )
            );

            await Assert
                .That((SignedIntFlags)result.UserData.Object)
                .IsEqualTo(SignedIntFlags.Left);
        }

        [global::TUnit.Core.Test]
        public async Task SignedFlagsHasAllReturnsBoolean()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue hasAll = descriptor.CallbackHasAll(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left | SignedIntFlags.Right, descriptor),
                    UserData.Create(SignedIntFlags.Left, descriptor)
                )
            );

            await Assert.That(hasAll.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(hasAll.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SignedFlagsHasAnyReturnsBoolean()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue hasAny = descriptor.CallbackHasAny(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SignedIntFlags.Left, descriptor),
                    UserData.Create(SignedIntFlags.Right, descriptor)
                )
            );

            await Assert.That(hasAny.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(hasAny.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SignedBinaryOperationThrowsWhenArgumentCountInvalid()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CallbackAnd(
                    context,
                    TestHelpers.CreateArguments(UserData.Create(SignedIntFlags.Left, descriptor))
                )
            );

            await Assert.That(exception.Message).Contains("expects two arguments");
        }

        [global::TUnit.Core.Test]
        public async Task SignedEnumsUseSignedArithmetic()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleSignedEnum));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackBwNot(
                context,
                TestHelpers.CreateArguments(
                    UserData.Create(SampleSignedEnum.NegativeOne, descriptor)
                )
            );

            await Assert
                .That((SampleSignedEnum)result.UserData.Object)
                .IsEqualTo(SampleSignedEnum.Zero);
            await Assert.That(descriptor.IsUnsigned).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SignedConversionSupportsAllUnderlyingTypes()
        {
            foreach ((Type enumType, object flagValue) in SignedEnumCases())
            {
                StandardEnumUserDataDescriptor descriptor = new(enumType);
                ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());
                DynValue left = UserData.Create(flagValue, descriptor);
                DynValue result = descriptor.CallbackAnd(
                    context,
                    TestHelpers.CreateArguments(left, left)
                );

                long expected = Convert.ToInt64(flagValue, CultureInfo.InvariantCulture);
                long actual = Convert.ToInt64(result.UserData.Object, CultureInfo.InvariantCulture);

                await Assert.That(descriptor.IsUnsigned).IsFalse();
                await Assert.That(actual).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task UnsignedConversionSupportsAllUnderlyingTypes()
        {
            foreach ((Type enumType, object flagValue) in UnsignedEnumCases())
            {
                StandardEnumUserDataDescriptor descriptor = new(enumType);
                ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());
                DynValue left = UserData.Create(flagValue, descriptor);
                DynValue result = descriptor.CallbackAnd(
                    context,
                    TestHelpers.CreateArguments(left, left)
                );

                ulong expected = Convert.ToUInt64(flagValue, CultureInfo.InvariantCulture);
                ulong actual = Convert.ToUInt64(
                    result.UserData.Object,
                    CultureInfo.InvariantCulture
                );

                await Assert.That(descriptor.IsUnsigned).IsTrue();
                await Assert.That(actual).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task BinaryOperationSignedAcceptsNumericArguments()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleSignedEnum));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            DynValue result = descriptor.CallbackOr(
                context,
                TestHelpers.CreateArguments(DynValue.NewNumber(1), DynValue.NewNumber(2))
            );

            await Assert.That((int)result.UserData.Object).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task BinaryOperationRejectsInvalidOperandType()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CallbackOr(
                    context,
                    TestHelpers.CreateArguments(DynValue.NewString("bad"), DynValue.NewNumber(1))
                )
            );

            await Assert.That(exception.Message).Contains("Enum userdata or number expected");
        }

        [global::TUnit.Core.Test]
        public async Task BinaryOperationRejectsForeignDescriptor()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            StandardEnumUserDataDescriptor foreignDescriptor = new(typeof(SampleEnum));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CallbackOr(
                    context,
                    TestHelpers.CreateArguments(
                        UserData.Create(SampleEnum.One, foreignDescriptor),
                        DynValue.NewNumber(1)
                    )
                )
            );

            await Assert.That(exception.Message).Contains("Enum userdata or number expected");
        }

        [global::TUnit.Core.Test]
        public async Task SignedBinaryOperationRejectsForeignDescriptor()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SignedIntFlags));
            StandardEnumUserDataDescriptor foreignDescriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CallbackOr(
                    context,
                    TestHelpers.CreateArguments(
                        UserData.Create(SampleFlags.Fast, foreignDescriptor),
                        UserData.Create(SignedIntFlags.Left, descriptor)
                    )
                )
            );

            await Assert.That(exception.Message).Contains("Enum userdata or number expected");
        }

        [global::TUnit.Core.Test]
        public async Task BinaryOperationThrowsWhenArgumentCountInvalid()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CallbackOr(
                    context,
                    TestHelpers.CreateArguments(UserData.Create(SampleFlags.Fast, descriptor))
                )
            );

            await Assert.That(exception.Message).Contains("expects two arguments");
        }

        [global::TUnit.Core.Test]
        public async Task UnaryOperationThrowsWhenArgumentCountInvalid()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CallbackBwNot(context, TestHelpers.CreateArguments())
            );

            await Assert.That(exception.Message).Contains("expects one argument");
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsConcatForFlags()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleFlags));

            DynValue meta = descriptor.MetaIndex(new Script(), SampleFlags.Fast, "__concat");

            await Assert.That(meta.Type).IsEqualTo(DataType.ClrFunction);
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsNullForNonFlags()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleEnum));
            DynValue meta = descriptor.MetaIndex(new Script(), SampleEnum.One, "__concat");

            await Assert.That(meta).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleRespectsDescriptorType()
        {
            StandardEnumUserDataDescriptor descriptor = new(typeof(SampleEnum));

            await Assert
                .That(descriptor.IsTypeCompatible(typeof(SampleEnum), SampleEnum.One))
                .IsTrue();
            await Assert.That(descriptor.IsTypeCompatible(typeof(SampleEnum), null)).IsFalse();
        }

        private static IEnumerable<(Type EnumType, object Value)> SignedEnumCases()
        {
            yield return (typeof(SByteEnum), SByteEnum.Value);
            yield return (typeof(ShortEnum), ShortEnum.Value);
            yield return (typeof(IntEnum), IntEnum.Value);
            yield return (typeof(LongEnum), LongEnum.Value);
        }

        private static IEnumerable<(Type EnumType, object Value)> UnsignedEnumCases()
        {
            yield return (typeof(ByteFlags), ByteFlags.Flag);
            yield return (typeof(UShortFlags), UShortFlags.Flag);
            yield return (typeof(UIntFlags), UIntFlags.Flag);
            yield return (typeof(ULongFlags), ULongFlags.Flag);
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
#pragma warning restore CA2007
