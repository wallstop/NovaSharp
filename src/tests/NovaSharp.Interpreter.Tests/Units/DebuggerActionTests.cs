namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Infrastructure;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebuggerActionTests
    {
        [Test]
        public void TimeStampUsesInjectedProvider()
        {
            DateTimeOffset fixedTime = new(2025, 11, 26, 17, 45, 12, TimeSpan.Zero);
            FixedTimeProvider provider = new(fixedTime);

            DebuggerAction action = new(provider);

            Assert.That(action.TimeStampUtc, Is.EqualTo(fixedTime.UtcDateTime));
        }

        [Test]
        public void LinesSetterCopiesInputAndTreatsNullAsEmpty()
        {
            DebuggerAction action = new();
            int[] source = new[] { 10, 20, 30 };

            action.Lines = source;

            Assert.That(action.Lines, Is.EquivalentTo(source));

            source[0] = 999;
            Assert.That(action.Lines[0], Is.Not.EqualTo(source[0]), "Lines should be copied");

            action.Lines = null;
            Assert.That(action.Lines, Is.Not.Null);
            Assert.That(action.Lines, Is.Empty);
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
    }
}
