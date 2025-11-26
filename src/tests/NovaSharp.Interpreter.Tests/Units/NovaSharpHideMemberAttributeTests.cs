namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class NovaSharpHideMemberAttributeTests
    {
        [Test]
        public void HiddenMembersAreNotExposedToScripts()
        {
            UserData.RegisterType<HiddenMembersSample>();
            Script script = new Script(CoreModules.PresetComplete);
            script.Globals["sample"] = UserData.Create(new HiddenMembersSample());

            DynValue visibleResult = script.DoString("return sample.VisibleMethod()");
            Assert.That(visibleResult.Number, Is.EqualTo(5));

            Assert.That(
                () => script.DoString("return sample.HiddenMethod()"),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void HiddenMembersPropagateThroughInheritance()
        {
            UserData.RegisterType<DerivedHiddenMembersSample>();
            Script script = new Script(CoreModules.PresetComplete);
            script.Globals["sample"] = UserData.Create(new DerivedHiddenMembersSample());

            DynValue visibleResult = script.DoString("return sample.Visible()");
            Assert.That(visibleResult.Number, Is.EqualTo(10));

            Assert.That(
                () => script.DoString("return sample.HiddenProperty"),
                Throws.TypeOf<ScriptRuntimeException>()
            );
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

            public int HiddenProperty
            {
                get { return _hiddenPropertyValue; }
            }
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
