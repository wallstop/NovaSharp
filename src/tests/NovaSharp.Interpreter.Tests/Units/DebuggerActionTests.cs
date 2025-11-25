namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebuggerActionTests
    {
        private static readonly int[] BreakpointLines = { 1, 2, 3 };

        [Test]
        public void ConstructorInitializesTimestampNearCurrentTime()
        {
            FakeTimeProvider provider = new();
            DateTime before = provider.GetUtcNow().UtcDateTime;
            DebuggerAction action = new(provider);
            provider.Advance(TimeSpan.FromMilliseconds(1));
            DateTime after = provider.GetUtcNow().UtcDateTime;

            Assert.That(action.TimeStampUtc, Is.InRange(before, after));
        }

        [Test]
        public void AgeIncreasesOverTime()
        {
            FakeTimeProvider provider = new();
            DebuggerAction action = new(provider);

            TimeSpan initialAge = action.Age;
            provider.Advance(TimeSpan.FromMilliseconds(5));
            TimeSpan laterAge = action.Age;

            Assert.That(laterAge, Is.GreaterThanOrEqualTo(initialAge));
        }

        [Test]
        public void LinesSetterCopiesAndHandlesNull()
        {
            DebuggerAction action = new();

            int[] original = (int[])BreakpointLines.Clone();
            action.Lines = original;

            original[0] = 42;

            Assert.That(action.Lines, Is.EqualTo(BreakpointLines));

            action.Lines = null;

            Assert.That(action.Lines.Count, Is.EqualTo(0));
        }

        [Test]
        public void ToStringIncludesBreakpointLocationDetails()
        {
            DebuggerAction action = new()
            {
                Action = DebuggerAction.ActionType.ToggleBreakpoint,
                SourceId = 7,
                SourceLine = 12,
                SourceCol = 3,
            };

            Assert.That(action.ToString(), Is.EqualTo("ToggleBreakpoint 7:(12,3)"));
        }

        [Test]
        public void ToStringFallsBackToActionNameForOtherActions()
        {
            DebuggerAction action = new() { Action = DebuggerAction.ActionType.Run };

            Assert.That(action.ToString(), Is.EqualTo("Run"));
        }
    }
}
