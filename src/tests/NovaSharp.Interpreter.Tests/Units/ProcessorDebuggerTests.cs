namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProcessorDebuggerTests
    {
        [Test]
        public void ListenDebuggerUpdatesHighlightForLineBasedRun()
        {
            Script script = new();
            script.LoadString("return 0");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: true);

            SourceRef initial = new(0, 0, 0, 1, 1, true);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Run,
                -1,
                executionStackDepth: 0,
                lastHighlight: initial
            );

            Instruction instruction = new(new SourceRef(0, 0, 0, 2, 2, true))
            {
                OpCode = OpCode.Debug,
            };

            processor.ListenDebuggerForTests(instruction, 0);

            Assert.That(processor.GetLastHighlightForTests(), Is.SameAs(instruction.SourceCodeRef));
        }

        [Test]
        public void PauseRequestResetsDebuggerActionBeforeRefreshing()
        {
            Script script = new();
            script.LoadString("return 0");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepIn,
                actionTarget: 123,
                executionStackDepth: 0,
                lastHighlight: new SourceRef(0, 0, 0, 1, 1, true)
            );

            Instruction instruction = new(new SourceRef(0, 0, 0, 1, 1, true))
            {
                OpCode = OpCode.Debug,
            };
            instruction.SourceCodeRef.Breakpoint = true;

            processor.ListenDebuggerForTests(instruction, 0);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
                Assert.That(processor.GetDebuggerActionTargetForTests(), Is.EqualTo(-1));
            });
        }

        [Test]
        public void ByteCodeStepOverSkipsUntilTargetInstruction()
        {
            Script script = new();
            script.LoadString("return 7");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.ByteCodeStepOver,
                actionTarget: 10,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            Assert.That(
                processor.GetDebuggerActionForTests(),
                Is.EqualTo(DebuggerAction.ActionType.ByteCodeStepOver)
            );
            Assert.That(processor.GetDebuggerActionTargetForTests(), Is.EqualTo(10));
        }

        [Test]
        public void ByteCodeStepOverTriggersActionWhenTargetReached()
        {
            Script script = new();
            script.LoadString("return 8");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.ByteCodeStepOver,
                actionTarget: 4,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 4);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
                Assert.That(processor.GetDebuggerActionTargetForTests(), Is.EqualTo(-1));
            });
        }

        [Test]
        public void StepOverReturnsWhenSameLocationAndDeeperStack()
        {
            Script script = new();
            script.LoadString("return 9");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 2);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            SourceRef location = new SourceRef(0, 0, 0, 1, 1, true);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepOver,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: location
            );

            Instruction instruction = new(location) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 1);

            Assert.That(
                processor.GetDebuggerActionForTests(),
                Is.EqualTo(DebuggerAction.ActionType.StepOver)
            );
        }

        [Test]
        public void StepOutReturnsWhenStackIsDeeper()
        {
            Script script = new();
            script.LoadString("return 10");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 3);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepOut,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 2);

            Assert.That(
                processor.GetDebuggerActionForTests(),
                Is.EqualTo(DebuggerAction.ActionType.StepOut)
            );
        }

        private static void PrepareCallStack(Processor processor, int frameCount = 1)
        {
            processor.ClearCallStackForTests();

            for (int i = 0; i < frameCount; i++)
            {
                processor.PushCallStackFrameForTests(
                    new CallStackItem()
                    {
                        DebugEntryPoint = 0,
                        CallingSourceRef = SourceRef.GetClrLocation(),
                    }
                );
            }
        }

        private sealed class StubDebugger : IDebugger
        {
            private readonly Queue<DebuggerAction> actions = new();

            public bool PauseRequested { get; set; }

            public DebuggerCaps Caps { get; set; } =
                DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;

            public void EnqueueAction(DebuggerAction.ActionType actionType)
            {
                actions.Enqueue(new DebuggerAction() { Action = actionType });
            }

            public DebuggerCaps GetDebuggerCaps()
            {
                return Caps;
            }

            public void SetDebugService(DebugService debugService) { }

            public void SetSourceCode(SourceCode sourceCode) { }

            public void SetByteCode(string[] byteCode) { }

            public bool IsPauseRequested()
            {
                return PauseRequested;
            }

            public bool SignalRuntimeException(ScriptRuntimeException ex)
            {
                return false;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                if (actions.Count == 0)
                {
                    return new DebuggerAction() { Action = DebuggerAction.ActionType.Run };
                }

                return actions.Dequeue();
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }

            public List<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }
    }
}
