namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
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
        public void StepOutAdvancesWhenStackDepthDrops()
        {
            Script script = new();
            script.LoadString("return 8");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepOut,
                actionTarget: -1,
                executionStackDepth: 2,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 3);

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
        public void DebuggerActionQueueSetsByteCodeStepOverState()
        {
            Script script = new();
            script.LoadString("return 8");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.ByteCodeStepOver);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.ByteCodeStepOver)
                );
                Assert.That(processor.GetDebuggerActionTargetForTests(), Is.EqualTo(6));
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
        public void StepOverContinuesWhenLocationChanges()
        {
            Script script = new();
            script.LoadString("return 9");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            SourceRef previous = new SourceRef(0, 0, 0, 1, 1, true);
            SourceRef updated = new SourceRef(0, 0, 0, 2, 2, true);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepOver,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: previous
            );

            Instruction instruction = new(updated) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 1);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
                Assert.That(processor.GetLastHighlightForTests(), Is.SameAs(updated));
            });
        }

        [Test]
        public void ByteCodeStepOutAdvancesWhenStackDepthDrops()
        {
            Script script = new();
            script.LoadString("return 10");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.ByteCodeStepOut,
                actionTarget: -1,
                executionStackDepth: 2,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 3);

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
        public void StepInContinuesWhenLocationIsNew()
        {
            Script script = new();
            script.LoadString("return 12");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            SourceRef previous = new SourceRef(0, 0, 0, 1, 1, true);
            SourceRef updated = new SourceRef(0, 0, 0, 3, 3, true);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepIn,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: previous
            );

            Instruction instruction = new(updated) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 6);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
                Assert.That(processor.GetLastHighlightForTests(), Is.SameAs(updated));
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
        public void DebuggerActionQueueSetsByteCodeStepInState()
        {
            Script script = new();
            script.LoadString("return 27");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.ByteCodeStepIn);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 11);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.ByteCodeStepIn)
                );
                Assert.That(processor.GetDebuggerActionTargetForTests(), Is.EqualTo(-1));
            });
        }

        [Test]
        public void RefreshDebuggerThreadsUsesParentCoroutineStack()
        {
            Script script = new();
            script.DoString("function idle() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("idle"));
            Coroutine coroutine = coroutineValue.Coroutine;

            Processor coroutineProcessor = coroutine.GetProcessorForTests();
            Processor parentProcessor = script.GetMainProcessorForTests();
            List<Processor> parentStack = parentProcessor.GetCoroutineStackForTests();
            List<Processor> originalStack = parentStack.ToList();

            try
            {
                parentStack.Clear();
                parentStack.Add(coroutineProcessor);

                List<WatchItem> threads = coroutineProcessor.RefreshDebuggerThreadsForTests();

                Assert.Multiple(() =>
                {
                    Assert.That(threads, Has.Count.EqualTo(1));
                    Assert.That(threads[0].Address, Is.EqualTo(coroutine.ReferenceId));
                    Assert.That(threads[0].Name, Is.EqualTo($"coroutine #{coroutine.ReferenceId}"));
                });
            }
            finally
            {
                parentStack.Clear();
                parentStack.AddRange(originalStack);
            }
        }

        [Test]
        public void ToggleBreakpointFallsBackToNearestLine()
        {
            Script script = new();
            script.LoadString(
                @"
                local x = 10
                local y = 4
                return x + y
            "
            );

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

        [Test]
        public void SetBreakpointForcesStateWhenLocationMatches()
        {
            Script script = new();
            script.LoadString("return 17");

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

            processor.ListenDebuggerForTests(instruction, instructionPtr: 2);

            IReadOnlyList<SourceRef> breakpoints = processor.GetBreakpointsForTests();
            Assert.That(breakpoints, Has.Member(target));
            Assert.That(breakpoints.Single(s => s == target).Breakpoint, Is.True);
        }

        [Test]
        public void ClearBreakpointForcesStateWhenLocationMatches()
        {
            Script script = new();
            script.LoadString("return 18");

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
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 4);

            IReadOnlyList<SourceRef> breakpoints = processor.GetBreakpointsForTests();
            Assert.That(breakpoints.Any(r => r == target && r.Breakpoint), Is.False);
        }

        [Test]
        public void SetBreakpointSnapsToNearestLocationWhenNoExactMatchExists()
        {
            Script script = new();
            script.LoadString(
                @"
                local value = 0
                return value
            "
            );

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
                    SourceLine = target.FromLine + 50,
                    SourceCol = target.FromChar + 25,
                    Lines = new[] { target.FromLine + 50 },
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
            processor.ListenDebuggerForTests(instruction, instructionPtr: 6);

            IReadOnlyList<SourceRef> breakpoints = processor.GetBreakpointsForTests();
            Assert.Multiple(() =>
            {
                Assert.That(breakpoints, Is.Not.Empty);
                Assert.That(breakpoints.Single().Breakpoint, Is.True);
                Assert.That(breakpoints.Single().SourceIdx, Is.EqualTo(target.SourceIdx));
            });
        }

        [Test]
        public void ClearBreakpointSnapsToNearestLocationWhenNoExactMatchExists()
        {
            Script script = new();
            script.LoadString(
                @"
                local data = 0
                return data
            "
            );

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
                    SourceLine = target.FromLine + 40,
                    SourceCol = target.FromChar + 20,
                    Lines = new[] { target.FromLine + 40 },
                }
            );
            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.ClearBreakpoint,
                    SourceId = target.SourceIdx,
                    SourceLine = target.FromLine + 40,
                    SourceCol = target.FromChar + 20,
                    Lines = new[] { target.FromLine + 40 },
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
            processor.ListenDebuggerForTests(instruction, instructionPtr: 9);

            Assert.That(processor.GetBreakpointsForTests(), Is.Empty);
        }

        [Test]
        public void SetBreakpointReturnsFalseWhenSourceHasNoRefs()
        {
            Script script = new();
            script.LoadString("return 19");

            SourceCode source = script.GetSourceCode(0);
            source.Refs.Clear();

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);
            processor.ClearBreakpointsForTests();

            StubDebugger debugger = new();
            debugger.EnqueueAction(
                new DebuggerAction()
                {
                    Action = DebuggerAction.ActionType.SetBreakpoint,
                    SourceId = source.SourceId,
                    SourceLine = 1,
                    SourceCol = 1,
                    Lines = new[] { 1 },
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
            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            Assert.That(processor.GetBreakpointsForTests(), Is.Empty);
        }

        [Test]
        public void RefreshActionProcessesNextQueuedDebuggerCommand()
        {
            Script script = new();
            script.LoadString("return 20");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Refresh);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 8);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
                Assert.That(debugger.UpdateCallCount, Is.GreaterThanOrEqualTo(2));
            });
        }

        [Test]
        public void PauseRequestDuringRefreshStillProcessesQueuedAction()
        {
            Script script = new();
            script.LoadString("return 21");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseAfterNextUpdate = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Refresh);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 1);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.PauseRequested, Is.True);
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
            });
        }

        [Test]
        public void RefreshDebuggerEvaluatesWatchExpressions()
        {
            Script script = new();
            script.LoadString("return 15");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            DynamicExpression watch = new(script, "const_watch", DynValue.NewNumber(42));

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.WatchItems.Add(watch);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 3);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches), Is.True);
                List<WatchItem> watches = debugger.LastWatchUpdates[WatchType.Watches];
                Assert.That(watches, Has.Count.EqualTo(1));
                Assert.That(watches[0].Name, Is.EqualTo("const_watch"));
                Assert.That(watches[0].Value.Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void RefreshDebuggerCapturesWatchEvaluationErrors()
        {
            Script script = new();
            script.LoadString("return 16");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            DynamicExpression failingWatch = script.CreateDynamicExpression("missing_symbol()");

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.WatchItems.Add(failingWatch);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 4);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches), Is.True);
                WatchItem watch = debugger.LastWatchUpdates[WatchType.Watches].Single();
                Assert.That(watch.IsError, Is.True);
                Assert.That(watch.Value.Type, Is.EqualTo(DataType.String));
                Assert.That(watch.Value.String, Does.Contain("cannot call functions"));
            });
        }

        [Test]
        public void RefreshDebuggerHandlesEmptyWatchList()
        {
            Script script = new();
            script.LoadString("return 17");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 6);

            Assert.Multiple(() =>
            {
                Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches), Is.True);
                Assert.That(debugger.LastWatchUpdates[WatchType.Watches], Is.Empty);
            });
        }

        [Test]
        public void HardRefreshRequestsBreakpointRefresh()
        {
            Script script = new();
            script.LoadString("return 18");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.HardRefresh);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.Unknown,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, instructionPtr: 7);

            Assert.Multiple(() =>
            {
                Assert.That(
                    processor.GetDebuggerActionForTests(),
                    Is.EqualTo(DebuggerAction.ActionType.Run)
                );
                Assert.That(debugger.RefreshBreakpointsCallCount, Is.GreaterThanOrEqualTo(1));
            });
        }

        [Test]
        public void RuntimeExceptionRefreshesDebuggerWhenSignalRequestsPause()
        {
            Script script = new();

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { SignalRuntimeExceptionResult = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.DebuggerEnabled = true;

            string failingChunk =
                @"
                local function explode()
                    error('boom')
                end
                explode()
            ";

            Assert.That(
                () => script.DoString(failingChunk),
                Throws.TypeOf<ScriptRuntimeException>()
            );

            Assert.Multiple(() =>
            {
                Assert.That(debugger.SignalRuntimeExceptionCallCount, Is.EqualTo(1));
                Assert.That(debugger.UpdateCallCount, Is.GreaterThan(0));
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
            private readonly Queue<DebuggerAction> _actions = new();

            public bool PauseRequested { get; set; }
            public bool SignalRuntimeExceptionResult { get; set; }
            public bool PauseAfterNextUpdate { get; set; }

            public DebuggerCaps Caps { get; set; } =
                DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;

            public int UpdateCallCount { get; private set; }
            public int RefreshBreakpointsCallCount { get; private set; }
            public int SignalRuntimeExceptionCallCount { get; private set; }
            public List<DynamicExpression> WatchItems { get; } = new();
            public Dictionary<WatchType, List<WatchItem>> LastWatchUpdates { get; } = new();

            public void EnqueueAction(DebuggerAction.ActionType actionType)
            {
                _actions.Enqueue(new DebuggerAction() { Action = actionType });
            }

            public void EnqueueAction(DebuggerAction action)
            {
                _actions.Enqueue(action);
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
                SignalRuntimeExceptionCallCount++;
                return SignalRuntimeExceptionResult;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                if (_actions.Count == 0)
                {
                    return new DebuggerAction() { Action = DebuggerAction.ActionType.Run };
                }

                return _actions.Dequeue();
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items)
            {
                UpdateCallCount++;
                LastWatchUpdates[watchType] = items.ToList();
                if (PauseAfterNextUpdate)
                {
                    PauseAfterNextUpdate = false;
                    PauseRequested = true;
                }
            }

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                return WatchItems.Select(expr => expr).ToList();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
            {
                RefreshBreakpointsCallCount++;
            }
        }
    }
}
