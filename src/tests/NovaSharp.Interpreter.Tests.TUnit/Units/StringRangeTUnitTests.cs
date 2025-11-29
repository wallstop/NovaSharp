#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.CoreLib.StringLib;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class StringRangeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task FromLuaRangeDefaultsStartAndEndWhenNil()
        {
            StringRange range = StringRange.FromLuaRange(DynValue.Nil, DynValue.Nil, 5);

            await Assert.That(range.Start).IsEqualTo(1);
            await Assert.That(range.End).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        public async Task FromLuaRangeUsesStartWhenEndMissing()
        {
            StringRange range = StringRange.FromLuaRange(DynValue.NewNumber(3), DynValue.Nil);

            await Assert.That(range.Start).IsEqualTo(3);
            await Assert.That(range.End).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task ApplyToStringSupportsNegativeIndices()
        {
            StringRange range = StringRange.FromLuaRange(
                DynValue.NewNumber(-5),
                DynValue.NewNumber(-2)
            );

            string result = range.ApplyToString("NovaSharp");

            await Assert.That(result).IsEqualTo("Shar");
        }

        [global::TUnit.Core.Test]
        public async Task ApplyToStringClampsIndicesToBounds()
        {
            StringRange range = new(0, 50);

            string result = range.ApplyToString("Lua");

            await Assert.That(result).IsEqualTo("Lua");
        }

        [global::TUnit.Core.Test]
        public async Task ApplyToStringReturnsEmptyWhenStartExceedsEnd()
        {
            StringRange range = new(5, 2);

            string result = range.ApplyToString("Nova");

            await Assert.That(result).IsEmpty();
        }

        [global::TUnit.Core.Test]
        public async Task LengthReturnsInclusiveRangeWidth()
        {
            StringRange range = new(2, 6);

            await Assert.That(range.Length()).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultConstructorInitializesBounds()
        {
            StringRange range = new();

            await Assert.That(range.Start).IsEqualTo(0);
            await Assert.That(range.End).IsEqualTo(0);
        }
    }
}
#pragma warning restore CA2007
