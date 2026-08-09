namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    public sealed class ProcessorSymbolHelpersTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task FindSymbolByNameFallsBackToGlobalWhenStackEmpty()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = processor.FindSymbolByName("missingVar");

            await Assert.That(symbol.SymbolType).IsEqualTo(SymbolRefType.Global);
            await Assert.That(symbol.EnvironmentRef.SymbolType).IsEqualTo(SymbolRefType.DefaultEnv);
            await Assert.That(symbol.Name).IsEqualTo("missingVar");
        }

        [global::TUnit.Core.Test]
        public async Task GetGlobalSymbolThrowsWhenEnvIsNotTable()
        {
            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                Processor.GetGlobalSymbolForTests(LuaValue.NewNumber(1), "value")
            );
            await Assert.That(exception.Message).Contains("_ENV is not a table");
        }

        [global::TUnit.Core.Test]
        public async Task InternalAdjustTupleReturnsEmptyArrayWhenValuesNull()
        {
            LuaValue[] result = Processor.InternalAdjustTupleForTests(null);
            await Assert.That(result.Length).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task InternalAdjustTupleFlattensNestedTupleTail()
        {
            LuaValue nested = LuaValue.NewTuple(LuaValue.NewNumber(3));
            LuaValue[] values =
            {
                LuaValue.NewNumber(1),
                LuaValue.NewTuple(LuaValue.NewNumber(2), nested),
            };

            LuaValue[] result = Processor.InternalAdjustTupleForTests(values);

            double[] numbers = result.Select(v => v.Number).ToArray();
            await Assert.That(numbers.Length).IsEqualTo(3);
            await Assert.That(numbers[0]).IsEqualTo(1d);
            await Assert.That(numbers[1]).IsEqualTo(2d);
            await Assert.That(numbers[2]).IsEqualTo(3d);
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
