namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.Scopes;

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

            DynValue[] values = new[] { DynValue.NewNumber(1), DynValue.NewString("two") };

            ClosureContext context = new(symbols, values);

            await Assert
                .That(context.Symbols.SequenceEqual(ExpectedSymbols))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(context.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(context[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(context[1].String).IsEqualTo("two").ConfigureAwait(false);
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
            DynValue[] values = new[] { DynValue.NewNumber(1) };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                _ = new ClosureContext(null, values)
            );

            await Assert.That(exception.ParamName).IsEqualTo("symbols").ConfigureAwait(false);
        }
    }
}
