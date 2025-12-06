namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;

    public sealed class BuildTimeScopeFrameTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PopBlockThrowsWhenStackUnderflows()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                frame.PopBlock()
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PopBlockReturnsScopeBlockAfterPush()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            frame.PushBlock();

            RuntimeScopeBlock block = frame.PopBlock();

            await Assert.That(block).IsNotNull().ConfigureAwait(false);
            await Assert.That(block.ToInclusive).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetRuntimeFrameDataThrowsWhenBlocksAreUnbalanced()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            frame.PushBlock();

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                frame.GetRuntimeFrameData()
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetRuntimeFrameDataReturnsFrameAfterResolve()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            frame.ResolveLRefs();

            RuntimeScopeFrame runtimeFrame = frame.GetRuntimeFrameData();

            await Assert.That(runtimeFrame).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryDefineLocalRenamesExistingSymbols()
        {
            BuildTimeScopeFrame frame = new(hasVarArgs: false);
            SymbolRef first = frame.TryDefineLocal("value");

            SymbolRef second = frame.TryDefineLocal("value");

            await Assert.That(ReferenceEquals(second, first)).IsFalse().ConfigureAwait(false);
            await Assert.That(frame.Find("value")).IsSameReferenceAs(second).ConfigureAwait(false);
        }
    }
}
