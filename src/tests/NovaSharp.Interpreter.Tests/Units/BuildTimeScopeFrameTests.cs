namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BuildTimeScopeFrameTests
    {
        [Test]
        public void PopBlockThrowsWhenStackUnderflows()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            Assert.That(() => frame.PopBlock(), Throws.TypeOf<InternalErrorException>());
        }

        [Test]
        public void PopBlockReturnsScopeBlockAfterPush()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            frame.PushBlock();

            RuntimeScopeBlock block = frame.PopBlock();

            Assert.That(block, Is.Not.Null);
            Assert.That(block.ToInclusive, Is.EqualTo(0));
        }

        [Test]
        public void GetRuntimeFrameDataThrowsWhenBlocksAreUnbalanced()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            frame.PushBlock();

            Assert.That(() => frame.GetRuntimeFrameData(), Throws.TypeOf<InternalErrorException>());
        }

        [Test]
        public void GetRuntimeFrameDataReturnsFrameAfterResolve()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            frame.ResolveLRefs();

            RuntimeScopeFrame runtimeFrame = frame.GetRuntimeFrameData();

            Assert.That(runtimeFrame, Is.Not.Null);
        }

        [Test]
        public void TryDefineLocalRenamesExistingSymbols()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            SymbolRef first = frame.TryDefineLocal("value");

            SymbolRef second = frame.TryDefineLocal("value");

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(frame.Find("value"), Is.SameAs(second));
            });
        }
    }
}
