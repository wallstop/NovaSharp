namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Infrastructure;

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
