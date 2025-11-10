namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using DataTypes;
    using Errors;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InfrastructureTests
    {
        [Test]
        public void PerformanceResultFormatsCounters()
        {
            PerformanceResult result = new();

            SetProperty(result, "Name", "alloc");
            SetProperty(result, "Counter", 2048L);
            SetProperty(result, "Instances", 4);
            SetProperty(result, "Global", true);
            SetProperty(result, "Type", PerformanceCounterType.MemoryBytes);

            string formatted = result.ToString();
            Assert.Multiple(() =>
            {
                Assert.That(formatted, Does.Contain("alloc"));
                Assert.That(formatted, Does.Contain("(g)"));
                Assert.That(formatted, Does.Contain("4 times / 2048 bytes"));
            });

            SetProperty(result, "Type", PerformanceCounterType.TimeMilliseconds);
            string milliseconds = PerformanceResult.PerformanceCounterTypeToString(
                PerformanceCounterType.TimeMilliseconds
            );

            Assert.That(milliseconds, Is.EqualTo("ms"));
            Assert.That(
                () =>
                    PerformanceResult.PerformanceCounterTypeToString(
                        (PerformanceCounterType)int.MaxValue
                    ),
                Throws.TypeOf<InvalidOperationException>()
            );
        }

        [Test]
        public void DynamicExpressionExceptionPrefixesMessage()
        {
            DynamicExpressionException formatted = new("value {0}", 42);
            Assert.That(formatted.Message, Is.EqualTo("<dynamic>: value 42"));

            DynamicExpressionException direct = new("plain");
            Assert.That(direct.Message, Is.EqualTo("<dynamic>: plain"));
        }

        [Test]
        public void InternalErrorExceptionDefaultsToFallbackMessage()
        {
            InternalErrorException defaultCtor = new();
            Assert.That(defaultCtor.Message, Is.EqualTo("Internal error"));

            InternalErrorException nullMessage = new InternalErrorException(null);
            Assert.That(nullMessage.Message, Is.EqualTo("Internal error"));

            InternalErrorException whitespaceMessage = new InternalErrorException("  ");
            Assert.That(whitespaceMessage.Message, Is.EqualTo("Internal error"));
        }

        [Test]
        public void InternalErrorExceptionCapturesInnerException()
        {
            InvalidOperationException inner = new("boom");
            InternalErrorException exception = new InternalErrorException("fatal", inner);

            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Is.EqualTo("fatal"));
                Assert.That(exception.InnerException, Is.SameAs(inner));
            });
        }

        [Test]
        public void InternalErrorExceptionFormatsMessage()
        {
            InternalErrorException messageOnly = new InternalErrorException("fatal");
            Assert.That(messageOnly.Message, Does.Contain("fatal"));

            InternalErrorException formatted = new InternalErrorException("problem {0}", 7);
            Assert.That(formatted.Message, Does.Contain("problem 7"));

            InternalErrorException nullFormat = new InternalErrorException(
                null,
                Array.Empty<object>()
            );
            Assert.That(nullFormat.Message, Is.EqualTo("Internal error"));
        }

        [Test]
        public void ExecutionStateStartsSuspendedWithFreshStacks()
        {
            Type stateType = typeof(Script).Assembly.GetType(
                "NovaSharp.Interpreter.Execution.VM.ExecutionState",
                true
            );

            object state = Activator.CreateInstance(stateType, nonPublic: true);

            FieldInfo valueStackField = stateType.GetField(
                "valueStack",
                BindingFlags.Instance | BindingFlags.Public
            );
            FieldInfo executionStackField = stateType.GetField(
                "executionStack",
                BindingFlags.Instance | BindingFlags.Public
            );
            FieldInfo instructionPtrField = stateType.GetField(
                "instructionPtr",
                BindingFlags.Instance | BindingFlags.Public
            );
            FieldInfo coroutineStateField = stateType.GetField(
                "state",
                BindingFlags.Instance | BindingFlags.Public
            );

            Assert.Multiple(() =>
            {
                Assert.That(valueStackField.GetValue(state), Is.Not.Null);
                Assert.That(executionStackField.GetValue(state), Is.Not.Null);
                Assert.That(instructionPtrField.GetValue(state), Is.EqualTo(0));
                Assert.That(
                    coroutineStateField.GetValue(state),
                    Is.EqualTo(CoroutineState.NotStarted)
                );
            });
        }

        private static void SetProperty<TInstance>(
            TInstance instance,
            string propertyName,
            object value
        )
        {
            PropertyInfo property = typeof(TInstance).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            property.SetValue(instance, value);
        }
    }
}
