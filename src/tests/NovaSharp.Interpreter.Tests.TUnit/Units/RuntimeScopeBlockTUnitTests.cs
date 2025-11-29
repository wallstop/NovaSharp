#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.Scopes;

    public sealed class RuntimeScopeBlockTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultsUseEmptyToBeClosedArray()
        {
            RuntimeScopeBlock block = new();
            await Assert.That(block.ToBeClosed).IsNotNull();
            await Assert.That(block.ToBeClosed.Length).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task PropertiesAreMutableInsideTests()
        {
            RuntimeScopeBlock block = new()
            {
                From = 10,
                To = 20,
                ToInclusive = 30,
                ToBeClosed = new[] { SymbolRef.Local("value", 0) },
            };

            await Assert.That(block.From).IsEqualTo(10);
            await Assert.That(block.To).IsEqualTo(20);
            await Assert.That(block.ToInclusive).IsEqualTo(30);
            await Assert.That(block.ToBeClosed.Length).IsEqualTo(1);
            await Assert.That(block.ToBeClosed[0].Name).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task ToStringPrintsRangeInformation()
        {
            RuntimeScopeBlock block = new()
            {
                From = 1,
                To = 2,
                ToInclusive = 3,
            };

            await Assert.That(block.ToString()).IsEqualTo("ScopeBlock : 1 -> 2 --> 3");
        }
    }
}
#pragma warning restore CA2007
