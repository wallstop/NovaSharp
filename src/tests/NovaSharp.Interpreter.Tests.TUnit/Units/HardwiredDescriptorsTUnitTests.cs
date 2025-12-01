#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using CollectionAssert = NUnit.Framework.CollectionAssert;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NovaSharp.Interpreter.Modules;

    [UserDataIsolation]
    public sealed class HardwiredDescriptorsTUnitTests
    {
        private TestHardwiredDescriptor _descriptor;

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorSupportsReadAndWrite()
        {
            Script script = CreateScript();
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("obj.value = 123\nreturn obj.value");

            await Assert.That(result.Number).IsEqualTo(123);
            await Assert.That(host.Value).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorRejectsWriteWhenAccessDenied()
        {
            Script script = CreateScript();
            script.Globals["obj"] = UserData.Create(new TestHost());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("obj.readonly = 'x'")
            )!;

            await Assert.That(exception.Message).Contains("cannot be assigned");
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorThrowsWhenInstanceMissing()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                _descriptor.Value.GetValue(script, null)
            )!;

            await Assert.That(exception.Message).Contains("instance member value");
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorAppliesDefaultArguments()
        {
            Script script = CreateScript();
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("return obj:call(5)");

            await Assert.That(result.String).IsEqualTo("fallback");
            await Assert.That(host.Value).IsEqualTo(5);
            await Assert.That(_descriptor.Method.LastArgumentCount).IsEqualTo(2);
            CollectionAssert.AreEqual(new object[] { 5, "fallback" }, _descriptor.Method.LastParameters);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorUsesProvidedArguments()
        {
            Script script = CreateScript();
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("return obj:call(7, 'custom')");

            await Assert.That(result.String).IsEqualTo("custom");
            await Assert.That(host.Value).IsEqualTo(7);
            await Assert.That(_descriptor.Method.LastArgumentCount).IsEqualTo(2);
            CollectionAssert.AreEqual(new object[] { 7, "custom" }, _descriptor.Method.LastParameters);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorRejectsStaticInvocation()
        {
            Script script = CreateScript();
            script.Globals["TestHost"] = UserData.CreateStatic<TestHost>();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("TestHost:call(3)")
            )!;

            await Assert.That(exception.Message).Contains("call");
        }

        private Script CreateScript()
        {
            EnsureDescriptorRegistered();
            Script script = new(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private void EnsureDescriptorRegistered()
        {
            if (_descriptor == null || !UserData.IsTypeRegistered<TestHost>())
            {
                UserData.UnregisterType<TestHost>();
                _descriptor = new TestHardwiredDescriptor();
                UserData.RegisterType(_descriptor);
            }
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
#pragma warning restore CA2007
