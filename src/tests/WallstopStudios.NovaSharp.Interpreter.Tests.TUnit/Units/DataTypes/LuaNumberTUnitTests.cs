namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes;

using System;
using System.Threading.Tasks;
using global::TUnit.Core;
using WallstopStudios.NovaSharp.Interpreter.Compatibility;
using WallstopStudios.NovaSharp.Interpreter.DataTypes;
using WallstopStudios.NovaSharp.Interpreter.Errors;

/// <summary>
/// Comprehensive tests for the LuaNumber discriminated union struct.
/// Tests cover integer/float discrimination, arithmetic operations, bitwise operations,
/// comparison semantics, and Lua specification compliance.
/// </summary>
public sealed class LuaNumberTUnitTests
{
    [Test]
    public async Task FromIntegerCreatesIntegerSubtype()
    {
        LuaNumber num = LuaNumber.FromInteger(42);
        await Assert.That(num.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(num.IsFloat).IsFalse().ConfigureAwait(false);
        await Assert.That(num.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task FromFloatCreatesFloatSubtype()
    {
        LuaNumber num = LuaNumber.FromFloat(3.14);
        await Assert.That(num.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(num.IsInteger).IsFalse().ConfigureAwait(false);
        await Assert.That(num.AsFloat).IsEqualTo(3.14).ConfigureAwait(false);
    }

    [Test]
    public async Task FromDoubleWithWholeNumberCreatesInteger()
    {
        LuaNumber num = LuaNumber.FromDouble(42.0);
        await Assert.That(num.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task FromDoubleWithFractionalNumberCreatesFloat()
    {
        LuaNumber num = LuaNumber.FromDouble(3.14);
        await Assert.That(num.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsFloat).IsEqualTo(3.14).ConfigureAwait(false);
    }

    [Test]
    public async Task FromDoubleWithNaNCreatesFloat()
    {
        LuaNumber num = LuaNumber.FromDouble(double.NaN);
        await Assert.That(num.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(double.IsNaN(num.AsFloat)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task FromDoubleWithInfinityCreatesFloat()
    {
        LuaNumber posInf = LuaNumber.FromDouble(double.PositiveInfinity);
        LuaNumber negInf = LuaNumber.FromDouble(double.NegativeInfinity);

        await Assert.That(posInf.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(negInf.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(double.IsPositiveInfinity(posInf.AsFloat)).IsTrue().ConfigureAwait(false);
        await Assert.That(double.IsNegativeInfinity(negInf.AsFloat)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task FromDoubleWithNegativeZeroCreatesFloat()
    {
        // Negative zero must remain a float to preserve IEEE 754 semantics
        // (e.g., 1.0 / -0.0 must return -inf, not +inf)
        LuaNumber negZero = LuaNumber.FromDouble(-0.0);

        await Assert.That(negZero.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(negZero.ToDouble).IsEqualTo(0.0).ConfigureAwait(false);
        await Assert.That(double.IsNegative(negZero.ToDouble)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task DivisionByNegativeZeroReturnsNegativeInfinity()
    {
        LuaNumber one = LuaNumber.FromFloat(1.0);
        LuaNumber negZero = LuaNumber.FromDouble(-0.0);

        LuaNumber result = LuaNumber.Divide(one, negZero);

        await Assert
            .That(double.IsNegativeInfinity(result.ToDouble))
            .IsTrue()
            .ConfigureAwait(false);
    }

    [Test]
    public async Task MaxIntegerHasCorrectValue()
    {
        await Assert
            .That(LuaNumber.MaxInteger.AsInteger)
            .IsEqualTo(long.MaxValue)
            .ConfigureAwait(false);
        await Assert.That(LuaNumber.MaxInteger.IsInteger).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task MinIntegerHasCorrectValue()
    {
        await Assert
            .That(LuaNumber.MinInteger.AsInteger)
            .IsEqualTo(long.MinValue)
            .ConfigureAwait(false);
        await Assert.That(LuaNumber.MinInteger.IsInteger).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task LuaTypeNameReturnsCorrectString()
    {
        LuaNumber integer = LuaNumber.FromInteger(5);
        LuaNumber floatNum = LuaNumber.FromFloat(5.5);

        await Assert.That(integer.LuaTypeName).IsEqualTo("integer").ConfigureAwait(false);
        await Assert.That(floatNum.LuaTypeName).IsEqualTo("float").ConfigureAwait(false);
    }

    // Arithmetic operations - Addition

    [Test]
    public async Task AddIntegerPlusIntegerReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(20);
        LuaNumber result = LuaNumber.Add(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(30L).ConfigureAwait(false);
    }

    [Test]
    public async Task AddIntegerPlusFloatReturnsFloat()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromFloat(5.5);
        LuaNumber result = LuaNumber.Add(a, b);

        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(15.5).ConfigureAwait(false);
    }

    [Test]
    public async Task AddIntegerOverflowWraps()
    {
        LuaNumber a = LuaNumber.MaxInteger;
        LuaNumber b = LuaNumber.FromInteger(1);
        LuaNumber result = LuaNumber.Add(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(long.MinValue).ConfigureAwait(false);
    }

    // Arithmetic operations - Subtraction

    [Test]
    public async Task SubtractIntegerMinusIntegerReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(30);
        LuaNumber b = LuaNumber.FromInteger(10);
        LuaNumber result = LuaNumber.Subtract(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(20L).ConfigureAwait(false);
    }

    [Test]
    public async Task SubtractIntegerUnderflowWraps()
    {
        LuaNumber a = LuaNumber.MinInteger;
        LuaNumber b = LuaNumber.FromInteger(1);
        LuaNumber result = LuaNumber.Subtract(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(long.MaxValue).ConfigureAwait(false);
    }

    // Arithmetic operations - Multiplication

    [Test]
    public async Task MultiplyIntegerTimesIntegerReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(6);
        LuaNumber b = LuaNumber.FromInteger(7);
        LuaNumber result = LuaNumber.Multiply(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task MultiplyIntegerTimesFloatReturnsFloat()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromFloat(2.5);
        LuaNumber result = LuaNumber.Multiply(a, b);

        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(25.0).ConfigureAwait(false);
    }

    // Arithmetic operations - Division

    [Test]
    public async Task DivideAlwaysReturnsFloat()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(2);
        LuaNumber result = LuaNumber.Divide(a, b);

        // Per Lua spec §3.4.1, regular division always returns float
        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(5.0).ConfigureAwait(false);
    }

    [Test]
    public async Task DivideByZeroReturnsInfinity()
    {
        LuaNumber a = LuaNumber.FromFloat(10.0);
        LuaNumber b = LuaNumber.FromFloat(0.0);
        LuaNumber result = LuaNumber.Divide(a, b);

        await Assert.That(double.IsPositiveInfinity(result.AsFloat)).IsTrue().ConfigureAwait(false);
    }

    // Floor Division

    [Test]
    public async Task FloorDivideIntegerByIntegerReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(17);
        LuaNumber b = LuaNumber.FromInteger(5);
        LuaNumber result = LuaNumber.FloorDivide(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(3L).ConfigureAwait(false);
    }

    [Test]
    public async Task FloorDivideNegativeIntegerFollowsFloorSemantics()
    {
        // Lua floor division: -17 // 5 = floor(-17/5) = floor(-3.4) = -4
        LuaNumber a = LuaNumber.FromInteger(-17);
        LuaNumber b = LuaNumber.FromInteger(5);
        LuaNumber result = LuaNumber.FloorDivide(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(-4L).ConfigureAwait(false);
    }

    [Test]
    public async Task FloorDivideIntegerByZeroThrowsError()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(0);

        await Assert
            .That(() => LuaNumber.FloorDivide(a, b))
            .Throws<ScriptRuntimeException>()
            .ConfigureAwait(false);
    }

    [Test]
    public async Task FloorDivideFloatByZeroReturnsInfinity()
    {
        LuaNumber a = LuaNumber.FromFloat(10.0);
        LuaNumber b = LuaNumber.FromFloat(0.0);
        LuaNumber result = LuaNumber.FloorDivide(a, b);

        await Assert.That(double.IsPositiveInfinity(result.AsFloat)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task FloorDivideIntegerByFloatReturnsFloat()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromFloat(3.0);
        LuaNumber result = LuaNumber.FloorDivide(a, b);

        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(3.0).ConfigureAwait(false);
    }

    // Modulo

    [Test]
    public async Task ModuloIntegerByIntegerReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(17);
        LuaNumber b = LuaNumber.FromInteger(5);
        LuaNumber result = LuaNumber.Modulo(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(2L).ConfigureAwait(false);
    }

    [Test]
    public async Task ModuloNegativeFollowsLuaSemantics()
    {
        // Lua: -17 % 5 = -17 - floor(-17/5) * 5 = -17 - (-4) * 5 = -17 + 20 = 3
        LuaNumber a = LuaNumber.FromInteger(-17);
        LuaNumber b = LuaNumber.FromInteger(5);
        LuaNumber result = LuaNumber.Modulo(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(3L).ConfigureAwait(false);
    }

    [Test]
    public async Task ModuloByZeroThrowsError()
    {
        // Default Modulo (no version) uses Lua 5.4 behavior: throws error
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(0);

        await Assert
            .That(() => LuaNumber.Modulo(a, b))
            .Throws<ScriptRuntimeException>()
            .ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    public async Task ModuloByZeroReturnsNaNInLua51And52(LuaCompatibilityVersion version)
    {
        // In Lua 5.1/5.2, integer modulo by zero returns nan (promotes to float)
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(0);
        LuaNumber result = LuaNumber.Modulo(a, b, version);

        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(double.IsNaN(result.AsFloat)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    [Arguments(LuaCompatibilityVersion.Latest)]
    public async Task ModuloByZeroThrowsErrorInLua53Plus(LuaCompatibilityVersion version)
    {
        // In Lua 5.3+, integer modulo by zero throws error
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(0);

        await Assert
            .That(() => LuaNumber.Modulo(a, b, version))
            .Throws<ScriptRuntimeException>()
            .ConfigureAwait(false);
    }

    // Power

    [Test]
    public async Task PowerAlwaysReturnsFloat()
    {
        LuaNumber a = LuaNumber.FromInteger(2);
        LuaNumber b = LuaNumber.FromInteger(10);
        LuaNumber result = LuaNumber.Power(a, b);

        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(1024.0).ConfigureAwait(false);
    }

    // Negate

    [Test]
    public async Task NegateIntegerReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber result = LuaNumber.Negate(a);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(-42L).ConfigureAwait(false);
    }

    [Test]
    public async Task NegateMinIntegerWraps()
    {
        // Negating MinValue wraps to MinValue (two's complement)
        LuaNumber a = LuaNumber.MinInteger;
        LuaNumber result = LuaNumber.Negate(a);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(long.MinValue).ConfigureAwait(false);
    }

    [Test]
    public async Task NegateFloatReturnsFloat()
    {
        LuaNumber a = LuaNumber.FromFloat(3.14);
        LuaNumber result = LuaNumber.Negate(a);

        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(-3.14).ConfigureAwait(false);
    }

    // Bitwise operations

    [Test]
    public async Task BitwiseAndReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(0xFF);
        LuaNumber b = LuaNumber.FromInteger(0x0F);
        LuaNumber result = LuaNumber.BitwiseAnd(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(0x0FL).ConfigureAwait(false);
    }

    [Test]
    public async Task BitwiseOrReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(0xF0);
        LuaNumber b = LuaNumber.FromInteger(0x0F);
        LuaNumber result = LuaNumber.BitwiseOr(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(0xFFL).ConfigureAwait(false);
    }

    [Test]
    public async Task BitwiseXorReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(0xFF);
        LuaNumber b = LuaNumber.FromInteger(0x0F);
        LuaNumber result = LuaNumber.BitwiseXor(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(0xF0L).ConfigureAwait(false);
    }

    [Test]
    public async Task BitwiseNotReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(0);
        LuaNumber result = LuaNumber.BitwiseNot(a);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(-1L).ConfigureAwait(false);
    }

    [Test]
    public async Task ShiftLeftReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(1);
        LuaNumber b = LuaNumber.FromInteger(8);
        LuaNumber result = LuaNumber.ShiftLeft(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(256L).ConfigureAwait(false);
    }

    [Test]
    public async Task ShiftRightReturnsInteger()
    {
        LuaNumber a = LuaNumber.FromInteger(256);
        LuaNumber b = LuaNumber.FromInteger(4);
        LuaNumber result = LuaNumber.ShiftRight(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(16L).ConfigureAwait(false);
    }

    [Test]
    public async Task ShiftRightLargeShiftReturnsZero()
    {
        LuaNumber a = LuaNumber.FromInteger(-1);
        LuaNumber b = LuaNumber.FromInteger(64);
        LuaNumber result = LuaNumber.ShiftRight(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(0L).ConfigureAwait(false);
    }

    [Test]
    public async Task BitwiseOnFloatWithIntegerValueSucceeds()
    {
        LuaNumber a = LuaNumber.FromFloat(10.0); // Integer-like float
        LuaNumber b = LuaNumber.FromFloat(3.0);
        LuaNumber result = LuaNumber.BitwiseAnd(a, b);

        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(2L).ConfigureAwait(false);
    }

    [Test]
    public async Task BitwiseOnNonIntegerFloatThrowsError()
    {
        LuaNumber a = LuaNumber.FromFloat(10.5);
        LuaNumber b = LuaNumber.FromInteger(3);

        await Assert
            .That(() => LuaNumber.BitwiseAnd(a, b))
            .Throws<ScriptRuntimeException>()
            .ConfigureAwait(false);
    }

    // Comparison operations

    [Test]
    public async Task EqualIntegersAreEqual()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber b = LuaNumber.FromInteger(42);

        await Assert.That(LuaNumber.Equal(a, b)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task EqualIntegerAndEquivalentFloatAreEqual()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber b = LuaNumber.FromFloat(42.0);

        await Assert.That(LuaNumber.Equal(a, b)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task DifferentValuesAreNotEqual()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber b = LuaNumber.FromInteger(43);

        await Assert.That(LuaNumber.Equal(a, b)).IsFalse().ConfigureAwait(false);
    }

    [Test]
    public async Task NaNIsNotEqualToItself()
    {
        LuaNumber nan = LuaNumber.NaN;

        await Assert.That(LuaNumber.Equal(nan, nan)).IsFalse().ConfigureAwait(false);
    }

    [Test]
    public async Task LessThanComparesCorrectly()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(20);

        await Assert.That(LuaNumber.LessThan(a, b)).IsTrue().ConfigureAwait(false);
        await Assert.That(LuaNumber.LessThan(b, a)).IsFalse().ConfigureAwait(false);
    }

    [Test]
    public async Task LessThanOrEqualComparesCorrectly()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(10);
        LuaNumber c = LuaNumber.FromInteger(20);

        await Assert.That(LuaNumber.LessThanOrEqual(a, b)).IsTrue().ConfigureAwait(false);
        await Assert.That(LuaNumber.LessThanOrEqual(a, c)).IsTrue().ConfigureAwait(false);
        await Assert.That(LuaNumber.LessThanOrEqual(c, a)).IsFalse().ConfigureAwait(false);
    }

    // ToString

    [Test]
    public async Task ToStringForIntegerHasNoDecimalPoint()
    {
        LuaNumber num = LuaNumber.FromInteger(42);

        await Assert.That(num.ToString()).IsEqualTo("42").ConfigureAwait(false);
    }

    [Test]
    public async Task ToStringForIntegerLikeFloatHasDecimalZero()
    {
        LuaNumber num = LuaNumber.FromFloat(42.0);

        await Assert.That(num.ToString()).IsEqualTo("42.0").ConfigureAwait(false);
    }

    [Test]
    public async Task ToStringForNaNReturnsNan()
    {
        LuaNumber num = LuaNumber.NaN;

        await Assert.That(num.ToString()).IsEqualTo("nan").ConfigureAwait(false);
    }

    [Test]
    public async Task ToStringForPositiveInfinityReturnsInf()
    {
        LuaNumber num = LuaNumber.PositiveInfinity;

        await Assert.That(num.ToString()).IsEqualTo("inf").ConfigureAwait(false);
    }

    [Test]
    public async Task ToStringForNegativeInfinityReturnsNegInf()
    {
        LuaNumber num = LuaNumber.NegativeInfinity;

        await Assert.That(num.ToString()).IsEqualTo("-inf").ConfigureAwait(false);
    }

    // TryParse

    [Test]
    public async Task TryParseIntegerSucceeds()
    {
        bool success = LuaNumber.TryParse("42", out LuaNumber result);

        await Assert.That(success).IsTrue().ConfigureAwait(false);
        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task TryParseFloatSucceeds()
    {
        bool success = LuaNumber.TryParse("3.14", out LuaNumber result);

        await Assert.That(success).IsTrue().ConfigureAwait(false);
        await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsFloat).IsEqualTo(3.14).ConfigureAwait(false);
    }

    [Test]
    public async Task TryParseHexIntegerSucceeds()
    {
        bool success = LuaNumber.TryParse("0x1F", out LuaNumber result);

        await Assert.That(success).IsTrue().ConfigureAwait(false);
        await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(result.AsInteger).IsEqualTo(31L).ConfigureAwait(false);
    }

    [Test]
    public async Task TryParseInvalidStringFails()
    {
        bool success = LuaNumber.TryParse("not a number", out LuaNumber result);

        await Assert.That(success).IsFalse().ConfigureAwait(false);
    }

    [Test]
    public async Task TryParseEmptyStringFails()
    {
        bool success = LuaNumber.TryParse("", out LuaNumber result);

        await Assert.That(success).IsFalse().ConfigureAwait(false);
    }

    // TryToInteger

    [Test]
    public async Task TryToIntegerFromIntegerSucceeds()
    {
        LuaNumber num = LuaNumber.FromInteger(42);
        bool success = num.TryToInteger(out long value);

        await Assert.That(success).IsTrue().ConfigureAwait(false);
        await Assert.That(value).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task TryToIntegerFromIntegerLikeFloatSucceeds()
    {
        LuaNumber num = LuaNumber.FromFloat(42.0);
        bool success = num.TryToInteger(out long value);

        await Assert.That(success).IsTrue().ConfigureAwait(false);
        await Assert.That(value).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task TryToIntegerFromNonIntegerFloatFails()
    {
        LuaNumber num = LuaNumber.FromFloat(3.14);
        bool success = num.TryToInteger(out long _);

        await Assert.That(success).IsFalse().ConfigureAwait(false);
    }

    [Test]
    public async Task TryToIntegerFromNaNFails()
    {
        LuaNumber num = LuaNumber.NaN;
        bool success = num.TryToInteger(out long _);

        await Assert.That(success).IsFalse().ConfigureAwait(false);
    }

    // Operators

    [Test]
    public async Task OperatorPlusWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(20);
        LuaNumber result = a + b;

        await Assert.That(result.AsInteger).IsEqualTo(30L).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorMinusWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(30);
        LuaNumber b = LuaNumber.FromInteger(10);
        LuaNumber result = a - b;

        await Assert.That(result.AsInteger).IsEqualTo(20L).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorUnaryMinusWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber result = -a;

        await Assert.That(result.AsInteger).IsEqualTo(-42L).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorMultiplyWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(6);
        LuaNumber b = LuaNumber.FromInteger(7);
        LuaNumber result = a * b;

        await Assert.That(result.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorDivideWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(4);
        LuaNumber result = a / b;

        await Assert.That(result.AsFloat).IsEqualTo(2.5).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorModuloWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(17);
        LuaNumber b = LuaNumber.FromInteger(5);
        LuaNumber result = a % b;

        await Assert.That(result.AsInteger).IsEqualTo(2L).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorBitwiseAndWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(0xFF);
        LuaNumber b = LuaNumber.FromInteger(0x0F);
        LuaNumber result = a & b;

        await Assert.That(result.AsInteger).IsEqualTo(0x0FL).ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorEqualityWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber b = LuaNumber.FromInteger(42);
        LuaNumber c = LuaNumber.FromInteger(43);

        await Assert.That(a == b).IsTrue().ConfigureAwait(false);
        await Assert.That(a != c).IsTrue().ConfigureAwait(false);
    }

    [Test]
    public async Task OperatorComparisonWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(10);
        LuaNumber b = LuaNumber.FromInteger(20);

        await Assert.That(a < b).IsTrue().ConfigureAwait(false);
        await Assert.That(b > a).IsTrue().ConfigureAwait(false);
        await Assert.That(a <= b).IsTrue().ConfigureAwait(false);
        await Assert.That(b >= a).IsTrue().ConfigureAwait(false);
    }

    // Implicit conversions

    [Test]
    public async Task ImplicitConversionFromIntWorks()
    {
        LuaNumber num = 42;

        await Assert.That(num.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task ImplicitConversionFromLongWorks()
    {
        LuaNumber num = 42L;

        await Assert.That(num.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task ImplicitConversionFromDoubleWorks()
    {
        LuaNumber num = 3.14;

        await Assert.That(num.IsFloat).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsFloat).IsEqualTo(3.14).ConfigureAwait(false);
    }

    // Hash code

    [Test]
    public async Task EqualValuesHaveSameHashCode()
    {
        LuaNumber a = LuaNumber.FromInteger(42);
        LuaNumber b = LuaNumber.FromFloat(42.0);

        // They are equal per Lua semantics
        await Assert.That(LuaNumber.Equal(a, b)).IsTrue().ConfigureAwait(false);
        // So they must have the same hash code
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode()).ConfigureAwait(false);
    }

    // Named alternate methods (CA2225)

    [Test]
    public async Task FromInt32NamedAlternateWorks()
    {
        LuaNumber num = LuaNumber.FromInt32(42);

        await Assert.That(num.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task FromInt64NamedAlternateWorks()
    {
        LuaNumber num = LuaNumber.FromInt64(42L);

        await Assert.That(num.IsInteger).IsTrue().ConfigureAwait(false);
        await Assert.That(num.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task ToDoubleValueNamedAlternateWorks()
    {
        LuaNumber num = LuaNumber.FromInteger(42);
        double value = num.ToDoubleValue();

        await Assert.That(value).IsEqualTo(42.0).ConfigureAwait(false);
    }

    [Test]
    public async Task ToInt64NamedAlternateWorks()
    {
        LuaNumber num = LuaNumber.FromFloat(42.9);
        long value = num.ToInt64();

        await Assert.That(value).IsEqualTo(42L).ConfigureAwait(false);
    }

    [Test]
    public async Task RemainderNamedAlternateWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(17);
        LuaNumber b = LuaNumber.FromInteger(5);
        LuaNumber result = LuaNumber.Remainder(a, b);

        await Assert.That(result.AsInteger).IsEqualTo(2L).ConfigureAwait(false);
    }

    [Test]
    public async Task XorNamedAlternateWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(0xFF);
        LuaNumber b = LuaNumber.FromInteger(0x0F);
        LuaNumber result = LuaNumber.Xor(a, b);

        await Assert.That(result.AsInteger).IsEqualTo(0xF0L).ConfigureAwait(false);
    }

    [Test]
    public async Task OnesComplementNamedAlternateWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(0);
        LuaNumber result = LuaNumber.OnesComplement(a);

        await Assert.That(result.AsInteger).IsEqualTo(-1L).ConfigureAwait(false);
    }

    [Test]
    public async Task LeftShiftNamedAlternateWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(1);
        LuaNumber b = LuaNumber.FromInteger(8);
        LuaNumber result = LuaNumber.LeftShift(a, b);

        await Assert.That(result.AsInteger).IsEqualTo(256L).ConfigureAwait(false);
    }

    [Test]
    public async Task RightShiftNamedAlternateWorks()
    {
        LuaNumber a = LuaNumber.FromInteger(256);
        LuaNumber b = LuaNumber.FromInteger(4);
        LuaNumber result = LuaNumber.RightShift(a, b);

        await Assert.That(result.AsInteger).IsEqualTo(16L).ConfigureAwait(false);
    }
}
