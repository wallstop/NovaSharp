namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Debugging
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;

    public sealed class DebuggerActionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TimeStampUsesInjectedProvider()
        {
            DateTimeOffset fixedTime = new(2025, 11, 26, 17, 45, 12, TimeSpan.Zero);
            FixedTimeProvider provider = new(fixedTime);

            DebuggerAction action = new(provider);

            await Assert
                .That(action.TimeStampUtc)
                .IsEqualTo(fixedTime.UtcDateTime)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LinesSetterCopiesInputAndTreatsNullAsEmpty()
        {
            DebuggerAction action = new();
            int[] source = { 10, 20, 30 };

            action.Lines = source;
            await Assert.That(action.Lines).IsEquivalentTo(source).ConfigureAwait(false);

            source[0] = 999;
            await Assert.That(action.Lines[0]).IsNotEqualTo(source[0]).ConfigureAwait(false);

            action.Lines = null;
            await Assert.That(action.Lines).IsNotNull().ConfigureAwait(false);
            await Assert.That(action.Lines.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringFormatsBreakpointActionsWithSourceLocation()
        {
            DebuggerAction toggleAction = new()
            {
                Action = DebuggerAction.ActionType.ToggleBreakpoint,
                SourceId = 1,
                SourceLine = 42,
                SourceCol = 10,
            };

            await Assert
                .That(toggleAction.ToString())
                .IsEqualTo("ToggleBreakpoint 1:(42,10)")
                .ConfigureAwait(false);

            DebuggerAction setAction = new()
            {
                Action = DebuggerAction.ActionType.SetBreakpoint,
                SourceId = 2,
                SourceLine = 99,
                SourceCol = 5,
            };

            await Assert
                .That(setAction.ToString())
                .IsEqualTo("SetBreakpoint 2:(99,5)")
                .ConfigureAwait(false);

            DebuggerAction clearAction = new()
            {
                Action = DebuggerAction.ActionType.ClearBreakpoint,
                SourceId = 3,
                SourceLine = 15,
                SourceCol = 1,
            };

            await Assert
                .That(clearAction.ToString())
                .IsEqualTo("ClearBreakpoint 3:(15,1)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringReturnsActionNameForNonBreakpointActions()
        {
            DebuggerAction runAction = new() { Action = DebuggerAction.ActionType.Run };

            await Assert.That(runAction.ToString()).IsEqualTo("Run").ConfigureAwait(false);

            DebuggerAction stepInAction = new() { Action = DebuggerAction.ActionType.StepIn };

            await Assert.That(stepInAction.ToString()).IsEqualTo("StepIn").ConfigureAwait(false);

            DebuggerAction stepOverAction = new() { Action = DebuggerAction.ActionType.StepOver };

            await Assert
                .That(stepOverAction.ToString())
                .IsEqualTo("StepOver")
                .ConfigureAwait(false);

            DebuggerAction stepOutAction = new() { Action = DebuggerAction.ActionType.StepOut };

            await Assert.That(stepOutAction.ToString()).IsEqualTo("StepOut").ConfigureAwait(false);

            DebuggerAction refreshAction = new() { Action = DebuggerAction.ActionType.Refresh };

            await Assert.That(refreshAction.ToString()).IsEqualTo("Refresh").ConfigureAwait(false);

            DebuggerAction hardRefreshAction = new()
            {
                Action = DebuggerAction.ActionType.HardRefresh,
            };

            await Assert
                .That(hardRefreshAction.ToString())
                .IsEqualTo("HardRefresh")
                .ConfigureAwait(false);

            DebuggerAction noneAction = new() { Action = DebuggerAction.ActionType.None };

            await Assert.That(noneAction.ToString()).IsEqualTo("None").ConfigureAwait(false);

            DebuggerAction resetAction = new()
            {
                Action = DebuggerAction.ActionType.ResetBreakpoints,
            };

            await Assert
                .That(resetAction.ToString())
                .IsEqualTo("ResetBreakpoints")
                .ConfigureAwait(false);

            DebuggerAction byteCodeStepInAction = new()
            {
                Action = DebuggerAction.ActionType.ByteCodeStepIn,
            };

            await Assert
                .That(byteCodeStepInAction.ToString())
                .IsEqualTo("ByteCodeStepIn")
                .ConfigureAwait(false);

            DebuggerAction byteCodeStepOverAction = new()
            {
                Action = DebuggerAction.ActionType.ByteCodeStepOver,
            };

            await Assert
                .That(byteCodeStepOverAction.ToString())
                .IsEqualTo("ByteCodeStepOver")
                .ConfigureAwait(false);

            DebuggerAction byteCodeStepOutAction = new()
            {
                Action = DebuggerAction.ActionType.ByteCodeStepOut,
            };

            await Assert
                .That(byteCodeStepOutAction.ToString())
                .IsEqualTo("ByteCodeStepOut")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AgeReturnsTimeSinceCreation()
        {
            AdvancingTimeProvider provider = new(
                new DateTimeOffset(2025, 11, 26, 10, 0, 0, TimeSpan.Zero)
            );

            DebuggerAction action = new(provider);

            // Time has not advanced yet
            await Assert.That(action.Age).IsEqualTo(TimeSpan.Zero).ConfigureAwait(false);

            // Advance time by 5 seconds
            provider.Advance(TimeSpan.FromSeconds(5));

            await Assert.That(action.Age).IsEqualTo(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            // Advance time by another 10 seconds
            provider.Advance(TimeSpan.FromSeconds(10));

            await Assert.That(action.Age).IsEqualTo(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultConstructorUsesSystemTimeProvider()
        {
            DebuggerAction action = new();

            // Verify that timestamp is recent (within last second)
            TimeSpan elapsed = DateTime.UtcNow - action.TimeStampUtc;
            await Assert.That(elapsed.TotalSeconds).IsLessThan(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithNullProviderFallsBackToSystemProvider()
        {
            DebuggerAction action = new(null);

            // Verify that timestamp is recent (within last second)
            TimeSpan elapsed = DateTime.UtcNow - action.TimeStampUtc;
            await Assert.That(elapsed.TotalSeconds).IsLessThan(1).ConfigureAwait(false);
        }

        private sealed class FixedTimeProvider : ITimeProvider
        {
            private readonly DateTimeOffset _timestamp;

            public FixedTimeProvider(DateTimeOffset timestamp)
            {
                _timestamp = timestamp;
            }

            public DateTimeOffset GetUtcNow()
            {
                return _timestamp;
            }
        }

        private sealed class AdvancingTimeProvider : ITimeProvider
        {
            private DateTimeOffset _current;

            public AdvancingTimeProvider(DateTimeOffset start)
            {
                _current = start;
            }

            public void Advance(TimeSpan duration)
            {
                _current = _current.Add(duration);
            }

            public DateTimeOffset GetUtcNow()
            {
                return _current;
            }
        }
    }
}
