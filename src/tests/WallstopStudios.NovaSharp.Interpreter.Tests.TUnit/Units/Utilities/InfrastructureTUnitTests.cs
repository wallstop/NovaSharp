namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Utilities
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Diagnostics;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    public sealed class InfrastructureTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PerformanceResultFormatsCounters()
        {
            PerformanceResult result = new()
            {
                Name = "alloc",
                Counter = 2048L,
                Instances = 4,
                Global = true,
                Type = PerformanceCounterType.MemoryBytes,
            };

            string formatted = result.ToString();
            await Assert.That(formatted).Contains("alloc").ConfigureAwait(false);
            await Assert.That(formatted).Contains("(g)").ConfigureAwait(false);
            await Assert.That(formatted).Contains("4 times / 2048 bytes").ConfigureAwait(false);

            string milliseconds = PerformanceResult.PerformanceCounterTypeToString(
                PerformanceCounterType.TimeMilliseconds
            );
            await Assert.That(milliseconds).IsEqualTo("ms").ConfigureAwait(false);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                PerformanceResult.PerformanceCounterTypeToString(
                    (PerformanceCounterType)int.MaxValue
                )
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionExceptionPrefixesMessage()
        {
            DynamicExpressionException formatted = new("value {0}", 42);
            await Assert
                .That(formatted.Message)
                .IsEqualTo("<dynamic>: value 42")
                .ConfigureAwait(false);

            DynamicExpressionException direct = new("plain");
            await Assert.That(direct.Message).IsEqualTo("<dynamic>: plain").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InternalErrorExceptionUsesFallbackMessages()
        {
            InternalErrorException defaultCtor = new();
            await Assert
                .That(defaultCtor.Message)
                .IsEqualTo("Internal error")
                .ConfigureAwait(false);

            InternalErrorException nullMessage = new InternalErrorException(null);
            await Assert
                .That(nullMessage.Message)
                .IsEqualTo("Internal error")
                .ConfigureAwait(false);

            InternalErrorException whitespaceMessage = new InternalErrorException("  ");
            await Assert
                .That(whitespaceMessage.Message)
                .IsEqualTo("Internal error")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InternalErrorExceptionCapturesInnerException()
        {
            InvalidOperationException inner = new("boom");
            InternalErrorException exception = new InternalErrorException("fatal", inner);

            await Assert.That(exception.Message).IsEqualTo("fatal").ConfigureAwait(false);
            await Assert
                .That(exception.InnerException)
                .IsSameReferenceAs(inner)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InternalErrorExceptionFormatsMessages()
        {
            InternalErrorException messageOnly = new InternalErrorException("fatal");
            await Assert.That(messageOnly.Message).Contains("fatal").ConfigureAwait(false);

            InternalErrorException formatted = new InternalErrorException("problem {0}", 7);
            await Assert.That(formatted.Message).Contains("problem 7").ConfigureAwait(false);

            InternalErrorException nullFormat = new InternalErrorException(
                null,
                Array.Empty<object>()
            );
            await Assert.That(nullFormat.Message).IsEqualTo("Internal error").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecutionStateStartsSuspendedWithFreshStacks()
        {
            ExecutionState state = new();

            await Assert.That(state.ValueStack).IsNotNull().ConfigureAwait(false);
            await Assert.That(state.ExecutionStack).IsNotNull().ConfigureAwait(false);
            await Assert.That(state.InstructionPtr).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(state.State)
                .IsEqualTo(CoroutineState.NotStarted)
                .ConfigureAwait(false);
        }
    }
}
