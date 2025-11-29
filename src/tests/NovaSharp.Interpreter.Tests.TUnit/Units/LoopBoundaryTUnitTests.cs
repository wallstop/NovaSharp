#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;

    public sealed class LoopBoundaryTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IsBoundaryReturnsTrue()
        {
            LoopBoundary boundary = new();
            await Assert.That(boundary.IsBoundary()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CompileBreakThrowsInternalError()
        {
            LoopBoundary boundary = new();
            ByteCode byteCode = new(new Script());

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                boundary.CompileBreak(byteCode)
            );
            await Assert.That(exception).IsNotNull();
        }
    }
}
#pragma warning restore CA2007
