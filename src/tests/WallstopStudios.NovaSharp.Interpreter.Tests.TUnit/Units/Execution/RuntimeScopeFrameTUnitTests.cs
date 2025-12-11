namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;

    public sealed class RuntimeScopeFrameTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorInitializesDebugSymbolsList()
        {
            RuntimeScopeFrame frame = new();

            await Assert.That(frame.DebugSymbols).IsNotNull().ConfigureAwait(false);
            await Assert.That(frame.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(frame.ToString()).IsEqualTo("ScopeFrame : #0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CountTracksDebugSymbolMutations()
        {
            RuntimeScopeFrame frame = new();
            frame.DebugSymbols.Add(SymbolRef.Local("a", 0));
            frame.DebugSymbols.Add(SymbolRef.UpValue("b", 1));

            await Assert.That(frame.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(frame.DebugSymbols[1].Name).IsEqualTo("b").ConfigureAwait(false);
            await Assert.That(frame.ToString()).IsEqualTo("ScopeFrame : #2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToFirstBlockStoresAssignedOffset()
        {
            RuntimeScopeFrame frame = new() { ToFirstBlock = 3 };

            await Assert.That(frame.ToFirstBlock).IsEqualTo(3).ConfigureAwait(false);

            frame.ToFirstBlock = 0;
            await Assert.That(frame.ToFirstBlock).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
