namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class UserDataEventsTests
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

        [Test]
        public void InteropEventSimple()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules));

            SomeClass obj = new();
            s.Globals["myobj"] = obj;
            s.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            s.DoString(
                @"
				function handler(o, a)
					ext();
				end

				myobj.MyEvent.add(handler);
				"
            );

            obj.TriggerMyEvent();

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void InteropEventTwoObjects()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules));

            SomeClass obj = new();
            SomeClass obj2 = new();
            s.Globals["myobj"] = obj;
            s.Globals["myobj2"] = obj2;
            s.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            s.DoString(
                @"
				function handler(o, a)
					ext();
				end

				myobj.MyEvent.add(handler);
				"
            );

            obj.TriggerMyEvent();
            obj2.TriggerMyEvent();

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void InteropEventMulti()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules));

            SomeClass obj = new();
            s.Globals["myobj"] = obj;
            s.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            s.DoString(
                @"
				function handler(o, a)
					ext();
				end

				myobj.MyEvent.add(handler);
				myobj.MyEvent.add(handler);
				"
            );

            obj.TriggerMyEvent();

            Assert.That(invocationCount, Is.EqualTo(2));
        }

        [Test]
        public void InteropEventMultiAndDetach()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules));

            SomeClass obj = new();
            s.Globals["myobj"] = obj;
            s.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            s.DoString(
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

            Assert.That(invocationCount, Is.EqualTo(3));
        }

        [Test]
        public void InteropEventDetachAndDeregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules));

            SomeClass obj = new();
            s.Globals["myobj"] = obj;
            s.Globals["ext"] = DynValue.NewCallback(
                (c, a) =>
                {
                    invocationCount += 1;
                    return DynValue.Void;
                }
            );

            s.DoString(
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

            Assert.That(obj.TriggerMyEvent(), Is.False, "deregistration");
            Assert.That(invocationCount, Is.EqualTo(3));
        }

        [Test]
        public void InteropSEventDetachAndDeregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules))
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

            s.DoString(
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

            Assert.That(SomeClass.TriggerMySEvent(), Is.False, "deregistration");
            Assert.That(invocationCount, Is.EqualTo(3));
        }

        [Test]
        public void InteropSEventDetachAndReregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default(CoreModules))
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

            s.DoString(
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

            Assert.That(invocationCount, Is.EqualTo(2));
            Assert.That(SomeClass.TriggerMySEvent(), Is.True, "deregistration");
        }
    }
}
