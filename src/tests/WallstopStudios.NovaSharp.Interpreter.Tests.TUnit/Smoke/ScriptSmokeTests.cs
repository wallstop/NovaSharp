namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Smoke
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    public sealed class ScriptSmokeTests
    {
        [global::TUnit.Core.Test]
        public async Task DoStringEvaluatesExpressions()
        {
            Script script = new Script();
            DynValue result = script.DoString("return 40 + 2");

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(42);
        }
    }
}
