namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    public sealed class ProcessorIndexingTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecIndexSetThrowsWhenUserDataDescriptorRejectsField()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();

            DynValue value = DynValue.NewNumber(7);
            IUserDataDescriptor descriptor = new RejectingUserDataDescriptor();
            DynValue userdata = UserData.Create(new RejectingUserData(), descriptor);

            stack.Push(value);
            stack.Push(userdata);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.IndexSetN,
                Value = DynValue.NewString("missing"),
                NumVal = 0,
                NumVal2 = 0,
            };

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                processor.ExecIndexSetForTests(instruction, 0)
            );

            await Assert.That(exception.Message).Contains("missing");
        }

        [global::TUnit.Core.Test]
        public async Task ExecIndexThrowsWhenMultiIndexingThroughMetamethod()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();

            Table table = new(script);
            Table meta = new(script);
            meta.Set("__index", DynValue.NewCallback((ctx, args) => DynValue.NewString("ignored")));
            table.MetaTable = meta;

            stack.Push(DynValue.NewTable(table));

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.IndexL,
                Value = DynValue.NewString("field"),
            };

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                processor.ExecIndexForTests(instruction, 0)
            );

            await Assert.That(exception.Message).Contains("cannot multi-index through metamethods");

            stack.Clear();
            DynValue legacyUserData = UserData.Create(
                new RejectingUserData(),
                new RejectingUserDataDescriptor()
            );
            stack.Push(legacyUserData);
            Instruction legacyInstruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.IndexN,
                Value = DynValue.NewString("legacy-nil"),
            };

            processor.ExecIndexForTests(legacyInstruction, 0);

            await Assert.That(stack.Pop().IsNil()).IsTrue();

            PresenceAwareUserDataDescriptor presenceDescriptor = new();
            DynValue presenceUserData = UserData.Create(
                new RejectingUserData(),
                presenceDescriptor
            );
            Instruction presenceInstruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.IndexN,
                Value = DynValue.NewString("handled"),
            };
            stack.Push(presenceUserData);

            processor.ExecIndexForTests(presenceInstruction, 0);

            await Assert.That(stack.Pop().IsVoid()).IsTrue();

            stack.Push(presenceUserData);
            presenceInstruction.Value = DynValue.NewString("missing");
            ScriptRuntimeException missingException = ExpectException<ScriptRuntimeException>(() =>
                processor.ExecIndexForTests(presenceInstruction, 0)
            );

            await Assert.That(missingException.Message).Contains("missing");
        }

        private sealed class RejectingUserData { }

        private sealed class RejectingUserDataDescriptor : IUserDataDescriptor
        {
            public string Name => nameof(RejectingUserData);

            public Type Type => typeof(RejectingUserData);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                return DynValue.Nil;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                return Name;
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return null;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return obj is RejectingUserData;
            }
        }

        private sealed class PresenceAwareUserDataDescriptor : IUserDataDescriptorTryAccess
        {
            public string Name => nameof(PresenceAwareUserDataDescriptor);

            public Type Type => typeof(RejectingUserData);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                return TryIndex(script, obj, index, isDirectIndexing, out DynValue value)
                    ? value
                    : null;
            }

            public bool TryIndex(
                Script script,
                object obj,
                DynValue index,
                bool isDirectIndexing,
                out DynValue value
            )
            {
                if (index.String == "handled")
                {
                    value = DynValue.Void;
                    return true;
                }

                value = DynValue.Nil;
                return false;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                return Name;
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return null;
            }

            public bool TryMetaIndex(Script script, object obj, string metaname, out DynValue value)
            {
                value = DynValue.Nil;
                return false;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return obj is RejectingUserData;
            }
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
