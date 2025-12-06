namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Utilities
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib.StringLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    public sealed class StringRangeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task FromLuaRangeDefaultsStartAndEndWhenNil()
        {
            StringRange range = StringRange.FromLuaRange(DynValue.Nil, DynValue.Nil, 5);

            await Assert.That(range.Start).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(range.End).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromLuaRangeUsesStartWhenEndMissing()
        {
            StringRange range = StringRange.FromLuaRange(DynValue.NewNumber(3), DynValue.Nil);

            await Assert.That(range.Start).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(range.End).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ApplyToStringSupportsNegativeIndices()
        {
            StringRange range = StringRange.FromLuaRange(
                DynValue.NewNumber(-5),
                DynValue.NewNumber(-2)
            );

            string result = range.ApplyToString("NovaSharp");

            await Assert.That(result).IsEqualTo("Shar").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ApplyToStringClampsIndicesToBounds()
        {
            StringRange range = new(0, 50);

            string result = range.ApplyToString("Lua");

            await Assert.That(result).IsEqualTo("Lua").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ApplyToStringReturnsEmptyWhenStartExceedsEnd()
        {
            StringRange range = new(5, 2);

            string result = range.ApplyToString("Nova");

            await Assert.That(result).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LengthReturnsInclusiveRangeWidth()
        {
            StringRange range = new(2, 6);

            await Assert.That(range.Length()).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultConstructorInitializesBounds()
        {
            StringRange range = new();

            await Assert.That(range.Start).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(range.End).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
