namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class HardwiredMethodMemberDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteUsesDefaultValueWhenArgumentMissing()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(7));
            SampleHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            await Assert.That(descriptor.InvocationCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(descriptor.LastArgCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(descriptor.LastParameters[0]).IsEqualTo(7).ConfigureAwait(false);
            await Assert
                .That(descriptor.LastParameters[1])
                .IsEqualTo("fallback")
                .ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("7:fallback:2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecutePassesExplicitArgumentsAndReturnsDynValue()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(2),
                DynValue.NewString("overridden")
            );
            SampleHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            await Assert.That(descriptor.InvocationCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(descriptor.LastArgCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(descriptor.LastParameters[1])
                .IsEqualTo("overridden")
                .ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("2:overridden:2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteUsesDefaultValuePlaceholderToReduceArgumentCount()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(42));
            SentinelHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            await Assert.That(descriptor.InvocationCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(descriptor.LastArgCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(descriptor.LastParameters[1])
                .IsTypeOf<DefaultValue>()
                .ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("sentinel:42:1").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCountsExplicitOptionalArgumentWhenPlaceholderProvided()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(10),
                DynValue.NewString("custom")
            );
            SentinelHardwiredDescriptor descriptor = new();

            DynValue result = descriptor.Execute(script, null, context, args);

            await Assert.That(descriptor.InvocationCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(descriptor.LastArgCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(descriptor.LastParameters[1])
                .IsEqualTo("custom")
                .ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("custom:10:2").ConfigureAwait(false);
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
