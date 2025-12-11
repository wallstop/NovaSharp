namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;

    public sealed class HardwiredMemberDescriptorTUnitTests
    {
        private const CoreModules MinimalScriptModules = CoreModules.GlobalConsts;

        [global::TUnit.Core.Test]
        public async Task ReadWriteDescriptorGetsAndSetsValues()
        {
            Script script = new(MinimalScriptModules);
            SampleTarget target = new();
            SampleReadWriteDescriptor descriptor = new();

            descriptor.SetValue(script, target, DynValue.NewNumber(42));
            DynValue result = descriptor.GetValue(script, target);

            await Assert.That(target.Value).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ReadOnlyDescriptorThrowsOnSet()
        {
            Script script = new(MinimalScriptModules);
            SampleTarget target = new();
            SampleReadOnlyDescriptor descriptor = new(10);

            DynValue value = descriptor.GetValue(script, target);
            await Assert.That(value.Number).IsEqualTo(10d).ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(script, target, DynValue.NewNumber(5))
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task WriteOnlyDescriptorThrowsOnGet()
        {
            Script script = new(MinimalScriptModules);
            SampleTarget target = new();
            SampleWriteOnlyDescriptor descriptor = new();

            descriptor.SetValue(script, target, DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.GetValue(script, target)
            );
            await Assert.That(exception.Message).Contains("writeOnly").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AccessingInstanceMemberWithoutObjectThrows()
        {
            Script script = new(MinimalScriptModules);
            SampleReadWriteDescriptor descriptor = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.GetValue(script, null)
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetValueThrowsWhenDynValueNull()
        {
            Script script = new(MinimalScriptModules);
            SampleTarget target = new();
            SampleReadWriteDescriptor descriptor = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                descriptor.SetValue(script, target, value: null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorHonoursDefaultValues()
        {
            Script script = new(MinimalScriptModules);
            SampleTarget target = new() { Value = 3 };
            SampleHardwiredMethodDescriptor descriptor = new();

            List<DynValue> arguments = new() { DynValue.NewNumber(5) };
            CallbackArguments callbackArguments = new(arguments, isMethodCall: false);

            DynValue result = descriptor.Execute(script, target, context: null, callbackArguments);

            await Assert.That(result.Number).IsEqualTo(18d).ConfigureAwait(false);
            await Assert.That(descriptor.LastArgumentCount).IsEqualTo(2).ConfigureAwait(false);
        }

        private sealed class SampleTarget
        {
            public int Value { get; set; }
        }

        private sealed class SampleReadWriteDescriptor : HardwiredMemberDescriptor
        {
            public SampleReadWriteDescriptor()
                : base(
                    typeof(int),
                    "Value",
                    isStatic: false,
                    MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
                ) { }

            protected override object GetValueCore(Script script, object obj)
            {
                SampleTarget target = (SampleTarget)obj;
                return target.Value;
            }

            protected override void SetValueCore(Script script, object obj, object value)
            {
                SampleTarget target = (SampleTarget)obj;
                target.Value = (int)value;
            }
        }

        private sealed class SampleReadOnlyDescriptor : HardwiredMemberDescriptor
        {
            private readonly int _storedValue;

            public SampleReadOnlyDescriptor(int value)
                : base(typeof(int), "Constant", isStatic: false, MemberDescriptorAccess.CanRead)
            {
                _storedValue = value;
            }

            protected override object GetValueCore(Script script, object obj)
            {
                return _storedValue;
            }
        }

        private sealed class SampleWriteOnlyDescriptor : HardwiredMemberDescriptor
        {
            public SampleWriteOnlyDescriptor()
                : base(typeof(int), "writeOnly", isStatic: false, MemberDescriptorAccess.CanWrite)
            { }

            protected override void SetValueCore(Script script, object obj, object value)
            {
                SampleTarget target = (SampleTarget)obj;
                target.Value = (int)value;
            }
        }

        private sealed class SampleHardwiredMethodDescriptor : HardwiredMethodMemberDescriptor
        {
            public SampleHardwiredMethodDescriptor()
            {
                Initialize(
                    "AddWithTarget",
                    isStatic: false,
                    new[]
                    {
                        new ParameterDescriptor("value", typeof(int)),
                        new ParameterDescriptor(
                            "optional",
                            typeof(int),
                            hasDefaultValue: true,
                            defaultValue: 10
                        ),
                    },
                    isExtensionMethod: false
                );
            }

            public int LastArgumentCount { get; private set; }

            protected override object Invoke(
                Script script,
                object obj,
                object[] pars,
                int argscount
            )
            {
                SampleTarget target = (SampleTarget)obj;
                LastArgumentCount = argscount;
                int left = (int)pars[0];
                int right = pars.Length > 1 ? (int)pars[1] : 0;
                return target.Value + left + right;
            }
        }
    }
}
