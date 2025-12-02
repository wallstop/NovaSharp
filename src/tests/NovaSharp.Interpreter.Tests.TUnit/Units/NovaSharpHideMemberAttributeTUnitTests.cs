namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    [UserDataIsolation]
    public sealed class NovaSharpHideMemberAttributeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task HiddenMembersAreNotExposedToScripts()
        {
            UserData.RegisterType<HiddenMembersSample>();
            Script script = new Script(CoreModules.PresetComplete);
            script.Globals["sample"] = UserData.Create(new HiddenMembersSample());

            DynValue visibleResult = script.DoString("return sample.VisibleMethod()");
            await Assert.That(visibleResult.Number).IsEqualTo(5).ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return sample.HiddenMethod()")
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HiddenMembersPropagateThroughInheritance()
        {
            UserData.RegisterType<DerivedHiddenMembersSample>();
            Script script = new Script(CoreModules.PresetComplete);
            script.Globals["sample"] = UserData.Create(new DerivedHiddenMembersSample());

            DynValue visibleResult = script.DoString("return sample.Visible()");
            await Assert.That(visibleResult.Number).IsEqualTo(10).ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return sample.HiddenProperty")
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [NovaSharpHideMember("HiddenMethod")]
        private sealed class HiddenMembersSample
        {
            private readonly int _visibleValue;
            private readonly int _hiddenValue;

            public HiddenMembersSample()
            {
                _visibleValue = 5;
                _hiddenValue = 15;
            }

            public int VisibleMethod()
            {
                return _visibleValue;
            }

            public int HiddenMethod()
            {
                return _hiddenValue;
            }
        }

        [NovaSharpHideMember("HiddenProperty")]
        private class BaseHiddenMembersSample
        {
            private readonly int _hiddenPropertyValue = 42;

            public int HiddenProperty => _hiddenPropertyValue;
        }

        private sealed class DerivedHiddenMembersSample : BaseHiddenMembersSample
        {
            private readonly int _visibleValue;

            public DerivedHiddenMembersSample()
            {
                _visibleValue = 10;
            }

            public int Visible()
            {
                return _visibleValue;
            }
        }
    }
}
