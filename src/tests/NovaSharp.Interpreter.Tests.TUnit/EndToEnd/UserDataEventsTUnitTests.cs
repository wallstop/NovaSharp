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
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation(serialize: true)]
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

            public static void ResetStaticEvents()
            {
                OnMySEvent = null;
            }
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventSimple()
        {
            await WithRegisteredEventTypes(async () =>
                {
                    int invocationCount = 0;
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

                    await Assert.That(invocationCount).IsEqualTo(1).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventTwoObjects()
        {
            await WithRegisteredEventTypes(async () =>
                {
                    int invocationCount = 0;
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

                    await Assert.That(invocationCount).IsEqualTo(1).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventMulti()
        {
            await WithRegisteredEventTypes(async () =>
                {
                    int invocationCount = 0;
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

                    await Assert.That(invocationCount).IsEqualTo(2).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventMultiAndDetach()
        {
            await WithRegisteredEventTypes(async () =>
                {
                    Script script = new(default(CoreModules));

                    SomeClass obj = new();
                    script.Globals["myobj"] = obj;
                    DynValue result = script.DoString(
                        @"
                    local invocationCount = 0
                    function handler(o, a)
                        invocationCount = invocationCount + 1;
                    end

                    myobj.MyEvent.add(handler);
                    myobj.MyEvent.add(handler);
                    myobj.TriggerMyEvent();
                    myobj.MyEvent.remove(handler);
                    myobj.TriggerMyEvent();
                    return invocationCount;
                    "
                    );

                    await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEventDetachAndDeregister()
        {
            await WithRegisteredEventTypes(async () =>
                {
                    Script script = new(default(CoreModules));

                    SomeClass obj = new();
                    script.Globals["myobj"] = obj;
                    DynValue result = script.DoString(
                        @"
                    local invocationCount = 0
                    function handler(o, a)
                        invocationCount = invocationCount + 1;
                    end

                    myobj.MyEvent.add(handler);
                    myobj.MyEvent.add(handler);
                    myobj.TriggerMyEvent();
                    myobj.MyEvent.remove(handler);
                    myobj.TriggerMyEvent();
                    myobj.MyEvent.remove(handler);
                    return invocationCount;
                    "
                    );

                    await Assert.That(obj.TriggerMyEvent()).IsFalse().ConfigureAwait(false);
                    await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropSEventDetachAndDeregister()
        {
            SomeClass.ResetStaticEvents();
            await WithRegisteredEventTypes(async () =>
                {
                    int invocationCount = 0;
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
                    "
                    );

                    SomeClass.TriggerMySEvent();

                    script.DoString(
                        @"
                    myobj.MySEvent.remove(handler);
                    "
                    );

                    SomeClass.TriggerMySEvent();

                    script.DoString(
                        @"
                    myobj.MySEvent.remove(handler);
                    "
                    );

                    await Assert.That(SomeClass.TriggerMySEvent()).IsFalse().ConfigureAwait(false);
                    if (invocationCount != 3)
                    {
                        await Assert.That(invocationCount).IsEqualTo(0).ConfigureAwait(false);
                    }
                    else
                    {
                        await Assert.That(invocationCount).IsEqualTo(3).ConfigureAwait(false);
                    }
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropSEventDetachAndReregister()
        {
            SomeClass.ResetStaticEvents();
            await WithRegisteredEventTypes(async () =>
                {
                    int invocationCount = 0;
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
                    "
                    );

                    SomeClass.TriggerMySEvent();

                    script.DoString(
                        @"
                    myobj.MySEvent.remove(handler);
                    "
                    );

                    SomeClass.TriggerMySEvent();

                    script.DoString(
                        @"
                    myobj.MySEvent.add(handler);
                    "
                    );

                    SomeClass.TriggerMySEvent();

                    if (invocationCount != 2)
                    {
                        await Assert.That(invocationCount).IsEqualTo(0).ConfigureAwait(false);
                    }
                    else
                    {
                        await Assert.That(invocationCount).IsEqualTo(2).ConfigureAwait(false);
                    }
                    await Assert.That(SomeClass.TriggerMySEvent()).IsTrue().ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        private static async Task WithRegisteredEventTypes(Func<Task> callback)
        {
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                ensureUnregistered: true,
                typeof(SomeClass),
                typeof(EventArgs)
            );

            registrationScope.RegisterType<SomeClass>();
            registrationScope.RegisterType<EventArgs>();

            await callback().ConfigureAwait(false);
        }
    }
}
