namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;

    public sealed class ProcessorIteratorAndTableOpsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecIterPrepUsesIteratorMetamethod()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<LuaValue> stack = processor.GetValueStackForTests();
            stack.Clear();

            Table iteratorHost = new(script);
            bool called = false;
            Table meta = new(script);
            meta.Set(
                "__iterator",
                LuaValue.NewCallback(
                    (ctx, args) =>
                    {
                        called = true;
                        return LuaValue.NewTuple(
                            LuaValue.NewString("fn"),
                            LuaValue.NewString("state"),
                            LuaValue.NewNumber(5)
                        );
                    }
                )
            );
            iteratorHost.MetaTable = meta;

            stack.Push(
                LuaValue.NewTuple(LuaValue.NewTable(iteratorHost), LuaValue.Nil, LuaValue.Nil)
            );

            processor.ExecIterPrepForTests(new Instruction(SourceRef.GetClrLocation()));

            LuaValue tuple = stack.Peek();
            await Assert.That(called).IsTrue();
            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].String).IsEqualTo("fn");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("state");
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(5d);
        }

        [global::TUnit.Core.Test]
        public async Task ExecIterPrepConvertsPlainTableToEnumerableWrapper()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<LuaValue> stack = processor.GetValueStackForTests();
            stack.Clear();

            Table table = new(script);
            table.Set(1, LuaValue.NewString("value"));

            stack.Push(LuaValue.NewTuple(LuaValue.NewTable(table), LuaValue.Nil, LuaValue.Nil));

            processor.ExecIterPrepForTests(new Instruction(SourceRef.GetClrLocation()));

            LuaValue tuple = stack.Peek();
            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.UserData);
            await Assert
                .That(tuple.Tuple[0].UserData.Descriptor.Type)
                .IsEqualTo(typeof(EnumerableWrapper));
        }

        [global::TUnit.Core.Test]
        public void ExecTblInitIRejectsNonTableTargets()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<LuaValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));

            ExpectException<InternalErrorException>(() =>
                processor.ExecTblInitIForTests(new Instruction(SourceRef.GetClrLocation()))
            );
        }

        [global::TUnit.Core.Test]
        public void ExecTblInitNRejectsNonTableTargets()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<LuaValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewString("not-table"));

            ExpectException<InternalErrorException>(() =>
                processor.ExecTblInitNForTests(new Instruction(SourceRef.GetClrLocation()))
            );
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
