namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Threading.Tasks;
    using NovaSharp.RemoteDebugger.Threading;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BlockingChannelTests
    {
        [Test]
        public async Task DequeueBlocksUntilItemArrives()
        {
            BlockingChannel<int> channel = new();

            Task<int> consumer = Task.Run(() => channel.Dequeue());

            await Task.Delay(50).ConfigureAwait(false);
            Assert.That(
                consumer.IsCompleted,
                Is.False,
                "Dequeue should block until an item is enqueued."
            );

            Assert.That(channel.Enqueue(7), Is.True);

            int result = await consumer.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            Assert.That(result, Is.EqualTo(7));
        }

        [Test]
        public async Task StopCausesPendingDequeueToReturnDefault()
        {
            BlockingChannel<string> channel = new();
            Task<string> consumer = Task.Run(() => channel.Dequeue());

            await Task.Delay(50).ConfigureAwait(false);
            channel.Stop();

            string result = await consumer.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void EnqueueAfterStopReturnsFalse()
        {
            BlockingChannel<int> channel = new();
            channel.Stop();

            bool enqueued = channel.Enqueue(42);

            Assert.That(enqueued, Is.False);
        }

        [Test]
        public void StopIsIdempotentAndLaterDequeuesReturnDefault()
        {
            BlockingChannel<int> channel = new();

            channel.Stop();
            channel.Stop(); // second call should be a no-op

            int value = channel.Dequeue();
            Assert.That(value, Is.EqualTo(default(int)));
        }

        [Test]
        public void DequeueReturnsDefaultImmediatelyWhenAlreadyStopped()
        {
            BlockingChannel<string> channel = new();
            channel.Stop();

            string result = channel.Dequeue();

            Assert.That(result, Is.Null);
        }
    }
}
