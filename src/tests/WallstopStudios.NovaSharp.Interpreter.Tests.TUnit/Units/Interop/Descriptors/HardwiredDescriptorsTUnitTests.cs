namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class HardwiredDescriptorsTUnitTests
    {
        private TestHardwiredDescriptor _descriptor;

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorSupportsReadAndWrite()
        {
            using UserDataRegistrationScope registrationScope = CreateScript(out Script script);
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("obj.value = 123\nreturn obj.value");

            await Assert.That(result.Number).IsEqualTo(123).ConfigureAwait(false);
            await Assert.That(host.Value).IsEqualTo(123).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorRejectsWriteWhenAccessDenied()
        {
            using UserDataRegistrationScope registrationScope = CreateScript(out Script script);
            script.Globals["obj"] = UserData.Create(new TestHost());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("obj.readonly = 'x'")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("cannot be assigned")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorThrowsWhenInstanceMissing()
        {
            using UserDataRegistrationScope registrationScope = CreateScript(out Script script);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                _descriptor.Value.GetValue(script, null)
            )!;

            await Assert
                .That(exception.Message)
                .Contains("instance member value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorAppliesDefaultArguments()
        {
            using UserDataRegistrationScope registrationScope = CreateScript(out Script script);
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("return obj:call(5)");

            await Assert.That(result.String).IsEqualTo("fallback").ConfigureAwait(false);
            await Assert.That(host.Value).IsEqualTo(5).ConfigureAwait(false);
            await Assert
                .That(_descriptor.Method.LastArgumentCount)
                .IsEqualTo(2)
                .ConfigureAwait(false);
            await Assert
                .That(_descriptor.Method.LastParameters)
                .IsEquivalentTo(new object[] { 5, "fallback" })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorUsesProvidedArguments()
        {
            using UserDataRegistrationScope registrationScope = CreateScript(out Script script);
            TestHost host = new();
            script.Globals["obj"] = UserData.Create(host);

            DynValue result = script.DoString("return obj:call(7, 'custom')");

            await Assert.That(result.String).IsEqualTo("custom").ConfigureAwait(false);
            await Assert.That(host.Value).IsEqualTo(7).ConfigureAwait(false);
            await Assert
                .That(_descriptor.Method.LastArgumentCount)
                .IsEqualTo(2)
                .ConfigureAwait(false);
            await Assert
                .That(_descriptor.Method.LastParameters)
                .IsEquivalentTo(new object[] { 7, "custom" })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorRejectsStaticInvocation()
        {
            using UserDataRegistrationScope registrationScope = CreateScript(out Script script);
            script.Globals["TestHost"] = UserData.CreateStatic<TestHost>();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("TestHost:call(3)")
            )!;

            await Assert.That(exception.Message).Contains("call").ConfigureAwait(false);
        }

        private UserDataRegistrationScope CreateScript(out Script script)
        {
            UserDataRegistrationScope scope = UserDataRegistrationScope.Track<TestHost>(
                ensureUnregistered: true
            );
            _descriptor = new TestHardwiredDescriptor();
            scope.RegisterType<TestHost>(_descriptor);

            script = new(CoreModulePresets.Complete);
            script.Options.DebugPrint = _ => { };
            return scope;
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
