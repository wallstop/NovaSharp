namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    [NonParallelizable]
    public sealed class HardwiredDescriptorsTests
    {
        private TestHardwiredDescriptor _descriptor = null!;

        [SetUp]
        public void SetUp()
        {
            UserData.UnregisterType(typeof(TestHost));
            _descriptor = new TestHardwiredDescriptor();
            UserData.RegisterType(_descriptor);
        }

        [TearDown]
        public void TearDown()
        {
            UserData.UnregisterType(typeof(TestHost));
        }

        [Test]
        public void HardwiredMemberDescriptorSupportsReadAndWrite()
        {
            Script script = CreateScript();
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("obj.value = 123\nreturn obj.value");

            Assert.Multiple(() =>
            {
                Assert.That(result.Number, Is.EqualTo(123));
                Assert.That(host.Value, Is.EqualTo(123));
            });
        }

        [Test]
        public void HardwiredMemberDescriptorRejectsWriteWhenAccessDenied()
        {
            Script script = CreateScript();
            script.Globals["obj"] = UserData.Create(new TestHost());

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("obj.readonly = 'x'")
            )!;

            Assert.That(ex.Message, Does.Contain("cannot be assigned"));
        }

        [Test]
        public void HardwiredMemberDescriptorThrowsWhenInstanceMissing()
        {
            Script script = CreateScript();

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                _descriptor.Value.GetValue(script, null)
            )!;

            Assert.That(ex.Message, Does.Contain("instance member value"));
        }

        [Test]
        public void HardwiredMethodDescriptorAppliesDefaultArguments()
        {
            Script script = CreateScript();
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("return obj:call(5)");

            Assert.Multiple(() =>
            {
                Assert.That(result.String, Is.EqualTo("fallback"));
                Assert.That(host.Value, Is.EqualTo(5));
                Assert.That(_descriptor.Method.LastArgumentCount, Is.EqualTo(2));
                Assert.That(
                    _descriptor.Method.LastParameters,
                    Is.EqualTo(new object[] { 5, "fallback" })
                );
            });
        }

        [Test]
        public void HardwiredMethodDescriptorUsesProvidedArguments()
        {
            Script script = CreateScript();
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("return obj:call(7, 'custom')");

            Assert.Multiple(() =>
            {
                Assert.That(result.String, Is.EqualTo("custom"));
                Assert.That(host.Value, Is.EqualTo(7));
                Assert.That(_descriptor.Method.LastArgumentCount, Is.EqualTo(2));
                Assert.That(
                    _descriptor.Method.LastParameters,
                    Is.EqualTo(new object[] { 7, "custom" })
                );
            });
        }

        [Test]
        public void HardwiredMethodDescriptorRejectsStaticInvocation()
        {
            Script script = CreateScript();
            script.Globals["TestHost"] = UserData.CreateStatic(typeof(TestHost));

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("TestHost:call(3)")
            )!;

            Assert.That(ex.Message, Does.Contain("call"));
        }

        private static Script CreateScript()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private sealed class TestHost
        {
            public int Value { get; set; }
        }

        private sealed class TestHardwiredDescriptor : HardwiredUserDataDescriptor
        {
            public TestHardwiredDescriptor()
                : base(typeof(TestHost))
            {
                Value = new ValuePropertyDescriptor();
                ReadOnly = new ReadOnlyDescriptor();
                Method = new CallMethodDescriptor();

                AddMember("value", Value);
                AddMember("readonly", ReadOnly);
                AddMember("call", Method);
            }

            public ValuePropertyDescriptor Value { get; }

            public ReadOnlyDescriptor ReadOnly { get; }

            public CallMethodDescriptor Method { get; }
        }

        private sealed class ValuePropertyDescriptor : HardwiredMemberDescriptor
        {
            public ValuePropertyDescriptor()
                : base(
                    typeof(int),
                    "value",
                    isStatic: false,
                    MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
                ) { }

            protected override object GetValueCore(Script script, object obj)
            {
                return ((TestHost)obj).Value;
            }

            protected override void SetValueCore(Script script, object obj, object value)
            {
                ((TestHost)obj).Value = (int)value;
            }
        }

        private sealed class ReadOnlyDescriptor : HardwiredMemberDescriptor
        {
            public ReadOnlyDescriptor()
                : base(typeof(string), "readonly", isStatic: false, MemberDescriptorAccess.CanRead)
            { }

            protected override object GetValueCore(Script script, object obj)
            {
                return "constant";
            }
        }

        private sealed class CallMethodDescriptor : HardwiredMethodMemberDescriptor
        {
            public CallMethodDescriptor()
            {
                Initialize(
                    "call",
                    isStatic: false,
                    new[]
                    {
                        new ParameterDescriptor("value", typeof(int)),
                        new ParameterDescriptor("extra", typeof(string), true, "fallback"),
                    },
                    isExtensionMethod: false
                );
            }

            public object[] LastParameters { get; private set; } = Array.Empty<object>();

            public int LastArgumentCount { get; private set; }

            protected override object Invoke(
                Script script,
                object obj,
                object[] pars,
                int argscount
            )
            {
                LastParameters = (object[])pars.Clone();
                LastArgumentCount = argscount;
                ((TestHost)obj).Value = (int)pars[0];
                return pars[1];
            }
        }
    }
}
