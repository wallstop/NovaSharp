namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A thread-safe wrapper around <see cref="StringBuilder"/> that can be used as a <see cref="TextWriter"/>.
    /// This is necessary because <see cref="StringWriter"/> uses <see cref="StringBuilder"/> internally,
    /// which is not thread-safe. When multiple threads write to the same StringWriter (e.g., when
    /// both Console.Out and Console.Error are redirected to it), the StringBuilder's internal state
    /// can become corrupted, causing ArgumentOutOfRangeException on ToString().
    /// </summary>
    internal sealed class ThreadSafeStringWriter : TextWriter
    {
        private readonly StringBuilder _builder = new();
        private readonly object _lock = new();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (_lock)
            {
                _builder.Append(value);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative.");
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(
                    "Invalid offset length: offset + count exceeds the buffer length."
                );
            }

            lock (_lock)
            {
                _builder.Append(buffer, index, count);
            }
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            lock (_lock)
            {
                _builder.Append(value);
            }
        }

        public override void WriteLine(string value)
        {
            lock (_lock)
            {
                _builder.Append(value);
                _builder.Append(NewLine);
            }
        }

        public override void WriteLine()
        {
            lock (_lock)
            {
                _builder.Append(NewLine);
            }
        }

        public override string ToString()
        {
            lock (_lock)
            {
                return _builder.ToString();
            }
        }

        /// <summary>
        /// Returns the current length of the captured content.
        /// </summary>
        public int Length
        {
            get
            {
                lock (_lock)
                {
                    return _builder.Length;
                }
            }
        }

        /// <summary>
        /// Clears all captured content.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _builder.Clear();
            }
        }
    }
}
