namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
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

        [Test]
        public void ToggleBreakpointTogglesExistingLocation()
        {
            Script script = new();
            script.LoadString("return 13");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);
            processor.ClearBreakpointsForTests();

            SourceRef target = GetFirstBreakableSourceRef(script);
            StubDebugger debugger = new();
            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.ToggleBreakpoint,
                    SourceId = target.SourceIdx,
                    SourceLine = target.FromLine,
                    SourceCol = target.FromChar,
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, 0);
            Assert.That(target.Breakpoint, Is.True);

            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.ToggleBreakpoint,
                    SourceId = target.SourceIdx,
                    SourceLine = target.FromLine,
                    SourceCol = target.FromChar,
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );
            processor.ListenDebuggerForTests(instruction, 0);

            Assert.That(target.Breakpoint, Is.False);
        }

        [Test]
        public void SetAndClearBreakpointUpdateBreakpointCollection()
        {
            Script script = new();
            script.LoadString("return 14");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);
            processor.ClearBreakpointsForTests();

            SourceRef target = GetFirstBreakableSourceRef(script);
            StubDebugger debugger = new();
            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.SetBreakpoint,
                    SourceId = target.SourceIdx,
                    SourceLine = target.FromLine,
                    SourceCol = target.FromChar,
                    Lines = new[] { target.FromLine },
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, 0);
            Assert.That(processor.GetBreakpointsForTests(), Has.Member(target));

            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.ClearBreakpoint,
                    SourceId = target.SourceIdx,
                    SourceLine = target.FromLine,
                    SourceCol = target.FromChar,
                    Lines = new[] { target.FromLine },
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );
            processor.ListenDebuggerForTests(instruction, 0);

            Assert.That(processor.GetBreakpointsForTests(), Is.Empty);
        }

        [Test]
        public void ResetBreakpointsAppliesRequestedLines()
        {
            Script script = new();
            script.LoadString("local x = 1\nreturn x");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            SourceCode source = script.GetSourceCode(0);
            int[] candidateLines = source
                .Refs.Where(r => !r.CannotBreakpoint)
                .Select(r => r.FromLine)
                .Distinct()
                .Take(2)
                .ToArray();

            StubDebugger debugger = new();
            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.ResetBreakpoints,
                    SourceId = source.SourceId,
                    Lines = candidateLines,
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, 0);

            IEnumerable<int> activeLines = source
                .Refs.Where(r => r.Breakpoint)
                .Select(r => r.FromLine)
                .Distinct();
            Assert.That(activeLines, Is.EquivalentTo(candidateLines));
        }

        [Test]
        public void HardRefreshInvokesBreakpointRefresh()
        {
            Script script = new();
            script.LoadString("return 15");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.HardRefresh);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, 0);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.RefreshBreakpointsCallCount, Is.EqualTo(1));
                Assert.That(debugger.UpdateCallCount, Is.GreaterThan(0));
            });
        }

        [Test]
        public void RefreshActionDoesNotInvokeBreakpointRefresh()
        {
            Script script = new();
            script.LoadString("return 16");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Refresh);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, 0);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.RefreshBreakpointsCallCount, Is.EqualTo(0));
                Assert.That(debugger.UpdateCallCount, Is.GreaterThan(0));
            });
        }

        [Test]
        public void StepInReturnsEarlyWhenLocationMatchesHighlight()
        {
            Script script = new();
            script.LoadString("return 11");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            StubDebugger debugger = new();
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            SourceRef location = new SourceRef(0, 0, 0, 1, 1, true);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepIn,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: location
            );

            Instruction instruction = new(location) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.UpdateCallCount, Is.EqualTo(0));
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.StepIn)
                );
            });
        }

        [Test]
        public void ByteCodeStepOutReturnsEarlyWhenDepthNotReduced()
        {
            Script script = new();
            script.LoadString("return 12");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            StubDebugger debugger = new();
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.ByteCodeStepOut,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 6);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.UpdateCallCount, Is.EqualTo(0));
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.ByteCodeStepOut)
                );
            });
        }

        [Test]
        public void DebuggerActionQueueSetsStepInState()
        {
            Script script = new();
            script.LoadString("return 13");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.StepIn);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 7);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.StepIn)
                );
                Assert.That(processor.GetDebuggerActionTargetForTests(), Is.EqualTo(-1));
                Assert.That(debugger.UpdateCallCount, Is.GreaterThan(0));
            });
        }

        [Test]
        public void ToggleBreakpointFallsBackToNearestLine()
        {
            Script script = new();
            script.LoadString("return 14");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);
            processor.ClearBreakpointsForTests();

            StubDebugger debugger = new();
            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.ToggleBreakpoint,
                    SourceId = 0,
                    SourceLine = 100,
                    SourceCol = 1,
                    Lines = new[] { 100 },
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 8);

            IReadOnlyList<SourceRef> breakpoints = processor.GetBreakpointsForTests();

            Assert.Multiple(() =>
            {
                Assert.That(breakpoints, Is.Not.Empty);
                Assert.That(breakpoints[0].Breakpoint, Is.True);
            });
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

        private static SourceRef GetFirstBreakableSourceRef(Script script)
        {
            SourceCode source = script.GetSourceCode(0);
            return source.Refs.First(r => !r.CannotBreakpoint);
        }

        private sealed class StubDebugger : IDebugger
        {
            private readonly Queue<DebuggerAction> actions = new();

            public bool PauseRequested { get; set; }

            public DebuggerCaps Caps { get; set; } =
                DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;

            public int UpdateCallCount { get; private set; }
            public int RefreshBreakpointsCallCount { get; private set; }

            public void EnqueueAction(DebuggerAction.ActionType actionType)
            {
                actions.Enqueue(new DebuggerAction() { Action = actionType });
            }

            public void EnqueueAction(DebuggerAction action)
            {
                actions.Enqueue(action);
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

            public void Update(WatchType watchType, IEnumerable<WatchItem> items)
            {
                UpdateCallCount++;
            }

            public List<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
            {
                RefreshBreakpointsCallCount++;
            }
        }
    }
}
