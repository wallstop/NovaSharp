namespace NovaSharp.Interpreter.Tests.TestUtilities
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    internal static class TestWaitHelpers
    {
        internal static bool SpinUntil(
            Func<bool> condition,
            TimeSpan timeout,
            int sleepMilliseconds = 5
        )
        {
            ArgumentNullException.ThrowIfNull(condition);

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (condition())
                {
                    return true;
                }

                Thread.Sleep(sleepMilliseconds);
            }

            return condition();
        }

        internal static void SpinUntilOrThrow(
            Func<bool> condition,
            TimeSpan timeout,
            string failureMessage,
            int sleepMilliseconds = 5
        )
        {
            if (!SpinUntil(condition, timeout, sleepMilliseconds))
            {
                throw new TimeoutException(failureMessage);
            }
        }
    }
}
