namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RuntimeScopeFrameTests
    {
        [Test]
        public void ConstructorInitializesDebugSymbolsList()
        {
            RuntimeScopeFrame frame = new();

            Assert.Multiple(() =>
            {
                Assert.That(frame.DebugSymbols, Is.Not.Null);
                Assert.That(frame.Count, Is.EqualTo(0));
                Assert.That(frame.ToString(), Is.EqualTo("ScopeFrame : #0"));
            });
        }

        [Test]
        public void CountTracksDebugSymbolMutations()
        {
            RuntimeScopeFrame frame = new();
            frame.DebugSymbols.Add(SymbolRef.Local("a", 0));
            frame.DebugSymbols.Add(SymbolRef.UpValue("b", 1));

            Assert.Multiple(() =>
            {
                Assert.That(frame.Count, Is.EqualTo(2));
                Assert.That(frame.DebugSymbols[1].Name, Is.EqualTo("b"));
                Assert.That(frame.ToString(), Is.EqualTo("ScopeFrame : #2"));
            });
        }

        [Test]
        public void ToFirstBlockStoresAssignedOffset()
        {
            RuntimeScopeFrame frame = new() { ToFirstBlock = 3 };

            Assert.That(frame.ToFirstBlock, Is.EqualTo(3));

            frame.ToFirstBlock = 0;
            Assert.That(frame.ToFirstBlock, Is.EqualTo(0));
        }
    }
}
