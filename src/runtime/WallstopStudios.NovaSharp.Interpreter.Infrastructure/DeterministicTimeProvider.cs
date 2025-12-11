namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;

    /// <summary>
    /// Time provider for deterministic execution. Returns a controllable time value
    /// that can be advanced programmatically, making it suitable for lockstep multiplayer,
    /// replays, and testing scenarios.
    /// </summary>
    public sealed class DeterministicTimeProvider : ITimeProvider
    {
        /// <summary>
        /// Default starting time (2020-01-01 00:00:00 UTC).
        /// </summary>
        public static readonly DateTimeOffset DefaultStartTime = new DateTimeOffset(
            2020,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero
        );

        private DateTimeOffset _currentTime;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance starting at the default time.
        /// </summary>
        public DeterministicTimeProvider()
            : this(DefaultStartTime) { }

        /// <summary>
        /// Initializes a new instance starting at the specified time.
        /// </summary>
        /// <param name="startTime">The initial time value.</param>
        public DeterministicTimeProvider(DateTimeOffset startTime)
        {
            _currentTime = startTime;
        }

        /// <inheritdoc />
        public DateTimeOffset GetUtcNow()
        {
            lock (_lock)
            {
                return _currentTime;
            }
        }

        /// <summary>
        /// Sets the current time to a specific value.
        /// </summary>
        /// <param name="time">The new current time.</param>
        public void SetTime(DateTimeOffset time)
        {
            lock (_lock)
            {
                _currentTime = time;
            }
        }

        /// <summary>
        /// Advances the current time by the specified duration.
        /// </summary>
        /// <param name="duration">The amount of time to advance.</param>
        public void Advance(TimeSpan duration)
        {
            lock (_lock)
            {
                _currentTime = _currentTime.Add(duration);
            }
        }

        /// <summary>
        /// Advances the current time by the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds to advance.</param>
        public void AdvanceSeconds(double seconds)
        {
            Advance(TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Advances the current time by the specified number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds to advance.</param>
        public void AdvanceMilliseconds(double milliseconds)
        {
            Advance(TimeSpan.FromMilliseconds(milliseconds));
        }

        /// <summary>
        /// Resets the time to the specified value (defaults to <see cref="DefaultStartTime"/>).
        /// </summary>
        /// <param name="time">
        /// The time to reset to, or null to use <see cref="DefaultStartTime"/>.
        /// </param>
        public void Reset(DateTimeOffset? time = null)
        {
            lock (_lock)
            {
                _currentTime = time ?? DefaultStartTime;
            }
        }
    }
}
