namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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
        [Test]
        public void CallThrowsWhenEnteredFromDifferentThread()
        {
            Script script = new();
            script.Options.CheckThreadAccess = true;
            DynValue chunk = script.LoadString("return 42");

            Processor processor = GetMainProcessor(script);
            FieldInfo owningThreadField = typeof(Processor).GetField(
                "_owningThreadId",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            FieldInfo executionNestingField = typeof(Processor).GetField(
                "_executionNesting",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;

            owningThreadField.SetValue(processor, Thread.CurrentThread.ManagedThreadId);
            executionNestingField.SetValue(processor, 1);

            Exception observed = null;
            Thread worker = new Thread(() =>
            {
                try
                {
                    script.Call(chunk);
                }
                catch (Exception ex)
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

            owningThreadField.SetValue(processor, -1);
            executionNestingField.SetValue(processor, 0);
            script.Options.CheckThreadAccess = false;
        }

        [Test]
        public void EnterAndLeaveProcessorUpdateParentCoroutineStack()
        {
            Script script = new();
            Processor parent = GetMainProcessor(script);
            Processor child = CreateChildProcessor(parent);

            FieldInfo stackField = typeof(Processor).GetField(
                "_coroutinesStack",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            List<Processor> stack = (List<Processor>)stackField.GetValue(parent)!;

            MethodInfo enter = typeof(Processor).GetMethod(
                "EnterProcessor",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            MethodInfo leave = typeof(Processor).GetMethod(
                "LeaveProcessor",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;

            enter.Invoke(child, null);
            Assert.That(stack[^1], Is.SameAs(child));

            leave.Invoke(child, null);
            Assert.That(stack, Is.Empty);
        }

        [Test]
        public void PerformMessageDecorationBeforeUnwindUsesClrFunction()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

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
            Processor processor = GetMainProcessor(script);
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
            Processor processor = GetMainProcessor(script);

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

        private static Processor GetMainProcessor(Script script)
        {
            FieldInfo field = typeof(Script).GetField(
                "_mainProcessor",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (Processor)field.GetValue(script)!;
        }

        [Test]
        public void ParentConstructorInitializesState()
        {
            Script script = new();
            Processor parent = GetMainProcessor(script);
            Processor child = CreateChildProcessor(parent);

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivateField<Processor>(child, "_parent"), Is.SameAs(parent));
                Assert.That(
                    GetPrivateField<CoroutineState>(child, "_state"),
                    Is.EqualTo(CoroutineState.NotStarted)
                );
            });
        }

        [Test]
        public void RecycleConstructorReusesStacks()
        {
            Script script = new();
            Processor parent = GetMainProcessor(script);
            Processor recycleSource = CreateChildProcessor(parent);

            ConstructorInfo recycleCtor = typeof(Processor).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(Processor), typeof(Processor) },
                null
            )!;

            Processor recycled = (Processor)
                recycleCtor.Invoke(new object[] { parent, recycleSource });

            Assert.Multiple(() =>
            {
                Assert.That(
                    GetPrivateField<object>(recycled, "_valueStack"),
                    Is.SameAs(GetPrivateField<object>(recycleSource, "_valueStack"))
                );
                Assert.That(
                    GetPrivateField<object>(recycled, "_executionStack"),
                    Is.SameAs(GetPrivateField<object>(recycleSource, "_executionStack"))
                );
            });
        }

        [Test]
        public void CallDelegatesToParentCoroutineStackTop()
        {
            Script script = new();
            DynValue function = script.LoadString("return 321");

            Processor parent = GetMainProcessor(script);
            Processor child = CreateChildProcessor(parent);
            Processor delegated = CreateChildProcessor(parent);

            FieldInfo stackField = typeof(Processor).GetField(
                "_coroutinesStack",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            stackField.SetValue(parent, new List<Processor> { delegated });

            MethodInfo callMethod = typeof(Processor).GetMethod(
                "Call",
                BindingFlags.Public | BindingFlags.Instance
            )!;

            DynValue result = (DynValue)
                callMethod.Invoke(child, new object[] { function, Array.Empty<DynValue>() });

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
            Processor coroutineProcessor = GetProcessorFromCoroutine(coroutineValue.Coroutine);
            bool originalCanYield = SetCanYield(coroutineProcessor, false);

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
                SetCanYield(coroutineProcessor, originalCanYield);
            }
        }

        [Test]
        public void ExecIncrClonesReadOnlyValueBeforeIncrement()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> valueStack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            valueStack.Clear();
            valueStack.Push(DynValue.NewNumber(1)); // step value at offset 1
            valueStack.Push(DynValue.NewNumber(2).AsReadOnly());

            Instruction instruction = new(SourceRef.GetClrLocation()) { NumVal = 1 };
            MethodInfo execIncr = typeof(Processor).GetMethod(
                "ExecIncr",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;

            execIncr.Invoke(processor, new object[] { instruction });

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
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> valueStack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            valueStack.Clear();

            // Order matters: value is pushed last so it is popped first inside ExecCNot.
            valueStack.Push(DynValue.NewString("not-bool"));
            valueStack.Push(DynValue.NewNumber(1));

            MethodInfo execCNot = GetProcessorMethod("ExecCNot");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execCNot.Invoke(
                    processor,
                    new object[] { new Instruction(SourceRef.GetClrLocation()) }
                )
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException, Is.TypeOf<InternalErrorException>());
                Assert.That(ex.InnerException?.Message, Does.Contain("CNOT had non-bool arg"));
            });
        }

        [Test]
        public void ExecBeginFnClearsExistingScopeTracking()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
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

            MethodInfo execBegin = GetProcessorMethod("ExecBeginFn");
            execBegin.Invoke(processor, new object[] { instruction });

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
            Processor processor = GetMainProcessor(script);
            processor.ClearCallStackForTests();
            processor.PushCallStackFrameForTests(
                new CallStackItem() { BasePointer = 3, ReturnAddress = 42 }
            );

            MethodInfo popGuard = typeof(Processor).GetMethod(
                "PopExecStackAndCheckVStack",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;

            int returnAddress = (int)popGuard.Invoke(processor, new object[] { 3 });
            Assert.That(returnAddress, Is.EqualTo(42));
        }

        [Test]
        public void PopExecStackAndCheckVStackThrowsWhenGuardDiffers()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            processor.ClearCallStackForTests();
            processor.PushCallStackFrameForTests(
                new CallStackItem() { BasePointer = 2, ReturnAddress = 7 }
            );

            MethodInfo popGuard = typeof(Processor).GetMethod(
                "PopExecStackAndCheckVStack",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;

            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                popGuard.Invoke(processor, new object[] { 0 })
            )!;
            Assert.That(ex.InnerException, Is.TypeOf<InternalErrorException>());
        }

        [Test]
        public void SetGlobalSymbolThrowsWhenEnvIsNotTable()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

            MethodInfo setGlobal = typeof(Processor).GetMethod(
                "SetGlobalSymbol",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;

            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                setGlobal.Invoke(
                    processor,
                    new object[] { DynValue.NewString("not-table"), "value", DynValue.NewNumber(1) }
                )
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(ex.InnerException?.Message, Does.Contain("_ENV is not a table"));
            });
        }

        [Test]
        public void AssignGenericSymbolRejectsDefaultEnvSymbol()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

            Assert.That(
                () => processor.AssignGenericSymbol(SymbolRef.DefaultEnv, DynValue.NewNumber(1)),
                Throws
                    .TypeOf<ArgumentException>()
                    .With.Message.Contains("Can't AssignGenericSymbol on a DefaultEnv symbol")
            );
        }

        [Test]
        public void AssignGenericSymbolUpdatesLocalScope()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
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
        public void AssignGenericSymbolUpdatesUpvalueScope()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            processor.ClearCallStackForTests();

            ClosureContext closure = new();
            closure.Add(DynValue.NewNil());
            CallStackItem frame = new()
            {
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = closure,
            };
            processor.PushCallStackFrameForTests(frame);

            processor.AssignGenericSymbol(SymbolRef.Upvalue("uv", 0), DynValue.NewNumber(11));
            Assert.That(frame.ClosureScope[0].Number, Is.EqualTo(11));
        }

        [Test]
        public void AssignGenericSymbolThrowsForUnexpectedSymbolType()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

            SymbolRef invalid = SymbolRef.Local("value", 0);
            SetSymbolType(invalid, (SymbolRefType)999);

            Assert.That(
                () => processor.AssignGenericSymbol(invalid, DynValue.NewNumber(1)),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void GetGenericSymbolThrowsForUnexpectedSymbolType()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

            SymbolRef invalid = SymbolRef.Local("value", 0);
            SetSymbolType(invalid, (SymbolRefType)999);

            Assert.That(
                () => processor.GetGenericSymbol(invalid),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void StackTopToArrayReturnsValuesWithoutAlteringStackWhenNotPopping()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));

            DynValue[] peeked = InvokeStackArrayHelper(processor, "StackTopToArray", 2, pop: false);
            Assert.Multiple(() =>
            {
                Assert.That(peeked.Select(v => v.Number), Is.EqualTo(new[] { 3d, 2d }));
                Assert.That(stack.Count, Is.EqualTo(3));
            });

            DynValue[] popped = InvokeStackArrayHelper(processor, "StackTopToArray", 2, pop: true);
            Assert.Multiple(() =>
            {
                Assert.That(popped.Select(v => v.Number), Is.EqualTo(new[] { 3d, 2d }));
                Assert.That(stack.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void StackTopToArrayReverseReturnsValuesInAscendingOrder()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));
            stack.Push(DynValue.NewNumber(4));

            DynValue[] peeked = InvokeStackArrayHelper(
                processor,
                "StackTopToArrayReverse",
                3,
                pop: false
            );
            Assert.Multiple(() =>
            {
                Assert.That(peeked.Select(v => v.Number), Is.EqualTo(new[] { 2d, 3d, 4d }));
                Assert.That(stack.Count, Is.EqualTo(4));
            });

            DynValue[] popped = InvokeStackArrayHelper(
                processor,
                "StackTopToArrayReverse",
                3,
                pop: true
            );
            Assert.Multiple(() =>
            {
                Assert.That(popped.Select(v => v.Number), Is.EqualTo(new[] { 2d, 3d, 4d }));
                Assert.That(stack.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void PushCallStackFrameForTestsRejectsNullFrames()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

            Assert.That(
                () => processor.PushCallStackFrameForTests(null),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        [Test]
        public void ClearCallStackForTestsEmptiesExistingFrames()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);

            processor.PushCallStackFrameForTests(new CallStackItem());
            processor.PushCallStackFrameForTests(new CallStackItem());

            processor.ClearCallStackForTests();

            FastStack<CallStackItem> executionStack = GetPrivateField<FastStack<CallStackItem>>(
                processor,
                "_executionStack"
            );
            Assert.That(executionStack.Count, Is.EqualTo(0));
        }

        [Test]
        public void CloseSymbolsSubsetClosesValuesAndClearsTracking()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
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

            MethodInfo closeSubset = GetProcessorMethod("CloseSymbolsSubset");
            closeSubset.Invoke(
                processor,
                new object[] { frame, new[] { symbol }, DynValue.NewString("err") }
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
            Processor processor = GetMainProcessor(script);
            processor.ClearCallStackForTests();

            SymbolRef symbol = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewTable(new Table(script)) },
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef> { symbol } },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            MethodInfo closeSubset = GetProcessorMethod("CloseSymbolsSubset");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                closeSubset.Invoke(
                    processor,
                    new object[] { frame, new[] { symbol }, DynValue.Nil }
                )
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException, Is.TypeOf<ScriptRuntimeException>());
                Assert.That(ex.InnerException?.Message, Does.Contain("__close"));
            });
        }

        [Test]
        public void ClearBlockDataClosesSymbolsAndClearsRange()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
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

            MethodInfo clearBlockData = GetProcessorMethod("ClearBlockData");
            clearBlockData.Invoke(processor, new object[] { instruction });

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
            Processor processor = GetMainProcessor(script);
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

            MethodInfo clearBlockData = GetProcessorMethod("ClearBlockData");
            clearBlockData.Invoke(processor, new object[] { instruction });

            Assert.That(
                frame.LocalScope.Where(v => v != null).Select(v => v.Number),
                Is.EqualTo(new[] { 1d, 2d })
            );
        }

        [Test]
        public void FindSymbolByNameFallsBackToGlobalWhenStackEmpty()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
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
            Processor processor = GetMainProcessor(script);

            MethodInfo getGlobal = GetProcessorMethod("GetGlobalSymbol");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                getGlobal.Invoke(processor, new object[] { DynValue.NewNumber(1), "value" })
            )!;

            Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.InnerException?.Message, Does.Contain("_ENV is not a table"));
        }

        [Test]
        public void InternalAdjustTupleReturnsEmptyArrayWhenValuesNull()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            MethodInfo adjustTuple = GetProcessorMethod("InternalAdjustTuple");

            DynValue[] result = (DynValue[])adjustTuple.Invoke(processor, new object[] { null })!;
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void InternalAdjustTupleFlattensNestedTupleTail()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            MethodInfo adjustTuple = GetProcessorMethod("InternalAdjustTuple");

            DynValue nested = DynValue.NewTuple(DynValue.NewNumber(3));
            DynValue[] values =
            {
                DynValue.NewNumber(1),
                DynValue.NewTuple(DynValue.NewNumber(2), nested),
            };

            DynValue[] result = (DynValue[])adjustTuple.Invoke(processor, new object[] { values })!;

            Assert.That(result.Select(v => v.Number), Is.EqualTo(new[] { 1d, 2d, 3d }));
        }

        [Test]
        public void ExecIterPrepUsesIteratorMetamethod()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
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

            MethodInfo execIterPrep = GetProcessorMethod("ExecIterPrep");
            execIterPrep.Invoke(
                processor,
                new object[] { new Instruction(SourceRef.GetClrLocation()) }
            );

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
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();

            Table table = new(script);
            table.Set(1, DynValue.NewString("value"));

            stack.Push(DynValue.NewTuple(DynValue.NewTable(table), DynValue.Nil, DynValue.Nil));

            MethodInfo execIterPrep = GetProcessorMethod("ExecIterPrep");
            execIterPrep.Invoke(
                processor,
                new object[] { new Instruction(SourceRef.GetClrLocation()) }
            );

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
            Processor processor = GetMainProcessor(script);
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                ReturnAddress = 0,
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            MethodInfo execRet = GetProcessorMethod("ExecRet");
            Instruction instruction = new Instruction(SourceRef.GetClrLocation()) { NumVal = 2 };

            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execRet.Invoke(processor, new object[] { instruction })
            )!;
            Assert.That(ex.InnerException, Is.TypeOf<InternalErrorException>());
            Assert.That(
                ex.InnerException?.Message,
                Does.Contain("RET supports only 0 and 1 ret val scenarios")
            );
        }

        [Test]
        public void ExecBitNotThrowsWhenOperandIsNotInteger()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();
            stack.Push(DynValue.NewString("not-integer"));

            MethodInfo execBitNot = GetProcessorMethod("ExecBitNot");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execBitNot.Invoke(
                    processor,
                    new object[] { new Instruction(SourceRef.GetClrLocation()), 0 }
                )
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException, Is.TypeOf<ScriptRuntimeException>());
                Assert.That(ex.InnerException?.Message, Does.Contain("bitwise"));
            });
        }

        [Test]
        public void ExecBitAndThrowsWhenOperandsAreNotIntegers()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewString("bad"));

            MethodInfo execBitAnd = GetProcessorMethod("ExecBitAnd");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execBitAnd.Invoke(
                    processor,
                    new object[] { new Instruction(SourceRef.GetClrLocation()), 0 }
                )
            )!;

            Assert.That(ex.InnerException, Is.TypeOf<ScriptRuntimeException>());
            Assert.That(ex.InnerException?.Message, Does.Contain("bitwise"));
        }

        [Test]
        public void ExecTblInitIRejectsNonTableTargets()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();
            stack.Push(DynValue.NewNumber(1)); // value
            stack.Push(DynValue.NewNumber(2)); // not a table

            MethodInfo execTblInitI = GetProcessorMethod("ExecTblInitI");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execTblInitI.Invoke(
                    processor,
                    new object[] { new Instruction(SourceRef.GetClrLocation()) }
                )
            )!;

            Assert.That(ex.InnerException, Is.TypeOf<InternalErrorException>());
            Assert.That(ex.InnerException?.Message, Does.Contain("table ctor"));
        }

        [Test]
        public void ExecTblInitNRejectsNonTableTargets()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
            stack.Clear();
            stack.Push(DynValue.NewNumber(1)); // value
            stack.Push(DynValue.NewNumber(2)); // key
            stack.Push(DynValue.NewString("not-table"));

            MethodInfo execTblInitN = GetProcessorMethod("ExecTblInitN");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execTblInitN.Invoke(
                    processor,
                    new object[] { new Instruction(SourceRef.GetClrLocation()) }
                )
            )!;

            Assert.That(ex.InnerException, Is.TypeOf<InternalErrorException>());
            Assert.That(ex.InnerException?.Message, Does.Contain("table ctor"));
        }

        [Test]
        public void ExecIndexSetThrowsWhenUserDataDescriptorRejectsField()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
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

            MethodInfo execIndexSet = GetProcessorMethod("ExecIndexSet");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execIndexSet.Invoke(processor, new object[] { instruction, 0 })
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(ex.InnerException, Is.TypeOf<ScriptRuntimeException>());
                Assert.That(ex.InnerException?.Message, Does.Contain("missing"));
            });
        }

        [Test]
        public void ExecIndexThrowsWhenMultiIndexingThroughMetamethod()
        {
            Script script = new();
            Processor processor = GetMainProcessor(script);
            FastStack<DynValue> stack = GetPrivateField<FastStack<DynValue>>(
                processor,
                "_valueStack"
            );
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

            MethodInfo execIndex = GetProcessorMethod("ExecIndex");
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
                execIndex.Invoke(processor, new object[] { instruction, 0 })
            )!;

            Assert.That(ex.InnerException, Is.TypeOf<ScriptRuntimeException>());
            Assert.That(
                ex.InnerException?.Message,
                Does.Contain("cannot multi-index through metamethods")
            );
        }

        private static Processor CreateChildProcessor(Processor parent)
        {
            ConstructorInfo childCtor = typeof(Processor).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(Processor) },
                null
            )!;
            return (Processor)childCtor.Invoke(new object[] { parent });
        }

        private static Processor GetProcessorFromCoroutine(Coroutine coroutine)
        {
            FieldInfo field = typeof(Coroutine).GetField(
                "_processor",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (Processor)field.GetValue(coroutine)!;
        }

        private static bool SetCanYield(Processor processor, bool value)
        {
            FieldInfo field = typeof(Processor).GetField(
                "_canYield",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            bool original = (bool)field.GetValue(processor)!;
            field.SetValue(processor, value);
            return original;
        }

        private static DynValue[] InvokeStackArrayHelper(
            Processor processor,
            string methodName,
            int items,
            bool pop
        )
        {
            MethodInfo method = typeof(Processor).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (DynValue[])method.Invoke(processor, new object[] { items, pop });
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

        private static MethodInfo GetProcessorMethod(string name)
        {
            return typeof(Processor).GetMethod(
                name,
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
        }

        private static T GetPrivateField<T>(Processor processor, string fieldName)
        {
            FieldInfo field = typeof(Processor).GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (T)field.GetValue(processor)!;
        }

        private static void SetSymbolType(SymbolRef symbol, SymbolRefType type)
        {
            FieldInfo field = typeof(SymbolRef).GetField(
                "SymbolType",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            field.SetValue(symbol, type);
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
