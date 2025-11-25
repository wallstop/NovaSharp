namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Linq;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ClosureContextTests
    {
        private static readonly string[] ExpectedSymbols = { "alpha", "beta" };

        [Test]
        public void ConstructorCopiesSymbolNamesAndValues()
        {
            SymbolRef[] symbols = new[]
            {
                SymbolRef.Global("alpha", SymbolRef.DefaultEnv),
                SymbolRef.Global("beta", SymbolRef.DefaultEnv),
            };

            DynValue[] values = new[] { DynValue.NewNumber(1), DynValue.NewString("two") };

            ClosureContext context = new(symbols, values);

            Assert.Multiple(() =>
            {
                Assert.That(context.Symbols, Is.EqualTo(ExpectedSymbols));
                Assert.That(context.Count, Is.EqualTo(2));
                Assert.That(context[0].Number, Is.EqualTo(1));
                Assert.That(context[1].String, Is.EqualTo("two"));
            });
        }

        [Test]
        public void DefaultConstructorCreatesEmptyContext()
        {
            ClosureContext context = new();
            Assert.Multiple(() =>
            {
                Assert.That(context.Symbols, Is.EqualTo(Enumerable.Empty<string>()));
                Assert.That(context.Count, Is.EqualTo(0));
            });
        }
    }
}
