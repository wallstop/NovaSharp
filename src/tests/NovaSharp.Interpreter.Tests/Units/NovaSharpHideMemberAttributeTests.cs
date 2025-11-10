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
            public int VisibleMethod()
            {
                return 5;
            }

            public int HiddenMethod()
            {
                return 15;
            }
        }

        [NovaSharpHideMember("HiddenProperty")]
        private class BaseHiddenMembersSample
        {
            public int HiddenProperty
            {
                get { return 42; }
            }
        }

        private sealed class DerivedHiddenMembersSample : BaseHiddenMembersSample
        {
            public int Visible()
            {
                return 10;
            }
        }
    }
}
