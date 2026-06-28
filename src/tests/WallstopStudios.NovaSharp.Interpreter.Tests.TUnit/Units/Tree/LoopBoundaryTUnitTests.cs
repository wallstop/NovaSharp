namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class LoopBoundaryTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IsBoundaryReturnsTrue()
        {
            LoopBoundary boundary = new();
            await Assert.That(boundary.IsBoundary()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileBreakThrowsInternalError(LuaCompatibilityVersion version)
        {
            LoopBoundary boundary = new();
            ByteCode byteCode = new(new Script(version));

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                boundary.CompileBreak(byteCode)
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }
    }
}
