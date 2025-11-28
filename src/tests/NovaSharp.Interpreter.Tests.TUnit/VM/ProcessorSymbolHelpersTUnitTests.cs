namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.VM;

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
                Processor.GetGlobalSymbolForTests(DynValue.NewNumber(1), "value")
            );
            await Assert.That(exception.Message).Contains("_ENV is not a table");
        }

        [global::TUnit.Core.Test]
        public async Task InternalAdjustTupleReturnsEmptyArrayWhenValuesNull()
        {
            DynValue[] result = Processor.InternalAdjustTupleForTests(null);
            await Assert.That(result.Length).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task InternalAdjustTupleFlattensNestedTupleTail()
        {
            DynValue nested = DynValue.NewTuple(DynValue.NewNumber(3));
            DynValue[] values =
            {
                DynValue.NewNumber(1),
                DynValue.NewTuple(DynValue.NewNumber(2), nested),
            };

            DynValue[] result = Processor.InternalAdjustTupleForTests(values);

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
