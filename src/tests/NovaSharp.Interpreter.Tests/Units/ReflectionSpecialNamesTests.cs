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
        public void RecognizesIndexerSpecialNames()
        {
            ReflectionSpecialName getter = new ReflectionSpecialName("set_Item");
            ReflectionSpecialName setter = new ReflectionSpecialName("get_Item");

            Assert.Multiple(() =>
            {
                Assert.That(getter.Type, Is.EqualTo(ReflectionSpecialNameType.IndexSetter));
                Assert.That(getter.Argument, Is.Null);
                Assert.That(setter.Type, Is.EqualTo(ReflectionSpecialNameType.IndexGetter));
                Assert.That(setter.Argument, Is.Null);
            });
        }

        [Test]
        public void RecognizesBooleanOperatorNames()
        {
            ReflectionSpecialName opTrue = new ReflectionSpecialName("System.Boolean.op_True");
            ReflectionSpecialName opFalse = new ReflectionSpecialName("op_False");

            Assert.Multiple(() =>
            {
                Assert.That(opTrue.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorTrue));
                Assert.That(opTrue.Argument, Is.Null);
                Assert.That(opFalse.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorFalse));
                Assert.That(opFalse.Argument, Is.Null);
            });
        }

        [Test]
        public void RecognizesUnaryOperatorsWithArguments()
        {
            ReflectionSpecialName neg = new ReflectionSpecialName("op_UnaryNegation");
            ReflectionSpecialName not = new ReflectionSpecialName("op_LogicalNot");
            ReflectionSpecialName comp = new ReflectionSpecialName("op_OnesComplement");

            Assert.Multiple(() =>
            {
                Assert.That(neg.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorNeg));
                Assert.That(neg.Argument, Is.EqualTo("-"));
                Assert.That(not.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorNot));
                Assert.That(not.Argument, Is.EqualTo("!"));
                Assert.That(comp.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorCompl));
                Assert.That(comp.Argument, Is.EqualTo("~"));
            });
        }

        [Test]
        public void RecognizesBinaryOperatorsWithSymbolArguments()
        {
            ReflectionSpecialName or = new ReflectionSpecialName("op_BitwiseOr");
            ReflectionSpecialName xor = new ReflectionSpecialName("op_ExclusiveOr");
            ReflectionSpecialName mod = new ReflectionSpecialName("op_Modulus");

            Assert.Multiple(() =>
            {
                Assert.That(or.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorOr));
                Assert.That(or.Argument, Is.EqualTo("|"));
                Assert.That(xor.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorXor));
                Assert.That(xor.Argument, Is.EqualTo("^"));
                Assert.That(mod.Type, Is.EqualTo(ReflectionSpecialNameType.OperatorMod));
                Assert.That(mod.Argument, Is.EqualTo("%"));
            });
        }

        [Test]
        [TestCase("op_Implicit", ReflectionSpecialNameType.ImplicitCast, null)]
        [TestCase("op_BitwiseAnd", ReflectionSpecialNameType.OperatorAnd, "&")]
        [TestCase("op_Decrement", ReflectionSpecialNameType.OperatorDec, "--")]
        [TestCase("op_Division", ReflectionSpecialNameType.OperatorDiv, "/")]
        [TestCase("op_Equality", ReflectionSpecialNameType.OperatorEq, "==")]
        [TestCase("op_GreaterThan", ReflectionSpecialNameType.OperatorGt, ">")]
        [TestCase("op_GreaterThanOrEqual", ReflectionSpecialNameType.OperatorGte, ">=")]
        [TestCase("op_Increment", ReflectionSpecialNameType.OperatorInc, "++")]
        [TestCase("op_Inequality", ReflectionSpecialNameType.OperatorNeq, "!=")]
        [TestCase("op_LessThan", ReflectionSpecialNameType.OperatorLt, "<")]
        [TestCase("op_LessThanOrEqual", ReflectionSpecialNameType.OperatorLte, "<=")]
        [TestCase("op_Multiply", ReflectionSpecialNameType.OperatorMul, "*")]
        [TestCase("op_Subtraction", ReflectionSpecialNameType.OperatorSub, "-")]
        [TestCase("op_UnaryPlus", ReflectionSpecialNameType.OperatorUnaryPlus, "+")]
        public void RecognizesAdditionalOperatorMappings(
            string specialName,
            ReflectionSpecialNameType expectedType,
            string expectedArgument
        )
        {
            ReflectionSpecialName name = new(specialName);

            Assert.Multiple(() =>
            {
                Assert.That(name.Type, Is.EqualTo(expectedType));
                Assert.That(name.Argument, Is.EqualTo(expectedArgument));
            });
        }

        [Test]
        public void EqualityComparesTypeAndArgument()
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

            Assert.Multiple(() =>
            {
                Assert.That(getterFromString.Equals(getterTyped), Is.True);
                Assert.That(getterFromString == getterTyped, Is.True);
                Assert.That(getterFromString != differentArgument, Is.True);
                Assert.That(getterFromString.GetHashCode(), Is.EqualTo(getterTyped.GetHashCode()));
            });
        }

        [Test]
        public void EqualityHandlesNullArgumentsAndObjectOverrides()
        {
            ReflectionSpecialName opTrue = new("op_True");
            ReflectionSpecialName typedTrue = new(ReflectionSpecialNameType.OperatorTrue);
            ReflectionSpecialName opFalse = new("op_False");

            Assert.Multiple(() =>
            {
                Assert.That(opTrue.Equals((object)typedTrue), Is.True);
                Assert.That(opTrue.Equals(new object()), Is.False);
                Assert.That(opTrue != opFalse, Is.True);
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
