namespace NovaSharp.RemoteDebugger.Threading
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Thread-safe channel that blocks dequeuers until items arrive and supports cooperative shutdown.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    public class BlockingChannel<T>
    {
        private readonly Queue<T> _queue = new();
        private bool _stopped;

        /// <summary>
        /// Adds an item to the queue unless it has already been stopped.
        /// </summary>
        /// <param name="item">Item to enqueue.</param>
        /// <returns><c>true</c> when the item was queued; <c>false</c> if the queue is stopping.</returns>
        public bool Enqueue(T item)
        {
            if (_stopped)
            {
                return false;
            }

            lock (_queue)
            {
                if (_stopped)
                {
                    return false;
                }

                _queue.Enqueue(item);
                Monitor.Pulse(_queue);
            }
            return true;
        }

        /// <summary>
        /// Removes the next available item, blocking until one arrives or the queue is stopped.
        /// </summary>
        /// <returns>
        /// The dequeued item, or the default value of <typeparamref name="T"/> when the queue stops.
        /// </returns>
        public T Dequeue()
        {
            if (_stopped)
            {
                return default(T);
            }

            lock (_queue)
            {
                if (_stopped)
                {
                    return default(T);
                }

                while (_queue.Count == 0)
                {
                    Monitor.Wait(_queue);
                    if (_stopped)
                    {
                        return default(T);
                    }
                }
                return _queue.Dequeue();
            }
        }

        /// <summary>
        /// Signals the queue to stop, unblocking any waiting producers/consumers.
        /// </summary>
        public void Stop()
        {
            if (_stopped)
            {
                return;
            }

            lock (_queue)
            {
                if (_stopped)
                {
                    return;
                }

                _stopped = true;
                Monitor.PulseAll(_queue);
            }
        }
    }
}
