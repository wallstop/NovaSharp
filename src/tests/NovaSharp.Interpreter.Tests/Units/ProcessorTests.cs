namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.PredefinedUserData;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProcessorTests
    {
        private static readonly double[] DescendingPair = { 3d, 2d };
        private static readonly double[] AscendingTriple = { 2d, 3d, 4d };
        private static readonly double[] AscendingPair = { 1d, 2d };
        private static readonly double[] AscendingPairWithHead = { 1d, 2d, 3d };

        [Test]
        public void CallThrowsWhenEnteredFromDifferentThread()
        {
            Script script = new();
            script.Options.CheckThreadAccess = true;
            DynValue chunk = script.LoadString("return 42");

            Processor processor = script.GetMainProcessorForTests();
            processor.SetThreadOwnershipStateForTests(
                Thread.CurrentThread.ManagedThreadId,
                executionNesting: 1
            );

            InvalidOperationException observed = null;
            Thread worker = new Thread(() =>
            {
                try
                {
                    script.Call(chunk);
                }
                catch (InvalidOperationException ex)
                {
                    observed = ex;
                }
            })
            {
                IsBackground = true,
            };
            worker.Start();
            worker.Join();

            Assert.That(observed, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(observed, Is.TypeOf<InvalidOperationException>());
                Assert.That(
                    observed.Message,
                    Does.Contain("Cannot enter the same NovaSharp processor")
                );
            });

            processor.SetThreadOwnershipStateForTests(-1, executionNesting: 0);
            script.Options.CheckThreadAccess = false;
        }

        [Test]
        public void EnterAndLeaveProcessorUpdateParentCoroutineStack()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);

            List<Processor> stack = parent.GetCoroutineStackForTests();

            child.EnterProcessorForTests();
            Assert.That(stack[^1], Is.SameAs(child));

            child.LeaveProcessorForTests();
            Assert.That(stack, Is.Empty);
        }

        [Test]
        public void PerformMessageDecorationBeforeUnwindUsesClrFunction()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            bool invoked = false;
            DynValue handler = DynValue.NewCallback(
                (ctx, args) =>
                {
                    invoked = true;
                    Assert.That(args[0].String, Is.EqualTo("boom"));
                    return DynValue.NewString("decorated");
                }
            );

            string result = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            Assert.Multiple(() =>
            {
                Assert.That(invoked, Is.True);
                Assert.That(result, Is.EqualTo("decorated"));
            });
        }

        [Test]
        public void PerformMessageDecorationBeforeUnwindThrowsWhenHandlerIsNotFunction()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            DynValue handler = DynValue.NewNumber(42);

            string result = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            Assert.That(result, Is.EqualTo("error handler not set to a function.boom"));
        }

        [Test]
        public void PerformMessageDecorationBeforeUnwindAppendsInnerExceptionMessage()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            DynValue handler = DynValue.NewCallback(
                (ctx, args) =>
                {
                    throw new ScriptRuntimeException("decorator failure");
                }
            );

            string decorated = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            Assert.That(decorated, Is.EqualTo("decorator failure.boom"));
        }

        [Test]
        public void ParentConstructorInitializesState()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);

            Assert.Multiple(() =>
            {
                Assert.That(child.ParentProcessorForTests, Is.SameAs(parent));
                Assert.That(child.State, Is.EqualTo(CoroutineState.NotStarted));
            });
        }

        [Test]
        public void RecycleConstructorReusesStacks()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor recycleSource = Processor.CreateChildProcessorForTests(parent);

            Processor recycled = Processor.CreateRecycledProcessorForTests(parent, recycleSource);

            Assert.Multiple(() =>
            {
                Assert.That(
                    recycled.GetValueStackForTests(),
                    Is.SameAs(recycleSource.GetValueStackForTests())
                );
                Assert.That(
                    recycled.GetExecutionStackForTests(),
                    Is.SameAs(recycleSource.GetExecutionStackForTests())
                );
            });
        }

        [Test]
        public void CallDelegatesToParentCoroutineStackTop()
        {
            Script script = new();
            DynValue function = script.LoadString("return 321");

            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);
            Processor delegated = Processor.CreateChildProcessorForTests(parent);

            parent.ReplaceCoroutineStackForTests(new List<Processor> { delegated });

            DynValue result = child.Call(function, Array.Empty<DynValue>());

            Assert.That(result.Number, Is.EqualTo(321d));
        }

        [Test]
        public void CoroutineYieldPassesValuesWhenYieldingIsAllowed()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function worker()
                    coroutine.yield('pause')
                    return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("worker"));

            DynValue yielded = coroutineValue.Coroutine.Resume();

            Assert.Multiple(() =>
            {
                Assert.That(yielded.Type, Is.EqualTo(DataType.String));
                Assert.That(yielded.String, Is.EqualTo("pause"));
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Suspended));
            });
        }

        [Test]
        public void YieldingFromMainChunkThrowsCannotYieldMain()
        {
            Script script = new(CoreModules.PresetComplete);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("coroutine.yield('outside')")
            );

            Assert.That(ex.Message, Does.Contain("attempt to yield from outside a coroutine"));
        }

        [Test]
        public void YieldingWithClrBoundaryInsideCoroutineThrowsCannotYield()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function boundary()
                    coroutine.yield('pause')
                    return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("boundary"));
            Processor coroutineProcessor = coroutineValue.Coroutine.GetProcessorForTests();
            bool originalCanYield = coroutineProcessor.SwapCanYieldForTests(false);

            try
            {
                ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                    coroutineValue.Coroutine.Resume()
                );
                Assert.That(
                    ex.Message,
                    Does.Contain("attempt to yield across a CLR-call boundary")
                );
            }
            finally
            {
                coroutineProcessor.SwapCanYieldForTests(originalCanYield);
            }
        }

        [Test]
        public void ExecIncrClonesReadOnlyValueBeforeIncrement()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> valueStack = processor.GetValueStackForTests();
            valueStack.Clear();
            valueStack.Push(DynValue.NewNumber(1)); // step value at offset 1
            valueStack.Push(DynValue.NewNumber(2).AsReadOnly());

            Instruction instruction = new(SourceRef.GetClrLocation()) { NumVal = 1 };
            processor.ExecIncrForTests(instruction);

            DynValue result = valueStack.Peek();
            Assert.Multiple(() =>
            {
                Assert.That(result.Number, Is.EqualTo(3));
                Assert.That(result.ReadOnly, Is.False);
            });
        }

        [Test]
        public void ExecCNotThrowsWhenConditionIsNotBoolean()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> valueStack = processor.GetValueStackForTests();
            valueStack.Clear();

            // Order matters: value is pushed last so it is popped first inside ExecCNot.
            valueStack.Push(DynValue.NewString("not-bool"));
            valueStack.Push(DynValue.NewNumber(1));

            InternalErrorException ex = Assert.Throws<InternalErrorException>(() =>
                processor.ExecCNotForTests(new Instruction(SourceRef.GetClrLocation()))
            );
            Assert.That(ex.Message, Does.Contain("CNOT had non-bool arg"));
        }

        [Test]
        public void ExecBeginFnClearsExistingScopeTracking()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef>() },
                ToBeClosedIndices = new HashSet<int> { 0 },
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                NumVal = 1,
                NumVal2 = 0,
                SymbolList = new[] { SymbolRef.Local("tmp", 0) },
            };

            processor.ExecBeginFnForTests(instruction);

            Assert.Multiple(() =>
            {
                Assert.That(frame.BlocksToClose, Is.Null);
                Assert.That(frame.ToBeClosedIndices, Is.Null);
                Assert.That(frame.LocalScope, Is.Not.Null);
            });
        }

        [Test]
        public void PopExecStackAndCheckVStackReturnsReturnAddressWhenGuardMatches()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();
            processor.PushCallStackFrameForTests(
                new CallStackItem() { BasePointer = 3, ReturnAddress = 42 }
            );

            int returnAddress = processor.PopExecStackAndCheckVStackForTests(3);
            Assert.That(returnAddress, Is.EqualTo(42));
        }

        [Test]
        public void PopExecStackAndCheckVStackThrowsWhenGuardDiffers()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();
            processor.PushCallStackFrameForTests(
                new CallStackItem() { BasePointer = 2, ReturnAddress = 7 }
            );

            Assert.That(
                () => processor.PopExecStackAndCheckVStackForTests(0),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void SetGlobalSymbolThrowsWhenEnvIsNotTable()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                Processor.SetGlobalSymbolForTests(
                    DynValue.NewString("not-table"),
                    "value",
                    DynValue.NewNumber(1)
                )
            );
            Assert.That(ex.Message, Does.Contain("_ENV is not a table"));
        }

        [Test]
        public void SetGlobalSymbolStoresValueAndTreatsNullAsNil()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            DynValue env = DynValue.NewTable(script.Globals);

            Processor.SetGlobalSymbolForTests(env, "answer", DynValue.NewNumber(42));
            Assert.That(env.Table.Get("answer").Number, Is.EqualTo(42));

            Processor.SetGlobalSymbolForTests(env, "cleared", null);
            Assert.That(env.Table.Get("cleared").IsNil(), Is.True);
        }

        [Test]
        public void AssignGenericSymbolRejectsDefaultEnvSymbol()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            Assert.That(
                () => processor.AssignGenericSymbol(SymbolRef.DefaultEnv, DynValue.NewNumber(1)),
                Throws
                    .TypeOf<ArgumentException>()
                    .With.Message.Contains("Can't AssignGenericSymbol on a DefaultEnv symbol")
            );
        }

        [Test]
        public void AssignGenericSymbolUpdatesGlobalScope()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            SymbolRef global = SymbolRef.Global("greeting", SymbolRef.DefaultEnv);
            processor.AssignGenericSymbol(global, DynValue.NewString("hello"));

            Assert.That(script.Globals.Get("greeting").String, Is.EqualTo("hello"));
        }

        [Test]
        public void AssignGenericSymbolUpdatesLocalScope()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewNil() },
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            processor.AssignGenericSymbol(SymbolRef.Local("value", 0), DynValue.NewNumber(7));
            Assert.That(frame.LocalScope[0].Number, Is.EqualTo(7));
        }

        [Test]
        public void AssignGenericSymbolUpdatesUpValueScope()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            ClosureContext closure = new();
            closure.Add(DynValue.NewNil());
            CallStackItem frame = new()
            {
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = closure,
            };
            processor.PushCallStackFrameForTests(frame);

            processor.AssignGenericSymbol(SymbolRef.UpValue("uv", 0), DynValue.NewNumber(11));
            Assert.That(frame.ClosureScope[0].Number, Is.EqualTo(11));
        }

        [Test]
        public void AssignGenericSymbolThrowsForUnexpectedSymbolType()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            SymbolRef invalid = SymbolRef.Local("value", 0);
            invalid.SymbolType = (SymbolRefType)999;

            Assert.That(
                () => processor.AssignGenericSymbol(invalid, DynValue.NewNumber(1)),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void GetGenericSymbolThrowsForUnexpectedSymbolType()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            SymbolRef invalid = SymbolRef.Local("value", 0);
            invalid.SymbolType = (SymbolRefType)999;

            Assert.That(
                () => processor.GetGenericSymbol(invalid),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void StackTopToArrayReturnsValuesWithoutAlteringStackWhenNotPopping()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));

            DynValue[] peeked = processor.StackTopToArrayForTests(2, pop: false);
            Assert.Multiple(() =>
            {
                Assert.That(peeked.Select(v => v.Number), Is.EqualTo(DescendingPair));
                Assert.That(stack.Count, Is.EqualTo(3));
            });

            DynValue[] popped = processor.StackTopToArrayForTests(2, pop: true);
            Assert.Multiple(() =>
            {
                Assert.That(popped.Select(v => v.Number), Is.EqualTo(DescendingPair));
                Assert.That(stack.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void StackTopToArrayReverseReturnsValuesInAscendingOrder()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));
            stack.Push(DynValue.NewNumber(4));

            DynValue[] peeked = processor.StackTopToArrayReverseForTests(3, pop: false);
            Assert.Multiple(() =>
            {
                Assert.That(peeked.Select(v => v.Number), Is.EqualTo(AscendingTriple));
                Assert.That(stack.Count, Is.EqualTo(4));
            });

            DynValue[] popped = processor.StackTopToArrayReverseForTests(3, pop: true);
            Assert.Multiple(() =>
            {
                Assert.That(popped.Select(v => v.Number), Is.EqualTo(AscendingTriple));
                Assert.That(stack.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void PushCallStackFrameForTestsRejectsNullFrames()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            Assert.That(
                () => processor.PushCallStackFrameForTests(null),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        [Test]
        public void ClearCallStackForTestsEmptiesExistingFrames()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            processor.PushCallStackFrameForTests(new CallStackItem());
            processor.PushCallStackFrameForTests(new CallStackItem());

            processor.ClearCallStackForTests();

            FastStack<CallStackItem> executionStack = processor.GetExecutionStackForTests();
            Assert.That(executionStack.Count, Is.EqualTo(0));
        }

        [Test]
        public void CloseSymbolsSubsetClosesValuesAndClearsTracking()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            bool closed = false;
            DynValue closable = CreateClosableValue(script, _ => closed = true);

            CallStackItem frame = new()
            {
                LocalScope = new[] { closable },
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef> { symbol } },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            processor.CloseSymbolsSubsetForTests(
                frame,
                new[] { symbol },
                DynValue.NewString("err")
            );

            Assert.Multiple(() =>
            {
                Assert.That(closed, Is.True);
                Assert.That(frame.LocalScope[0].IsNil(), Is.True);
                Assert.That(frame.ToBeClosedIndices?.Contains(0), Is.False);
                Assert.That(
                    frame.BlocksToClose == null || frame.BlocksToClose.All(list => list.Count == 0)
                );
            });
        }

        [Test]
        public void CloseSymbolsSubsetThrowsWhenMetamethodMissing()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewTable(new Table(script)) },
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef> { symbol } },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                processor.CloseSymbolsSubsetForTests(frame, new[] { symbol }, DynValue.Nil)
            );
            Assert.That(ex.Message, Does.Contain("__close"));
        }

        [Test]
        public void ClearBlockDataClosesSymbolsAndClearsRange()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            bool closed = false;
            DynValue closable = CreateClosableValue(script, _ => closed = true);

            CallStackItem frame = new()
            {
                LocalScope = new[] { closable, DynValue.NewNumber(7) },
                BlocksToClose = new List<List<SymbolRef>>
                {
                    new List<SymbolRef>
                    {
                        SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed),
                    },
                },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.Clean,
                NumVal = 0,
                NumVal2 = 1,
                SymbolList = new[]
                {
                    SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed),
                },
            };

            processor.ClearBlockDataForTests(instruction);

            Assert.Multiple(() =>
            {
                Assert.That(closed, Is.True);
                Assert.That(frame.LocalScope[0], Is.Null);
                Assert.That(frame.LocalScope[1], Is.Null);
                Assert.That(frame.ToBeClosedIndices, Is.Empty);
                Assert.That(frame.BlocksToClose.All(list => list.Count == 0));
            });
        }

        [Test]
        public void ClearBlockDataSkipsWhenRangeInvalid()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewNumber(1), DynValue.NewNumber(2) },
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.Clean,
                NumVal = 2,
                NumVal2 = 0,
            };

            processor.ClearBlockDataForTests(instruction);

            Assert.That(
                frame.LocalScope.Where(v => v != null).Select(v => v.Number),
                Is.EqualTo(AscendingPair)
            );
        }

        [Test]
        public void FindSymbolByNameFallsBackToGlobalWhenStackEmpty()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = processor.FindSymbolByName("missingVar");

            Assert.Multiple(() =>
            {
                Assert.That(symbol.SymbolType, Is.EqualTo(SymbolRefType.Global));
                Assert.That(symbol.EnvironmentRef.SymbolType, Is.EqualTo(SymbolRefType.DefaultEnv));
                Assert.That(symbol.Name, Is.EqualTo("missingVar"));
            });
        }

        [Test]
        public void GetGlobalSymbolThrowsWhenEnvIsNotTable()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                Processor.GetGlobalSymbolForTests(DynValue.NewNumber(1), "value")
            );
            Assert.That(ex.Message, Does.Contain("_ENV is not a table"));
        }

        [Test]
        public void InternalAdjustTupleReturnsEmptyArrayWhenValuesNull()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            DynValue[] result = Processor.InternalAdjustTupleForTests(null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void InternalAdjustTupleFlattensNestedTupleTail()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            DynValue nested = DynValue.NewTuple(DynValue.NewNumber(3));
            DynValue[] values =
            {
                DynValue.NewNumber(1),
                DynValue.NewTuple(DynValue.NewNumber(2), nested),
            };

            DynValue[] result = Processor.InternalAdjustTupleForTests(values);

            Assert.That(result.Select(v => v.Number), Is.EqualTo(AscendingPairWithHead));
        }

        [Test]
        public void ExecIterPrepUsesIteratorMetamethod()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();

            Table iteratorHost = new(script);
            bool called = false;
            Table meta = new(script);
            meta.Set(
                "__iterator",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        called = true;
                        return DynValue.NewTuple(
                            DynValue.NewString("fn"),
                            DynValue.NewString("state"),
                            DynValue.NewNumber(5)
                        );
                    }
                )
            );
            iteratorHost.MetaTable = meta;

            stack.Push(
                DynValue.NewTuple(DynValue.NewTable(iteratorHost), DynValue.Nil, DynValue.Nil)
            );

            processor.ExecIterPrepForTests(new Instruction(SourceRef.GetClrLocation()));

            DynValue tuple = stack.Peek();
            Assert.Multiple(() =>
            {
                Assert.That(called, Is.True);
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("fn"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("state"));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(5));
            });
        }

        [Test]
        public void ExecIterPrepConvertsPlainTableToEnumerableWrapper()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();

            Table table = new(script);
            table.Set(1, DynValue.NewString("value"));

            stack.Push(DynValue.NewTuple(DynValue.NewTable(table), DynValue.Nil, DynValue.Nil));

            processor.ExecIterPrepForTests(new Instruction(SourceRef.GetClrLocation()));

            DynValue tuple = stack.Peek();
            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Type, Is.EqualTo(DataType.UserData));
                Assert.That(
                    tuple.Tuple[0].UserData.Descriptor.Type,
                    Is.EqualTo(typeof(EnumerableWrapper))
                );
            });
        }

        [Test]
        public void ExecRetThrowsWhenReturningMoreThanOneValue()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                ReturnAddress = 0,
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation()) { NumVal = 2 };

            InternalErrorException ex = Assert.Throws<InternalErrorException>(() =>
                processor.ExecRetForTests(instruction)
            );
            Assert.That(ex.Message, Does.Contain("RET supports only 0 and 1 ret val scenarios"));
        }

        [Test]
        public void ExecBitNotThrowsWhenOperandIsNotInteger()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewString("not-integer"));

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                processor.ExecBitNotForTests(new Instruction(SourceRef.GetClrLocation()), 0)
            );
            Assert.That(ex.Message, Does.Contain("bitwise"));
        }

        [Test]
        public void ExecBitAndThrowsWhenOperandsAreNotIntegers()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewString("bad"));

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                processor.ExecBitAndForTests(new Instruction(SourceRef.GetClrLocation()), 0)
            );
            Assert.That(ex.Message, Does.Contain("bitwise"));
        }

        [Test]
        public void ExecTblInitIRejectsNonTableTargets()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1)); // value
            stack.Push(DynValue.NewNumber(2)); // not a table

            InternalErrorException ex = Assert.Throws<InternalErrorException>(() =>
                processor.ExecTblInitIForTests(new Instruction(SourceRef.GetClrLocation()))
            );
            Assert.That(ex.Message, Does.Contain("table ctor"));
        }

        [Test]
        public void ExecTblInitNRejectsNonTableTargets()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1)); // value
            stack.Push(DynValue.NewNumber(2)); // key
            stack.Push(DynValue.NewString("not-table"));

            InternalErrorException ex = Assert.Throws<InternalErrorException>(() =>
                processor.ExecTblInitNForTests(new Instruction(SourceRef.GetClrLocation()))
            );
            Assert.That(ex.Message, Does.Contain("table ctor"));
        }

        [Test]
        public void ExecIndexSetThrowsWhenUserDataDescriptorRejectsField()
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

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                processor.ExecIndexSetForTests(instruction, 0)
            );
            Assert.That(ex.Message, Does.Contain("missing"));
        }

        [Test]
        public void ExecIndexThrowsWhenMultiIndexingThroughMetamethod()
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

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                processor.ExecIndexForTests(instruction, 0)
            );
            Assert.That(ex.Message, Does.Contain("cannot multi-index through metamethods"));
        }

        private static DynValue CreateClosableValue(Script script, Action<DynValue> onClose = null)
        {
            Table token = new(script);
            Table metatable = new(script);
            metatable.Set(
                "__close",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        if (onClose != null)
                        {
                            onClose(args.Count > 1 ? args[1] : DynValue.Nil);
                        }
                        return DynValue.Nil;
                    }
                )
            );
            token.MetaTable = metatable;
            return DynValue.NewTable(token);
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
    }
}
