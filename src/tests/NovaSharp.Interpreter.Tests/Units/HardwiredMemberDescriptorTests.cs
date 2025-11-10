namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwiredMemberDescriptorTests
    {
        [Test]
        public void ReadWriteDescriptorGetsAndSetsValues()
        {
            Script script = new Script(CoreModules.None);
            SampleTarget target = new SampleTarget();
            SampleReadWriteDescriptor descriptor = new SampleReadWriteDescriptor();

            descriptor.SetValue(script, target, DynValue.NewNumber(42));
            DynValue result = descriptor.GetValue(script, target);

            Assert.Multiple(() =>
            {
                Assert.That(target.Value, Is.EqualTo(42));
                Assert.That(result.Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void ReadOnlyDescriptorThrowsOnSet()
        {
            Script script = new Script(CoreModules.None);
            SampleTarget target = new SampleTarget();
            SampleReadOnlyDescriptor descriptor = new SampleReadOnlyDescriptor(10);

            DynValue value = descriptor.GetValue(script, target);
            Assert.That(value.Number, Is.EqualTo(10));

            Assert.That(
                () => descriptor.SetValue(script, target, DynValue.NewNumber(5)),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void AccessingInstanceMemberWithoutObjectThrows()
        {
            Script script = new Script(CoreModules.None);
            SampleReadWriteDescriptor descriptor = new SampleReadWriteDescriptor();

            Assert.That(
                () => descriptor.GetValue(script, null),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void HardwiredMethodDescriptorHonoursDefaultValues()
        {
            Script script = new Script(CoreModules.None);
            SampleTarget target = new SampleTarget { Value = 3 };
            SampleHardwiredMethodDescriptor descriptor = new SampleHardwiredMethodDescriptor();

            List<DynValue> arguments = new List<DynValue> { DynValue.NewNumber(5) };
            CallbackArguments callbackArguments = new CallbackArguments(
                arguments,
                isMethodCall: false
            );

            DynValue result = descriptor.Execute(script, target, null, callbackArguments);

            Assert.Multiple(() =>
            {
                Assert.That(result.Number, Is.EqualTo(3 + 5 + 10));
                Assert.That(descriptor.LastArgumentCount, Is.EqualTo(2));
            });
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

            protected override object GetValueImpl(Script script, object obj)
            {
                SampleTarget target = (SampleTarget)obj;
                return target.Value;
            }

            protected override void SetValueImpl(Script script, object obj, object value)
            {
                SampleTarget target = (SampleTarget)obj;
                target.Value = (int)value;
            }
        }

        private sealed class SampleReadOnlyDescriptor : HardwiredMemberDescriptor
        {
            private readonly int _value;

            public SampleReadOnlyDescriptor(int value)
                : base(typeof(int), "Constant", isStatic: false, MemberDescriptorAccess.CanRead)
            {
                _value = value;
            }

            protected override object GetValueImpl(Script script, object obj)
            {
                return _value;
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
