namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using static ProcessorDebuggerTestHelpers;

    public sealed class ProcessorDebuggerSteppingTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ListenDebuggerUpdatesHighlightForLineBasedRun()
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

            SourceRef next = new(0, 0, 0, 2, 2, true);
            Instruction instruction = new(next) { OpCode = OpCode.Debug };

            processor.ListenDebuggerForTests(instruction, 0);

            await Assert
                .That(
                    ReferenceEquals(processor.GetLastHighlightForTests(), instruction.SourceCodeRef)
                )
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PauseRequestResetsDebuggerActionBeforeRefreshing()
        {
            Script script = new();
            script.LoadString("return 0");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { PauseRequested = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            SourceRef location = new(0, 0, 0, 1, 1, true);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepIn,
                actionTarget: 123,
                executionStackDepth: 0,
                lastHighlight: location
            );

            Instruction instruction = new(location) { OpCode = OpCode.Debug };
            instruction.SourceCodeRef.Breakpoint = true;

            processor.ListenDebuggerForTests(instruction, 0);

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task ByteCodeStepOverSkipsUntilTargetInstruction()
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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.ByteCodeStepOver);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(10);
        }

        [global::TUnit.Core.Test]
        public async Task ByteCodeStepOverTriggersActionWhenTargetReached()
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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task StepOutAdvancesWhenStackDepthDrops()
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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task DebuggerActionQueueSetsByteCodeStepOverState()
        {
            Script script = new();
            script.LoadString("return 8");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.ByteCodeStepOver);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.ByteCodeStepOver);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(6);
        }

        [global::TUnit.Core.Test]
        public async Task StepOverReturnsWhenSameLocationAndDeeperStack()
        {
            Script script = new();
            script.LoadString("return 9");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 2);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            SourceRef location = new(0, 0, 0, 1, 1, true);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepOver,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: location
            );

            Instruction instruction = new(location) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 1);

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.StepOver);
        }

        [global::TUnit.Core.Test]
        public async Task StepOverContinuesWhenLocationChanges()
        {
            Script script = new();
            script.LoadString("return 9");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            SourceRef previous = new(0, 0, 0, 1, 1, true);
            SourceRef updated = new(0, 0, 0, 2, 2, true);

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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert
                .That(ReferenceEquals(processor.GetLastHighlightForTests(), updated))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ByteCodeStepOutAdvancesWhenStackDepthDrops()
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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task StepOutReturnsWhenStackIsDeeper()
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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.StepOut);
        }

        [global::TUnit.Core.Test]
        public async Task StepInReturnsEarlyWhenLocationMatchesHighlight()
        {
            Script script = new();
            script.LoadString("return 11");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            StubDebugger debugger = new();
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            SourceRef location = new(0, 0, 0, 1, 1, true);
            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.StepIn,
                actionTarget: -1,
                executionStackDepth: 1,
                lastHighlight: location
            );

            Instruction instruction = new(location) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 5);

            await Assert.That(debugger.UpdateCallCount).IsEqualTo(0);
            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.StepIn);
        }

        [global::TUnit.Core.Test]
        public async Task StepInContinuesWhenLocationIsNew()
        {
            Script script = new();
            script.LoadString("return 12");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor, frameCount: 1);

            SourceRef previous = new(0, 0, 0, 1, 1, true);
            SourceRef updated = new(0, 0, 0, 3, 3, true);

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

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.Run);
            await Assert
                .That(ReferenceEquals(processor.GetLastHighlightForTests(), updated))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ByteCodeStepOutReturnsEarlyWhenDepthNotReduced()
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

            await Assert.That(debugger.UpdateCallCount).IsEqualTo(0);
            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.ByteCodeStepOut);
        }

        [global::TUnit.Core.Test]
        public async Task DebuggerActionQueueSetsStepInState()
        {
            Script script = new();
            script.LoadString("return 13");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.StepIn);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 7);

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.StepIn);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(-1);
            await Assert.That(debugger.UpdateCallCount > 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DebuggerActionQueueSetsByteCodeStepInState()
        {
            Script script = new();
            script.LoadString("return 27");

            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new();
            debugger.EnqueueAction(DebuggerAction.ActionType.ByteCodeStepIn);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);

            processor.ConfigureDebuggerActionForTests(
                DebuggerAction.ActionType.None,
                actionTarget: -1,
                executionStackDepth: 0,
                lastHighlight: null
            );

            Instruction instruction = new(SourceRef.GetClrLocation()) { OpCode = OpCode.Debug };
            processor.ListenDebuggerForTests(instruction, instructionPtr: 11);

            await Assert
                .That(processor.GetDebuggerActionForTests())
                .IsEqualTo(DebuggerAction.ActionType.ByteCodeStepIn);
            await Assert.That(processor.GetDebuggerActionTargetForTests()).IsEqualTo(-1);
        }
    }
}
