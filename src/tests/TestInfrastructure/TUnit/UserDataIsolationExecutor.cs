using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using global::TUnit.Core.Interfaces;
using NovaSharp.Interpreter.DataTypes;

[assembly: global::TUnit.Core.Executors.TestExecutor(
    typeof(NovaSharp.Interpreter.Tests.UserDataIsolationExecutor)
)]

namespace NovaSharp.Interpreter.Tests
{
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated indirectly by TestExecutorAttribute"
    )]
    internal sealed class UserDataIsolationExecutor : ITestExecutor
    {
        private static readonly int MaxParallelism = ResolveMaxParallelism();
        private static readonly SemaphoreSlim IsolationGate = new(MaxParallelism, MaxParallelism);
        private static readonly SemaphoreSlim ExclusiveMutex = new(1, 1);

        public async ValueTask ExecuteTest(
            global::TUnit.Core.TestContext context,
            Func<ValueTask> action
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(action);

            if (!TryGetIsolationSettings(context, out bool serialize))
            {
                await action().ConfigureAwait(false);
                return;
            }

            if (serialize)
            {
                await ExecuteExclusivelyAsync(action).ConfigureAwait(false);
                return;
            }

            await ExecuteConcurrentlyAsync(action).ConfigureAwait(false);
        }

        private static async ValueTask ExecuteConcurrentlyAsync(Func<ValueTask> action)
        {
            await ExclusiveMutex.WaitAsync().ConfigureAwait(false);
            ExclusiveMutex.Release();

            await IsolationGate.WaitAsync().ConfigureAwait(false);

            try
            {
                await RunIsolatedAsync(action).ConfigureAwait(false);
            }
            finally
            {
                IsolationGate.Release();
            }
        }

        private static async ValueTask ExecuteExclusivelyAsync(Func<ValueTask> action)
        {
            await ExclusiveMutex.WaitAsync().ConfigureAwait(false);
            int acquired = 0;

            try
            {
                while (acquired < MaxParallelism)
                {
                    await IsolationGate.WaitAsync().ConfigureAwait(false);
                    acquired += 1;
                }

                await RunIsolatedAsync(action).ConfigureAwait(false);
            }
            finally
            {
                while (acquired > 0)
                {
                    IsolationGate.Release();
                    acquired -= 1;
                }

                ExclusiveMutex.Release();
            }
        }

        private static async ValueTask RunIsolatedAsync(Func<ValueTask> action)
        {
            IDisposable scope = UserData.BeginIsolationScope();
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                scope.Dispose();
            }
        }

        private static bool TryGetIsolationSettings(
            global::TUnit.Core.TestContext context,
            out bool serialize
        )
        {
            serialize = false;

            if (context.Metadata?.TestDetails?.Attributes?.AttributesByType == null)
            {
                return false;
            }

            if (
                context.Metadata.TestDetails.Attributes.AttributesByType.TryGetValue(
                    typeof(UserDataIsolationAttribute),
                    out System.Collections.Generic.IReadOnlyList<Attribute> attributes
                )
            )
            {
                if (
                    attributes.Count > 0
                    && attributes[0] is UserDataIsolationAttribute isolationAttribute
                )
                {
                    serialize = isolationAttribute.Serialize;
                    return true;
                }
            }

            return false;
        }

        private static int ResolveMaxParallelism()
        {
            string configured = Environment.GetEnvironmentVariable(
                "NS_USERDATA_ISOLATION_MAX_PARALLEL"
            );
            if (int.TryParse(configured, out int parsed) && parsed > 0)
            {
                return parsed;
            }

            int logicalProcessors = Environment.ProcessorCount;
            if (logicalProcessors <= 1)
            {
                return 1;
            }

            return Math.Max(logicalProcessors / 2, 1);
        }
    }
}
