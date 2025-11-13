namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

#pragma warning disable 169 // unused private field

    [TestFixture]
    public class UserDataEventsTests
    {
        public class SomeClass
        {
            public event EventHandler OnMyEvent;
            public static event EventHandler OnMySEvent;

            public bool Trigger_MyEvent()
            {
                if (OnMyEvent != null)
                {
                    OnMyEvent(this, EventArgs.Empty);
                    return true;
                }
                return false;
            }

            public static bool Trigger_MySEvent()
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

            Script s = new(default);

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

            obj.Trigger_MyEvent();

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void InteropEventTwoObjects()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default);

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

            obj.Trigger_MyEvent();
            obj2.Trigger_MyEvent();

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void InteropEventMulti()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default);

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

            obj.Trigger_MyEvent();

            Assert.That(invocationCount, Is.EqualTo(2));
        }

        [Test]
        public void InteropEventMultiAndDetach()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default);

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
				myobj.Trigger_MyEvent();
				myobj.MyEvent.remove(handler);
				myobj.Trigger_MyEvent();
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

            Script s = new(default);

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
				myobj.Trigger_MyEvent();
				myobj.MyEvent.remove(handler);
				myobj.Trigger_MyEvent();
				myobj.MyEvent.remove(handler);
				"
            );

            Assert.That(obj.Trigger_MyEvent(), Is.False, "deregistration");
            Assert.That(invocationCount, Is.EqualTo(3));
        }

        [Test]
        public void InteropSEventDetachAndDeregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default)
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
				myobj.Trigger_MySEvent();
				myobj.MySEvent.remove(handler);
				myobj.Trigger_MySEvent();
				myobj.MySEvent.remove(handler);
				"
            );

            Assert.That(SomeClass.Trigger_MySEvent(), Is.False, "deregistration");
            Assert.That(invocationCount, Is.EqualTo(3));
        }

        [Test]
        public void InteropSEventDetachAndReregister()
        {
            int invocationCount = 0;
            UserData.RegisterType<SomeClass>();
            UserData.RegisterType<EventArgs>();

            Script s = new(default)
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
				myobj.Trigger_MySEvent();
				myobj.MySEvent.remove(handler);
				myobj.Trigger_MySEvent();
				myobj.MySEvent.add(handler);
				myobj.Trigger_MySEvent();
			"
            );

            Assert.That(invocationCount, Is.EqualTo(2));
            Assert.That(SomeClass.Trigger_MySEvent(), Is.True, "deregistration");
        }
    }
}
