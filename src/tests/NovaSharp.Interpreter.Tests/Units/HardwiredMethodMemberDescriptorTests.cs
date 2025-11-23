namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwiredMethodMemberDescriptorTests
    {
        [Test]
        public void ExecuteUsesDefaultValueWhenArgumentMissing()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(7));
            SampleHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.InvocationCount, Is.EqualTo(1));
                Assert.That(descriptor.LastArgCount, Is.EqualTo(2));
                Assert.That(descriptor.LastParameters[0], Is.EqualTo(7));
                Assert.That(descriptor.LastParameters[1], Is.EqualTo("fallback"));
                Assert.That(result.String, Is.EqualTo("7:fallback:2"));
            });
        }

        [Test]
        public void ExecutePassesExplicitArgumentsAndReturnsDynValue()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(2),
                DynValue.NewString("overridden")
            );
            SampleHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.InvocationCount, Is.EqualTo(1));
                Assert.That(descriptor.LastArgCount, Is.EqualTo(2));
                Assert.That(descriptor.LastParameters[1], Is.EqualTo("overridden"));
                Assert.That(result.String, Is.EqualTo("2:overridden:2"));
            });
        }

        [Test]
        public void ExecuteUsesDefaultValuePlaceholderToReduceArgumentCount()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(42));
            SentinelHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.InvocationCount, Is.EqualTo(1));
                Assert.That(descriptor.LastArgCount, Is.EqualTo(1));
                Assert.That(descriptor.LastParameters[1], Is.InstanceOf<DefaultValue>());
                Assert.That(result.String, Is.EqualTo("sentinel:42:1"));
            });
        }

        [Test]
        public void ExecuteCountsExplicitOptionalArgumentWhenPlaceholderProvided()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(10),
                DynValue.NewString("custom")
            );
            SentinelHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.InvocationCount, Is.EqualTo(1));
                Assert.That(descriptor.LastArgCount, Is.EqualTo(2));
                Assert.That(descriptor.LastParameters[1], Is.EqualTo("custom"));
                Assert.That(result.String, Is.EqualTo("custom:10:2"));
            });
        }

        private sealed class SampleHardwiredDescriptor : HardwiredMethodMemberDescriptor
        {
            public int InvocationCount { get; private set; }
            public object[] LastParameters { get; private set; } = Array.Empty<object>();
            public int LastArgCount { get; private set; }

            public SampleHardwiredDescriptor()
            {
                ParameterDescriptor[] parameters =
                {
                    new ParameterDescriptor("value", typeof(int)),
                    new ParameterDescriptor(
                        "optional",
                        typeof(string),
                        hasDefaultValue: true,
                        defaultValue: "fallback"
                    ),
                };

                Initialize("Sample", isStatic: true, parameters, isExtensionMethod: false);
            }

            protected override object Invoke(
                Script script,
                object obj,
                object[] pars,
                int argscount
            )
            {
                InvocationCount++;
                LastParameters = pars;
                LastArgCount = argscount;
                return $"{pars[0]}:{pars[1]}:{argscount}";
            }
        }

        private sealed class SentinelHardwiredDescriptor : HardwiredMethodMemberDescriptor
        {
            public int InvocationCount { get; private set; }
            public object[] LastParameters { get; private set; } = Array.Empty<object>();
            public int LastArgCount { get; private set; }

            public SentinelHardwiredDescriptor()
            {
                ParameterDescriptor[] parameters =
                {
                    new ParameterDescriptor("value", typeof(int)),
                    new ParameterDescriptor(
                        "optional",
                        typeof(string),
                        hasDefaultValue: true,
                        defaultValue: DefaultValue.Instance
                    ),
                };

                Initialize("Sentinel", isStatic: true, parameters, isExtensionMethod: false);
            }

            protected override object Invoke(
                Script script,
                object obj,
                object[] pars,
                int argscount
            )
            {
                InvocationCount++;
                LastParameters = pars;
                LastArgCount = argscount;
                object second = pars[1] is DefaultValue ? "sentinel" : pars[1];
                return $"{second}:{pars[0]}:{argscount}";
            }
        }
    }
}
