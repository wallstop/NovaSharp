#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.Scopes;

    public sealed class RuntimeScopeFrameTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorInitializesDebugSymbolsList()
        {
            RuntimeScopeFrame frame = new();

            await Assert.That(frame.DebugSymbols).IsNotNull();
            await Assert.That(frame.Count).IsEqualTo(0);
            await Assert.That(frame.ToString()).IsEqualTo("ScopeFrame : #0");
        }

        [global::TUnit.Core.Test]
        public async Task CountTracksDebugSymbolMutations()
        {
            RuntimeScopeFrame frame = new();
            frame.DebugSymbols.Add(SymbolRef.Local("a", 0));
            frame.DebugSymbols.Add(SymbolRef.UpValue("b", 1));

            await Assert.That(frame.Count).IsEqualTo(2);
            await Assert.That(frame.DebugSymbols[1].Name).IsEqualTo("b");
            await Assert.That(frame.ToString()).IsEqualTo("ScopeFrame : #2");
        }

        [global::TUnit.Core.Test]
        public async Task ToFirstBlockStoresAssignedOffset()
        {
            RuntimeScopeFrame frame = new() { ToFirstBlock = 3 };

            await Assert.That(frame.ToFirstBlock).IsEqualTo(3);

            frame.ToFirstBlock = 0;
            await Assert.That(frame.ToFirstBlock).IsEqualTo(0);
        }
    }
}
#pragma warning restore CA2007
