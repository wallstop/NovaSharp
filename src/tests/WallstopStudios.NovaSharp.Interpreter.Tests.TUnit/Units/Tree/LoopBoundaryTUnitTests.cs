namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree;

    public sealed class LoopBoundaryTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IsBoundaryReturnsTrue()
        {
            LoopBoundary boundary = new();
            await Assert.That(boundary.IsBoundary()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompileBreakThrowsInternalError()
        {
            LoopBoundary boundary = new();
            ByteCode byteCode = new(new Script());

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                boundary.CompileBreak(byteCode)
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }
    }
}
