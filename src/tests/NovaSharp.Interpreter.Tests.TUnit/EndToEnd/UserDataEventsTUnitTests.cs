#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class UserDataEventsTUnitTests
    {
        internal sealed class SomeClass
        {
            public event EventHandler OnMyEvent;
            public static event EventHandler OnMySEvent;

            public bool TriggerMyEvent()
            {
                if (OnMyEvent != null)
                {
                    OnMyEvent(this, EventArgs.Empty);
                    return true;
                }

                return false;
            }

            public static bool TriggerMySEvent()
            {
                if (OnMySEvent != null)
                {
                    OnMySEvent(null, EventArgs.Empty);
                    return true;
                }

                return false;
            }
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventSimple()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules));

            SomeClass obj = new();
            script.Globals["myobj"] = obj;
            script.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MyEvent.add(handler);
                "
            );

            obj.TriggerMyEvent();

            await Assert.That(invocationCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventTwoObjects()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules));

            SomeClass obj = new();
            SomeClass obj2 = new();
            script.Globals["myobj"] = obj;
            script.Globals["myobj2"] = obj2;
            script.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MyEvent.add(handler);
                "
            );

            obj.TriggerMyEvent();
            obj2.TriggerMyEvent();

            await Assert.That(invocationCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventMulti()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules));

            SomeClass obj = new();
            script.Globals["myobj"] = obj;
            script.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MyEvent.add(handler);
                myobj.MyEvent.add(handler);
                "
            );

            obj.TriggerMyEvent();

            await Assert.That(invocationCount).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventMultiAndDetach()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules));

            SomeClass obj = new();
            script.Globals["myobj"] = obj;
            script.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MyEvent.add(handler);
                myobj.MyEvent.add(handler);
                myobj.TriggerMyEvent();
                myobj.MyEvent.remove(handler);
                myobj.TriggerMyEvent();
                "
            );

            await Assert.That(invocationCount).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventDetachAndDeregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules));

            SomeClass obj = new();
            script.Globals["myobj"] = obj;
            script.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MyEvent.add(handler);
                myobj.MyEvent.add(handler);
                myobj.TriggerMyEvent();
                myobj.MyEvent.remove(handler);
                myobj.TriggerMyEvent();
                myobj.MyEvent.remove(handler);
                "
            );

            await Assert.That(obj.TriggerMyEvent()).IsFalse();
            await Assert.That(invocationCount).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task InteropSEventDetachAndDeregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules))
            {
                Globals =
                {
                    ["myobj"] = typeof(SomeClass),
                    ["ext"] = DynValue.NewCallback(
                        (c, a) =>
                        {
                            invocationCount += 1;
                            return DynValue.Void;
                        }
                    ),
                },
            };

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MySEvent.add(handler);
                myobj.MySEvent.add(handler);
                myobj.TriggerMySEvent();
                myobj.MySEvent.remove(handler);
                myobj.TriggerMySEvent();
                myobj.MySEvent.remove(handler);
                "
            );

            await Assert.That(SomeClass.TriggerMySEvent()).IsFalse();
            await Assert.That(invocationCount).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task InteropSEventDetachAndReregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script script = new(default(CoreModules))
            {
                Globals =
                {
                    ["myobj"] = typeof(SomeClass),
                    ["ext"] = DynValue.NewCallback(
                        (c, a) =>
                        {
                            invocationCount += 1;
                            return DynValue.Void;
                        }
                    ),
                },
            };

            script.DoString(
                @"
                function handler(o, a)
                    ext();
                end

                myobj.MySEvent.add(handler);
                myobj.TriggerMySEvent();
                myobj.MySEvent.remove(handler);
                myobj.TriggerMySEvent();
                myobj.MySEvent.add(handler);
                myobj.TriggerMySEvent();
            "
            );

            await Assert.That(invocationCount).IsEqualTo(2);
            await Assert.That(SomeClass.TriggerMySEvent()).IsTrue();
        }
    }
}
#pragma warning restore CA2007
