namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
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
