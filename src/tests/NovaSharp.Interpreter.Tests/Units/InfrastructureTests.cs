namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
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
        public void InternalErrorExceptionFormatsMessage()
        {
            Type exceptionType = typeof(InternalErrorException);

            InternalErrorException messageOnly = (InternalErrorException)
                exceptionType
                    .GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        binder: null,
                        types: new[] { typeof(string) },
                        modifiers: null
                    )
                    .Invoke(new object[] { "fatal" });
            Assert.That(messageOnly.Message, Does.Contain("fatal"));

            InternalErrorException formatted = (InternalErrorException)
                exceptionType
                    .GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        binder: null,
                        types: new[] { typeof(string), typeof(object[]) },
                        modifiers: null
                    )
                    .Invoke(new object[] { "problem {0}", new object[] { 7 } });
            Assert.That(formatted.Message, Does.Contain("problem 7"));
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
