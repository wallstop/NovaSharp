namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Expressions;
    using static ProcessorDebuggerTestHelpers;
    using ThrowingExpression = NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution.ProcessorDebuggerTestHelpers.ThrowingExpression;

    public sealed class ProcessorDebuggerBreakpointAndRefreshTUnitTests
    {
        private static readonly int[] SingleLineOne = { 1 };

        [global::TUnit.Core.Test]
        public async Task ToggleBreakpointTogglesExistingLocation()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, 0);
            await Assert.That(target.Breakpoint).IsTrue();

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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            processor.ListenDebuggerForTests(instruction, 0);
            await Assert.That(target.Breakpoint).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SetAndClearBreakpointUpdateBreakpointCollection()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, 0);

            IReadOnlyList<SourceRef> breakpoints = processor.GetBreakpointsForTests();
            await Assert.That(breakpoints.Count).IsEqualTo(1);
            await Assert.That(ReferenceEquals(breakpoints[0], target)).IsTrue();

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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );
            processor.ListenDebuggerForTests(instruction, 0);

            await Assert.That(processor.GetBreakpointsForTests().Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task ResetBreakpointsAppliesRequestedLines()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, 0);

            int[] activeLines = source
                .Refs.Where(r => r.Breakpoint)
                .Select(r => r.FromLine)
                .Distinct()
                .OrderBy(line => line)
                .ToArray();
            int[] expectedLines = candidateLines.OrderBy(line => line).ToArray();

            await Assert.That(activeLines.Length).IsEqualTo(expectedLines.Length);
            for (int i = 0; i < activeLines.Length; i++)
            {
                await Assert.That(activeLines[i]).IsEqualTo(expectedLines[i]);
            }
        }

        [global::TUnit.Core.Test]
        public async Task HardRefreshInvokesBreakpointRefresh()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, 0);

            await Assert.That(debugger.RefreshBreakpointsCallCount).IsEqualTo(1);
            await Assert.That(debugger.UpdateCallCount > 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RefreshActionDoesNotInvokeBreakpointRefresh()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, 0);

            await Assert.That(debugger.RefreshBreakpointsCallCount).IsEqualTo(0);
            await Assert.That(debugger.UpdateCallCount > 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ClearBreakpointSnapsToNearestLocationWhenNoExactMatchExists()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 9);

            await Assert.That(processor.GetBreakpointsForTests().Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task SetBreakpointReturnsFalseWhenSourceHasNoRefs()
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
                    Lines = SingleLineOne,
                }
            );
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            await Assert.That(processor.GetBreakpointsForTests().Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task HardRefreshRequestsBreakpointRefresh()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 7);

            await Assert.That(debugger.RefreshBreakpointsCallCount > 0).IsTrue();
            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
        }

        [global::TUnit.Core.Test]
        public async Task RefreshActionProcessesNextQueuedDebuggerCommand()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 8);

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert.That(debugger.UpdateCallCount >= 2).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PauseRequestDuringRefreshStillProcessesQueuedAction()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 1);

            await Assert.That(debugger.PauseRequested).IsTrue();
            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
        }

        [global::TUnit.Core.Test]
        public async Task RefreshDebuggerEvaluatesWatchExpressions()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 3);

            await Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches)).IsTrue();
            List<WatchItem> watches = debugger.LastWatchUpdates[WatchType.Watches];
            await Assert.That(watches.Count).IsEqualTo(1);
            await Assert.That(watches[0].Name).IsEqualTo("const_watch");
            await Assert.That(watches[0].Value.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task RefreshDebuggerCapturesWatchEvaluationErrors()
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
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 4);

            await Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches)).IsTrue();
            WatchItem watch = debugger.LastWatchUpdates[WatchType.Watches].Single();
            await Assert.That(watch.IsError).IsTrue();
            await Assert.That(watch.Value.Type).IsEqualTo(DataType.String);
            await Assert
                .That(
                    watch.Value.String.Contains("cannot call functions", StringComparison.Ordinal)
                )
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RefreshDebuggerCapturesClrWatchExceptions()
        {
            Script script = new();
            script.LoadString("return 16");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            const string message = "watch exploded";
            ScriptLoadingContext loadingContext = new(script);
            Expression throwingExpression = new ThrowingExpression(loadingContext, message);
            DynamicExprExpression dynamicExpression = new(throwingExpression, loadingContext);
            DynamicExpression failingWatch = new(script, "clrThrow", dynamicExpression);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.WatchItems.Add(failingWatch);
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            await Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches)).IsTrue();
            WatchItem watch = debugger.LastWatchUpdates[WatchType.Watches].Single();
            await Assert.That(watch.IsError).IsTrue();
            await Assert.That(watch.Value.Type).IsEqualTo(DataType.String);
            await Assert
                .That(watch.Value.String.Contains(message, StringComparison.Ordinal))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RefreshDebuggerHandlesEmptyWatchList()
        {
            Script script = new();
            script.LoadString("return 17");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: SourceRef.GetClrLocation()
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 6);

            await Assert.That(debugger.LastWatchUpdates.ContainsKey(WatchType.Watches)).IsTrue();
            await Assert.That(debugger.LastWatchUpdates[WatchType.Watches].Count).IsEqualTo(0);
        }
    }
}
