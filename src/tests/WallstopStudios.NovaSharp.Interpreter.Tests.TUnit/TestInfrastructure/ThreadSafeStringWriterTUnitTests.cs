namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;

#pragma warning disable TUnit0055 // Disabled to allow proper disposal in tests
    public sealed class ThreadSafeStringWriterTUnitTests
    {
        [Test]
        public async Task WriteCharIsThreadSafe()
        {
            const int threadCount = 10;
            const int iterationsPerThread = 1000;

            using ThreadSafeStringWriter writer = new();
            List<Task> tasks = new();

            for (int t = 0; t < threadCount; t++)
            {
                char c = (char)('A' + t);
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            writer.Write(c);
                        }
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            string result = writer.ToString();
            await Assert
                .That(result.Length)
                .IsEqualTo(threadCount * iterationsPerThread)
                .ConfigureAwait(false);

            // Verify each character appears the correct number of times
            for (int t = 0; t < threadCount; t++)
            {
                char c = (char)('A' + t);
                int count = result.Count(x => x == c);
                await Assert.That(count).IsEqualTo(iterationsPerThread).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WriteStringIsThreadSafe()
        {
            const int threadCount = 10;
            const int iterationsPerThread = 100;

            using ThreadSafeStringWriter writer = new();
            List<Task> tasks = new();

            for (int t = 0; t < threadCount; t++)
            {
                string marker = $"[THREAD{t}]";
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            writer.Write(marker);
                        }
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            string result = writer.ToString();

            // Verify each thread's marker appears the correct number of times
            for (int t = 0; t < threadCount; t++)
            {
                string marker = $"[THREAD{t}]";
                int count = CountOccurrences(result, marker);
                await Assert.That(count).IsEqualTo(iterationsPerThread).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WriteLineIsThreadSafe()
        {
            const int threadCount = 10;
            const int iterationsPerThread = 100;

            using ThreadSafeStringWriter writer = new();
            List<Task> tasks = new();

            for (int t = 0; t < threadCount; t++)
            {
                string marker = $"THREAD{t}";
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            writer.WriteLine(marker);
                        }
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            string result = writer.ToString();

            // Verify each thread's marker appears the correct number of times
            for (int t = 0; t < threadCount; t++)
            {
                string marker = $"THREAD{t}";
                int count = CountOccurrences(result, marker);
                await Assert.That(count).IsEqualTo(iterationsPerThread).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task ToStringIsThreadSafeWithConcurrentWrites()
        {
            const int writerThreads = 5;
            const int readerThreads = 5;
            const int iterationsPerWriter = 500;
            const int iterationsPerReader = 100;

            using ThreadSafeStringWriter writer = new();
            List<Task> tasks = new();

            // Writer tasks
            for (int t = 0; t < writerThreads; t++)
            {
                string marker = $"[W{t}]";
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerWriter; i++)
                        {
                            writer.Write(marker);
                        }
                    })
                );
            }

            // Reader tasks - these should not throw ArgumentOutOfRangeException
            List<ArgumentOutOfRangeException> readerExceptions = new();
            for (int t = 0; t < readerThreads; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerReader; i++)
                        {
                            try
                            {
                                // This is the operation that would throw ArgumentOutOfRangeException
                                // with a non-thread-safe StringBuilder
                                _ = writer.ToString();
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                lock (readerExceptions)
                                {
                                    readerExceptions.Add(ex);
                                }
                            }
                            Thread.Yield();
                        }
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // The key assertion: no exceptions during concurrent ToString() calls
            string message =
                readerExceptions.Count > 0
                    ? $"Got {readerExceptions.Count} exceptions during concurrent access. First: {readerExceptions[0]}"
                    : "No exceptions expected";
            await Assert.That(readerExceptions.Count).IsEqualTo(0).Because(message);

            // Verify all writes completed
            string result = writer.ToString();
            for (int t = 0; t < writerThreads; t++)
            {
                string marker = $"[W{t}]";
                int count = CountOccurrences(result, marker);
                await Assert.That(count).IsEqualTo(iterationsPerWriter).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task LengthPropertyIsThreadSafe()
        {
            const int threadCount = 5;
            const int iterationsPerThread = 100;

            using ThreadSafeStringWriter writer = new();
            List<Task> tasks = new();

            for (int t = 0; t < threadCount; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            writer.Write("X");
                            _ = writer.Length; // Should not throw
                        }
                    })
                );
            }

            // Should not throw any exceptions
            await Task.WhenAll(tasks).ConfigureAwait(false);

            await Assert
                .That(writer.Length)
                .IsEqualTo(threadCount * iterationsPerThread)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ClearIsThreadSafe()
        {
            using ThreadSafeStringWriter writer = new();
            await writer.WriteAsync("initial content").ConfigureAwait(false);

            await Assert.That(writer.Length).IsGreaterThan(0).ConfigureAwait(false);

            writer.Clear();

            await Assert.That(writer.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(writer.ToString()).IsEqualTo(string.Empty).ConfigureAwait(false);
        }

        [Test]
        public async Task WriteCharArrayIsThreadSafe()
        {
            const int threadCount = 5;
            const int iterationsPerThread = 100;

            using ThreadSafeStringWriter writer = new();
            List<Task> tasks = new();

            for (int t = 0; t < threadCount; t++)
            {
                char[] buffer = new char[] { (char)('0' + t), '-' };
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            writer.Write(buffer, 0, buffer.Length);
                        }
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            string result = writer.ToString();
            await Assert
                .That(result.Length)
                .IsEqualTo(threadCount * iterationsPerThread * 2)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WriteCharArrayThrowsOnNullBuffer()
        {
            using ThreadSafeStringWriter writer = new();

            await Assert
                .That(() => writer.Write(null, 0, 0))
                .Throws<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WriteCharArrayThrowsOnNegativeIndex()
        {
            using ThreadSafeStringWriter writer = new();
            char[] buffer = new char[] { 'a', 'b', 'c' };

            await Assert
                .That(() => writer.Write(buffer, -1, 1))
                .Throws<ArgumentOutOfRangeException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WriteCharArrayThrowsOnNegativeCount()
        {
            using ThreadSafeStringWriter writer = new();
            char[] buffer = new char[] { 'a', 'b', 'c' };

            await Assert
                .That(() => writer.Write(buffer, 0, -1))
                .Throws<ArgumentOutOfRangeException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WriteCharArrayThrowsOnInvalidOffsetLength()
        {
            using ThreadSafeStringWriter writer = new();
            char[] buffer = new char[] { 'a', 'b', 'c' };

            await Assert
                .That(() => writer.Write(buffer, 2, 5))
                .Throws<ArgumentException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WriteNullStringDoesNothing()
        {
            using ThreadSafeStringWriter writer = new();

            await writer.WriteAsync((string)null).ConfigureAwait(false);

            await Assert.That(writer.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }
    }
#pragma warning restore TUnit0055
}
