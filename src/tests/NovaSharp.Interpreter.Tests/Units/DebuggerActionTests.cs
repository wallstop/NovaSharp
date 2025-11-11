namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Threading;
    using NovaSharp.Interpreter.Debugging;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebuggerActionTests
    {
        [Test]
        public void ConstructorInitializesTimestampNearCurrentTime()
        {
            DateTime before = DateTime.UtcNow;
            DebuggerAction action = new();
            DateTime after = DateTime.UtcNow;

            Assert.That(action.TimeStampUtc, Is.InRange(before, after));
        }

        [Test]
        public void AgeIncreasesOverTime()
        {
            DebuggerAction action = new();

            TimeSpan initialAge = action.Age;
            Thread.Sleep(5);
            TimeSpan laterAge = action.Age;

            Assert.That(laterAge, Is.GreaterThanOrEqualTo(initialAge));
        }

        [Test]
        public void LinesSetterCopiesAndHandlesNull()
        {
            DebuggerAction action = new();

            int[] original = new[] { 1, 2, 3 };
            action.Lines = original;

            original[0] = 42;

            Assert.That(action.Lines, Is.EqualTo(new[] { 1, 2, 3 }));

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
