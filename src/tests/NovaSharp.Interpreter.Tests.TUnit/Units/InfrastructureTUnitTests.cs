#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.VM;

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
            await Assert.That(formatted).Contains("alloc");
            await Assert.That(formatted).Contains("(g)");
            await Assert.That(formatted).Contains("4 times / 2048 bytes");

            string milliseconds = PerformanceResult.PerformanceCounterTypeToString(
                PerformanceCounterType.TimeMilliseconds
            );
            await Assert.That(milliseconds).IsEqualTo("ms");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                PerformanceResult.PerformanceCounterTypeToString(
                    (PerformanceCounterType)int.MaxValue
                )
            );
            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionExceptionPrefixesMessage()
        {
            DynamicExpressionException formatted = new("value {0}", 42);
            await Assert.That(formatted.Message).IsEqualTo("<dynamic>: value 42");

            DynamicExpressionException direct = new("plain");
            await Assert.That(direct.Message).IsEqualTo("<dynamic>: plain");
        }

        [global::TUnit.Core.Test]
        public async Task InternalErrorExceptionUsesFallbackMessages()
        {
            InternalErrorException defaultCtor = new();
            await Assert.That(defaultCtor.Message).IsEqualTo("Internal error");

            InternalErrorException nullMessage = new InternalErrorException(null);
            await Assert.That(nullMessage.Message).IsEqualTo("Internal error");

            InternalErrorException whitespaceMessage = new InternalErrorException("  ");
            await Assert.That(whitespaceMessage.Message).IsEqualTo("Internal error");
        }

        [global::TUnit.Core.Test]
        public async Task InternalErrorExceptionCapturesInnerException()
        {
            InvalidOperationException inner = new("boom");
            InternalErrorException exception = new InternalErrorException("fatal", inner);

            await Assert.That(exception.Message).IsEqualTo("fatal");
            await Assert.That(exception.InnerException).IsSameReferenceAs(inner);
        }

        [global::TUnit.Core.Test]
        public async Task InternalErrorExceptionFormatsMessages()
        {
            InternalErrorException messageOnly = new InternalErrorException("fatal");
            await Assert.That(messageOnly.Message).Contains("fatal");

            InternalErrorException formatted = new InternalErrorException("problem {0}", 7);
            await Assert.That(formatted.Message).Contains("problem 7");

            InternalErrorException nullFormat = new InternalErrorException(
                null,
                Array.Empty<object>()
            );
            await Assert.That(nullFormat.Message).IsEqualTo("Internal error");
        }

        [global::TUnit.Core.Test]
        public async Task ExecutionStateStartsSuspendedWithFreshStacks()
        {
            ExecutionState state = new();

            await Assert.That(state.ValueStack).IsNotNull();
            await Assert.That(state.ExecutionStack).IsNotNull();
            await Assert.That(state.InstructionPtr).IsEqualTo(0);
            await Assert.That(state.State).IsEqualTo(CoroutineState.NotStarted);
        }
    }
}
#pragma warning restore CA2007
