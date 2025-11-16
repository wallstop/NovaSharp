namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LoopBoundaryTests
    {
        [Test]
        public void IsBoundaryReturnsTrue()
        {
            LoopBoundary boundary = new();

            Assert.That(boundary.IsBoundary(), Is.True);
        }

        [Test]
        public void CompileBreakThrowsInternalError()
        {
            LoopBoundary boundary = new();
            ByteCode byteCode = new(new Script());

            Assert.That(
                () => boundary.CompileBreak(byteCode),
                Throws.TypeOf<InternalErrorException>()
            );
        }
    }
}
