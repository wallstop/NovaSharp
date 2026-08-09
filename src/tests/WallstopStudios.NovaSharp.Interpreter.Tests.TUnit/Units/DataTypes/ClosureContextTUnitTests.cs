namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;

    public sealed class ClosureContextTUnitTests
    {
        private static readonly string[] ExpectedSymbols = { "alpha", "beta" };

        [global::TUnit.Core.Test]
        public async Task ConstructorCopiesSymbolNamesAndValues()
        {
            SymbolRef[] symbols = new[]
            {
                SymbolRef.Global("alpha", SymbolRef.DefaultEnv),
                SymbolRef.Global("beta", SymbolRef.DefaultEnv),
            };

            ValueSlot firstSlot = new(LuaValue.NewNumber(1));
            UpvalueCell firstCell = firstSlot.Capture();
            UpvalueCell[] values = new[] { firstCell, new UpvalueCell(LuaValue.NewString("two")) };

            ClosureContext context = new(symbols, values);

            await Assert
                .That(context.Symbols.SequenceEqual(ExpectedSymbols))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(context.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(context[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(context[1].String).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(firstSlot.Capture()).IsSameReferenceAs(firstCell);

            firstSlot.Assign(LuaValue.NewNumber(3));
            await Assert.That(context[0].Number).IsEqualTo(3).ConfigureAwait(false);

            context.GetSlot(0).Value = LuaValue.NewNumber(4);
            await Assert.That(firstSlot.Value.Number).IsEqualTo(4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultConstructorCreatesEmptyContext()
        {
            ClosureContext context = new();

            await Assert
                .That(context.Symbols.SequenceEqual(Array.Empty<string>()))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(context.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsOnNullSymbols()
        {
            UpvalueCell[] values = new[] { new UpvalueCell(LuaValue.NewNumber(1)) };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                _ = new ClosureContext(null, values)
            );

            await Assert.That(exception.ParamName).IsEqualTo("symbols").ConfigureAwait(false);
        }
    }
}
