#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Interop;

    public sealed class ReflectionSpecialNamesTUnitTests
    {
        [global::TUnit.Core.Test]
        public void ConstructorThrowsOnNullOrEmpty()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                ReflectionSpecialName name = new((string)null);
                _ = name.Type;
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ReflectionSpecialName name = new(string.Empty);
                _ = name.Argument;
            });
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesExplicitCast()
        {
            ReflectionSpecialName name = new("op_Explicit");

            await Assert.That(name.Type).IsEqualTo(ReflectionSpecialNameType.ExplicitCast);
            await Assert.That(name.Argument).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesAdditionOperatorWhenQualified()
        {
            ReflectionSpecialName name = new("System.Int32.op_Addition");

            await Assert.That(name.Type).IsEqualTo(ReflectionSpecialNameType.OperatorAdd);
            await Assert.That(name.Argument).IsEqualTo("+");
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesPropertyAccessors()
        {
            ReflectionSpecialName getter = new("get_Length");
            ReflectionSpecialName setter = new("set_Length");

            await Assert.That(getter.Type).IsEqualTo(ReflectionSpecialNameType.PropertyGetter);
            await Assert.That(getter.Argument).IsEqualTo("Length");
            await Assert.That(setter.Type).IsEqualTo(ReflectionSpecialNameType.PropertySetter);
            await Assert.That(setter.Argument).IsEqualTo("Length");
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesEventAccessors()
        {
            ReflectionSpecialName add = new("add_Click");
            ReflectionSpecialName remove = new("remove_Click");

            await Assert.That(add.Type).IsEqualTo(ReflectionSpecialNameType.AddEvent);
            await Assert.That(add.Argument).IsEqualTo("Click");
            await Assert.That(remove.Type).IsEqualTo(ReflectionSpecialNameType.RemoveEvent);
            await Assert.That(remove.Argument).IsEqualTo("Click");
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesIndexerSpecialNames()
        {
            ReflectionSpecialName setter = new("set_Item");
            ReflectionSpecialName getter = new("get_Item");

            await Assert.That(setter.Type).IsEqualTo(ReflectionSpecialNameType.IndexSetter);
            await Assert.That(setter.Argument).IsNull();
            await Assert.That(getter.Type).IsEqualTo(ReflectionSpecialNameType.IndexGetter);
            await Assert.That(getter.Argument).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesBooleanOperatorNames()
        {
            ReflectionSpecialName opTrue = new("System.Boolean.op_True");
            ReflectionSpecialName opFalse = new("op_False");

            await Assert.That(opTrue.Type).IsEqualTo(ReflectionSpecialNameType.OperatorTrue);
            await Assert.That(opTrue.Argument).IsNull();
            await Assert.That(opFalse.Type).IsEqualTo(ReflectionSpecialNameType.OperatorFalse);
            await Assert.That(opFalse.Argument).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesUnaryOperatorsWithArguments()
        {
            ReflectionSpecialName neg = new("op_UnaryNegation");
            ReflectionSpecialName logicalNot = new("op_LogicalNot");
            ReflectionSpecialName onesComp = new("op_OnesComplement");

            await Assert.That(neg.Type).IsEqualTo(ReflectionSpecialNameType.OperatorNeg);
            await Assert.That(neg.Argument).IsEqualTo("-");
            await Assert.That(logicalNot.Type).IsEqualTo(ReflectionSpecialNameType.OperatorNot);
            await Assert.That(logicalNot.Argument).IsEqualTo("!");
            await Assert.That(onesComp.Type).IsEqualTo(ReflectionSpecialNameType.OperatorCompl);
            await Assert.That(onesComp.Argument).IsEqualTo("~");
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesBinaryOperatorsWithSymbolArguments()
        {
            ReflectionSpecialName bitwiseOr = new("op_BitwiseOr");
            ReflectionSpecialName xor = new("op_ExclusiveOr");
            ReflectionSpecialName mod = new("op_Modulus");

            await Assert.That(bitwiseOr.Type).IsEqualTo(ReflectionSpecialNameType.OperatorOr);
            await Assert.That(bitwiseOr.Argument).IsEqualTo("|");
            await Assert.That(xor.Type).IsEqualTo(ReflectionSpecialNameType.OperatorXor);
            await Assert.That(xor.Argument).IsEqualTo("^");
            await Assert.That(mod.Type).IsEqualTo(ReflectionSpecialNameType.OperatorMod);
            await Assert.That(mod.Argument).IsEqualTo("%");
        }

        [global::TUnit.Core.Test]
        public async Task RecognizesAdditionalOperatorMappings()
        {
            foreach (
                (string name, ReflectionSpecialNameType type, string argument) in GetOperatorCases()
            )
            {
                ReflectionSpecialName specialName = new(name);
                await Assert.That(specialName.Type).IsEqualTo(type);
                await Assert.That(specialName.Argument).IsEqualTo(argument);
            }
        }

        [global::TUnit.Core.Test]
        public async Task EqualityComparesTypeAndArgument()
        {
            ReflectionSpecialName getterFromString = new("get_Length");
            ReflectionSpecialName getterTyped = new(
                ReflectionSpecialNameType.PropertyGetter,
                "Length"
            );
            ReflectionSpecialName differentArgument = new(
                ReflectionSpecialNameType.PropertyGetter,
                "Width"
            );

            await Assert.That(getterFromString.Equals(getterTyped)).IsTrue();
            await Assert.That(getterFromString == getterTyped).IsTrue();
            await Assert.That(getterFromString != differentArgument).IsTrue();
            await Assert.That(getterFromString.GetHashCode()).IsEqualTo(getterTyped.GetHashCode());
        }

        [global::TUnit.Core.Test]
        public async Task EqualityHandlesNullArgumentsAndObjectOverrides()
        {
            ReflectionSpecialName opTrue = new("op_True");
            ReflectionSpecialName typedTrue = new(ReflectionSpecialNameType.OperatorTrue);
            ReflectionSpecialName opFalse = new("op_False");

            await Assert.That(opTrue.Equals((object)typedTrue)).IsTrue();
            await Assert.That(opTrue.Equals(new object())).IsFalse();
            await Assert.That(opTrue != opFalse).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task UnknownNamesLeaveTypeAtDefault()
        {
            ReflectionSpecialName unknown = new("CustomMethod");

            await Assert.That(unknown.Type).IsEqualTo(default(ReflectionSpecialNameType));
            await Assert.That(unknown.Argument).IsNull();
        }

        private static IEnumerable<(
            string Name,
            ReflectionSpecialNameType Type,
            string Argument
        )> GetOperatorCases()
        {
            yield return ("op_Implicit", ReflectionSpecialNameType.ImplicitCast, null);
            yield return ("op_BitwiseAnd", ReflectionSpecialNameType.OperatorAnd, "&");
            yield return ("op_Decrement", ReflectionSpecialNameType.OperatorDec, "--");
            yield return ("op_Division", ReflectionSpecialNameType.OperatorDiv, "/");
            yield return ("op_Equality", ReflectionSpecialNameType.OperatorEq, "==");
            yield return ("op_GreaterThan", ReflectionSpecialNameType.OperatorGt, ">");
            yield return ("op_GreaterThanOrEqual", ReflectionSpecialNameType.OperatorGte, ">=");
            yield return ("op_Increment", ReflectionSpecialNameType.OperatorInc, "++");
            yield return ("op_Inequality", ReflectionSpecialNameType.OperatorNeq, "!=");
            yield return ("op_LessThan", ReflectionSpecialNameType.OperatorLt, "<");
            yield return ("op_LessThanOrEqual", ReflectionSpecialNameType.OperatorLte, "<=");
            yield return ("op_Multiply", ReflectionSpecialNameType.OperatorMul, "*");
            yield return ("op_Subtraction", ReflectionSpecialNameType.OperatorSub, "-");
            yield return ("op_UnaryPlus", ReflectionSpecialNameType.OperatorUnaryPlus, "+");
        }
    }
}
#pragma warning restore CA2007
