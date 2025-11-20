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
    using NovaSharp.Interpreter.Execution.VM;
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

        private static T GetPrivateField<T>(Processor processor, string fieldName)
        {
            FieldInfo field = typeof(Processor).GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (T)field.GetValue(processor)!;
        }
    }
}
