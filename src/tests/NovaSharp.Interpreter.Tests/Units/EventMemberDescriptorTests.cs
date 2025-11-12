namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class EventMemberDescriptorTests
    {
        [Test]
        public void TryCreateIfVisibleReturnsDescriptorForPublicEvent()
        {
            EventInfo eventInfo = typeof(SampleEventSource).GetEvent(
                nameof(SampleEventSource.PublicEvent)
            );
            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                eventInfo,
                InteropAccessMode.Default
            );

            Assert.That(descriptor, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(descriptor.EventInfo, Is.EqualTo(eventInfo));
                Assert.That(descriptor.IsStatic, Is.False);
            });
        }

        [Test]
        public void RemoveCallbackWithoutExistingSubscriptionDoesNotUnregister()
        {
            SampleEventSource source = new();
            Script script = new Script();
            DynValue handler = script.DoString("return function() end");

            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler));

            Assert.Multiple(() =>
            {
                Assert.That(source.AddInvokeCount, Is.EqualTo(0));
                Assert.That(source.RemoveInvokeCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void GetValueReturnsFacadeGrantingAddRemove()
        {
            SampleEventSource source = new();
            Script script = new Script();
            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            DynValue facadeValue = descriptor.GetValue(script, source);
            Assert.That(facadeValue.UserData.Object, Is.InstanceOf<EventFacade>());
        }

        [Test]
        public void AddAndRemoveCallbacksManageDelegatesAndClosures()
        {
            const string HitsVariable = "hits";
            SampleEventSource source = new();
            Script script = new Script();
            script.DoString($"{HitsVariable} = 0");
            DynValue handler1 = script.DoString(
                $"return function(sender, arg) {HitsVariable} = {HitsVariable} + 1 end"
            );
            DynValue handler2 = script.DoString(
                $"return function(sender, arg) {HitsVariable} = {HitsVariable} + 10 end"
            );

            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler1));

            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler2));

            Assert.Multiple(() =>
            {
                Assert.That(
                    source.AddInvokeCount,
                    Is.EqualTo(1),
                    "First add should register delegate once"
                );
                Assert.That(source.RemoveInvokeCount, Is.EqualTo(0));
            });

            source.RaiseEvent(DynValue.NewString("payload"));
            double hits = script.Globals.Get(HitsVariable).Number;
            Assert.That(hits, Is.EqualTo(11));

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler1));

            Assert.That(
                source.RemoveInvokeCount,
                Is.EqualTo(0),
                "Delegate detached only when last handler removed"
            );

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler2));

            Assert.That(source.RemoveInvokeCount, Is.EqualTo(1));

            source.RaiseEvent(DynValue.NewString("payload2"));
            hits = script.Globals.Get(HitsVariable).Number;
            Assert.That(hits, Is.EqualTo(11), "No handlers remain after removal");
        }

        [Test]
        public void StaticEventsDispatchHandlersAndTrackSubscriptions()
        {
            const string HitsVariable = "staticHits";
            StaticSampleEventSource.Reset();
            Script script = new Script();
            script.DoString($"{HitsVariable} = 0");
            DynValue handler = script.DoString(
                $"return function(_, amount) {HitsVariable} = {HitsVariable} + amount end"
            );

            EventMemberDescriptor descriptor = new(
                typeof(StaticSampleEventSource).GetEvent(
                    nameof(StaticSampleEventSource.GlobalEvent)
                )
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.AddCallback(descriptor, context, TestHelpers.CreateArguments(handler));

            Assert.That(descriptor.IsStatic, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(StaticSampleEventSource.AddInvokeCount, Is.EqualTo(1));
                Assert.That(StaticSampleEventSource.RemoveInvokeCount, Is.EqualTo(0));
            });

            StaticSampleEventSource.Raise(DynValue.NewNumber(2));
            StaticSampleEventSource.Raise(DynValue.NewNumber(3));

            double hits = script.Globals.Get(HitsVariable).Number;
            Assert.That(hits, Is.EqualTo(5));

            descriptor.RemoveCallback(descriptor, context, TestHelpers.CreateArguments(handler));
            Assert.That(StaticSampleEventSource.RemoveInvokeCount, Is.EqualTo(1));

            StaticSampleEventSource.Raise(DynValue.NewNumber(10));
            hits = script.Globals.Get(HitsVariable).Number;
            Assert.That(hits, Is.EqualTo(5), "Handlers removed from static event");
            StaticSampleEventSource.Reset();
        }

        [Test]
        public void EventDescriptorExposesNameAndGuardsAssignments()
        {
            SampleEventSource source = new();
            Script script = new Script();
            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo(nameof(SampleEventSource.PublicEvent)));
                Assert.That(descriptor.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(
                    () =>
                        descriptor.SetValue(
                            script,
                            source,
                            DynValue.NewString("should fail assignment")
                        ),
                    Throws.TypeOf<ScriptRuntimeException>()
                        .With.Message.Contains("cannot be assigned")
                );
            });
        }

        [Test]
        public void RemovingSameCallbackTwiceDoesNotDetachDelegateAgain()
        {
            SampleEventSource source = new();
            Script script = new Script();
            DynValue handler = script.DoString("return function() end");

            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler));
            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler));

            Assert.That(source.RemoveInvokeCount, Is.EqualTo(1));

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler));

            Assert.That(source.RemoveInvokeCount, Is.EqualTo(1));
        }

        [Test]
        public void RemovingUnknownCallbackLeavesDelegateAttached()
        {
            const string HitsVariable = "unknownRemovalHits";
            SampleEventSource source = new();
            Script script = new Script();
            script.DoString($"{HitsVariable} = 0");
            DynValue registered = script.DoString(
                $"return function(_, amount) {HitsVariable} = {HitsVariable} + amount end"
            );
            DynValue unknown = script.DoString("return function() end");

            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(registered));

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(unknown));

            Assert.That(source.RemoveInvokeCount, Is.EqualTo(0));

            source.RaiseEvent(DynValue.NewNumber(2));
            double hits = script.Globals.Get(HitsVariable).Number;
            Assert.That(hits, Is.EqualTo(2));

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(registered));
            Assert.That(source.RemoveInvokeCount, Is.EqualTo(1));
        }

        [Test]
        public void AddingSameClosureTwiceDoesNotRegisterDuplicateDelegates()
        {
            SampleEventSource source = new();
            Script script = new Script();
            DynValue handler = script.DoString("return function() end");

            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler));
            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler));

            Assert.That(source.AddInvokeCount, Is.EqualTo(1));
        }

        [Test]
        public void TryCreateIfVisibleRejectsPrivateEvents()
        {
            EventInfo hiddenEvent = typeof(PrivateEventSource).GetEvent(
                "HiddenEvent",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                hiddenEvent,
                InteropAccessMode.Default
            );

            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void TryCreateIfVisibleRejectsIncompatibleEvents()
        {
            EventInfo valueTypeEvent = typeof(ValueTypeEventSource).GetEvent(
                nameof(ValueTypeEventSource.ValueTypeEvent)
            );

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                valueTypeEvent,
                InteropAccessMode.Default
            );

            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void CheckEventIsCompatibleRejectsValueTypeEvents()
        {
            EventInfo valueTypeEvent = typeof(ValueTypeEventSource).GetEvent(
                nameof(ValueTypeEventSource.ValueTypeEvent)
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    EventMemberDescriptor.CheckEventIsCompatible(valueTypeEvent, false),
                    Is.False
                );
                Assert.That(
                    () => EventMemberDescriptor.CheckEventIsCompatible(valueTypeEvent, true),
                    Throws.ArgumentException.With.Message.Contains("value types")
                );
            });
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersReturningValues()
        {
            EventInfo returning = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ReturnsValue)
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    EventMemberDescriptor.CheckEventIsCompatible(returning, false),
                    Is.False
                );
                Assert.That(
                    () => EventMemberDescriptor.CheckEventIsCompatible(returning, true),
                    Throws.ArgumentException.With.Message.Contains("return type")
                );
            });
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersWithValueTypeParameters()
        {
            EventInfo valueParameter = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ValueParameter)
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    EventMemberDescriptor.CheckEventIsCompatible(valueParameter, false),
                    Is.False
                );
                Assert.That(
                    () =>
                        EventMemberDescriptor.CheckEventIsCompatible(valueParameter, true),
                    Throws.ArgumentException.With.Message.Contains("value type parameters")
                );
            });
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersWithByRefParameters()
        {
            EventInfo byRef = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ByRefParameter)
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    EventMemberDescriptor.CheckEventIsCompatible(byRef, false),
                    Is.False
                );
                Assert.That(
                    () => EventMemberDescriptor.CheckEventIsCompatible(byRef, true),
                    Throws.ArgumentException.With.Message.Contains("by-ref type parameters")
                );
            });
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersExceedingMaxArguments()
        {
            EventInfo tooMany = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.TooManyArguments)
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    EventMemberDescriptor.CheckEventIsCompatible(tooMany, false),
                    Is.False
                );
                Assert.That(
                    () => EventMemberDescriptor.CheckEventIsCompatible(tooMany, true),
                    Throws.ArgumentException.With.Message.Contains(
                        $"{EventMemberDescriptor.MAX_ARGS_IN_DELEGATE}"
                    )
                );
            });
        }

        [Test]
        public void DispatchEventInvokesZeroArgumentHandlers()
        {
            MultiSignatureEventSource source = new();
            Script script = new Script();
            script.DoString("hits = 0");
            DynValue handler = script.DoString("return function() hits = hits + 1 end");

            EventMemberDescriptor descriptor = new(
                typeof(MultiSignatureEventSource).GetEvent(
                    nameof(MultiSignatureEventSource.ZeroArgs)
                )
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler));

            source.RaiseZeroArgs();
            source.RaiseZeroArgs();

            double hits = script.Globals.Get("hits").Number;
            Assert.That(hits, Is.EqualTo(2));
        }

        [Test]
        public void DispatchEventForwardsMultipleArguments()
        {
            MultiSignatureEventSource source = new();
            Script script = new Script();
            script.DoString("payload = nil");
            DynValue handler = script.DoString(
                "return function(a, b, c) payload = table.concat({a, b, c}, \":\") end"
            );

            EventMemberDescriptor descriptor = new(
                typeof(MultiSignatureEventSource).GetEvent(
                    nameof(MultiSignatureEventSource.ThreeArgs)
                )
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler));

            source.RaiseThreeArgs("one", "two", "three");

            Assert.That(script.Globals.Get("payload").String, Is.EqualTo("one:two:three"));
        }

        [Test]
        public void CreateDelegateHandlesWideRangeOfArgumentCounts()
        {
            MultiArityEventSource source = new();
            Script script = new Script();
            script.DoString("hits = {}");
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            foreach (ArityCase @case in MultiArityEventSource.Cases)
            {
                EventMemberDescriptor descriptor = new(
                    typeof(MultiArityEventSource).GetEvent(@case.EventName)
                );

                string handlerSource = $@"
local max = {MultiArityEventSource.MaxArity}
return function(...)
    local args = {{ ... }}
    local actual = 0
    for i = 1, max do
        if args[i] ~= nil then
            actual = i
        end
    end
    hits['{@case.Id}'] = {{ count = actual, args = args }}
end";

                DynValue handler = script.DoString(handlerSource);

                descriptor.AddCallback(source, context, TestHelpers.CreateArguments(handler));

                @case.Raise(source);

                DynValue entry = script.Globals.Get("hits").Table.Get(@case.Id);
                Assert.That(entry.Type, Is.EqualTo(DataType.Table));

                DynValue count = entry.Table.Get("count");
                Assert.That(count.Type, Is.EqualTo(DataType.Number));
                Assert.That(
                    count.Number,
                    Is.EqualTo(@case.Arity),
                    $"Arity mismatch for {@case.EventName}"
                );

                DynValue args = entry.Table.Get("args");
                Assert.That(args.Type, Is.EqualTo(DataType.Table));

                for (int i = 1; i <= @case.Arity; i++)
                {
                    DynValue argValue = args.Table.Get(i);
                    Assert.That(
                        argValue.String,
                        Is.EqualTo($"a{i}"),
                        $"Unexpected argument {i} for {@case.EventName}"
                    );
                }

                if (@case.Arity < MultiArityEventSource.MaxArity)
                {
                    DynValue next = args.Table.Get(@case.Arity + 1);
                    Assert.That(
                        next.IsNil(),
                        Is.True,
                        $"Trailing argument should be nil for {@case.EventName}"
                    );
                }

                descriptor.RemoveCallback(
                    source,
                    context,
                    TestHelpers.CreateArguments(handler)
                );
            }
        }

        private sealed class SampleEventSource
        {
            private event EventHandler<DynValue> _event;

            public int AddInvokeCount { get; private set; }
            public int RemoveInvokeCount { get; private set; }

            public event EventHandler<DynValue> PublicEvent
            {
                add
                {
                    AddInvokeCount++;
                    _event += value;
                }
                remove
                {
                    RemoveInvokeCount++;
                    _event -= value;
                }
            }

            public void RaiseEvent(DynValue arg)
            {
                _event?.Invoke(null, arg);
            }
        }

        private struct ValueTypeEventSource
        {
            public event Action ValueTypeEvent;
        }

        private sealed class PrivateEventSource
        {
            private event Action HiddenEvent;

            // ReSharper disable once UnusedMember.Local - invoked via reflection in tests.
            public void Raise()
            {
                HiddenEvent?.Invoke();
            }
        }

        private sealed class IncompatibleEventSource
        {
            public event Func<int> ReturnsValue;

            public event Action<int> ValueParameter;

            public event ByRefHandler ByRefParameter;

            public event TooManyArgumentsHandler TooManyArguments;
        }

        private delegate void ByRefHandler(ref string value);

        private delegate void TooManyArgumentsHandler(
            object a1,
            object a2,
            object a3,
            object a4,
            object a5,
            object a6,
            object a7,
            object a8,
            object a9,
            object a10,
            object a11,
            object a12,
            object a13,
            object a14,
            object a15,
            object a16,
            object a17
        );

        private sealed class MultiSignatureEventSource
        {
            public event Action ZeroArgs;

            public event Action<object, object, object> ThreeArgs;

            public void RaiseZeroArgs()
            {
                ZeroArgs?.Invoke();
            }

            public void RaiseThreeArgs(object a, object b, object c)
            {
                ThreeArgs?.Invoke(a, b, c);
            }
        }

        private readonly struct ArityCase
        {
            public ArityCase(string id, string eventName, int arity, Action<MultiArityEventSource> raise)
            {
                Id = id;
                EventName = eventName;
                Arity = arity;
                Raise = raise;
            }

            public string Id { get; }

            public string EventName { get; }

            public int Arity { get; }

            public Action<MultiArityEventSource> Raise { get; }
        }

        private sealed class MultiArityEventSource
        {
            public const int MaxArity = 16;

            public static readonly ArityCase[] Cases =
                new ArityCase[]
                {
                    new("one", nameof(OneArg), 1, s => s.RaiseOneArg()),
                    new("four", nameof(FourArgs), 4, s => s.RaiseFourArgs()),
                    new("five", nameof(FiveArgs), 5, s => s.RaiseFiveArgs()),
                    new("six", nameof(SixArgs), 6, s => s.RaiseSixArgs()),
                    new("seven", nameof(SevenArgs), 7, s => s.RaiseSevenArgs()),
                    new("eight", nameof(EightArgs), 8, s => s.RaiseEightArgs()),
                    new("nine", nameof(NineArgs), 9, s => s.RaiseNineArgs()),
                    new("ten", nameof(TenArgs), 10, s => s.RaiseTenArgs()),
                    new("eleven", nameof(ElevenArgs), 11, s => s.RaiseElevenArgs()),
                    new("twelve", nameof(TwelveArgs), 12, s => s.RaiseTwelveArgs()),
                    new("thirteen", nameof(ThirteenArgs), 13, s => s.RaiseThirteenArgs()),
                    new("fourteen", nameof(FourteenArgs), 14, s => s.RaiseFourteenArgs()),
                    new("fifteen", nameof(FifteenArgs), 15, s => s.RaiseFifteenArgs()),
                    new("sixteen", nameof(SixteenArgs), MaxArity, s => s.RaiseSixteenArgs()),
                };

            public event Action<object> OneArg;

            public event Action<object, object, object, object> FourArgs;

            public event Action<object, object, object, object, object> FiveArgs;

            public event Action<object, object, object, object, object, object> SixArgs;

            public event Action<object, object, object, object, object, object, object> SevenArgs;

            public event Action<object, object, object, object, object, object, object, object> EightArgs;

            public event Action<object, object, object, object, object, object, object, object, object> NineArgs;

            public event Action<object, object, object, object, object, object, object, object, object, object> TenArgs;

            public event ElevenArgsDelegate ElevenArgs;

            public event TwelveArgsDelegate TwelveArgs;

            public event ThirteenArgsDelegate ThirteenArgs;

            public event FourteenArgsDelegate FourteenArgs;

            public event FifteenArgsDelegate FifteenArgs;

            public event SixteenArgsDelegate SixteenArgs;

            public void RaiseOneArg()
            {
                OneArg?.Invoke("a1");
            }

            public void RaiseFourArgs()
            {
                FourArgs?.Invoke("a1", "a2", "a3", "a4");
            }

            public void RaiseFiveArgs()
            {
                FiveArgs?.Invoke("a1", "a2", "a3", "a4", "a5");
            }

            public void RaiseSixArgs()
            {
                SixArgs?.Invoke("a1", "a2", "a3", "a4", "a5", "a6");
            }

            public void RaiseSevenArgs()
            {
                SevenArgs?.Invoke("a1", "a2", "a3", "a4", "a5", "a6", "a7");
            }

            public void RaiseEightArgs()
            {
                EightArgs?.Invoke("a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8");
            }

            public void RaiseNineArgs()
            {
                NineArgs?.Invoke("a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9");
            }

            public void RaiseTenArgs()
            {
                TenArgs?.Invoke("a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10");
            }

            public void RaiseElevenArgs()
            {
                ElevenArgs?.Invoke(
                    "a1",
                    "a2",
                    "a3",
                    "a4",
                    "a5",
                    "a6",
                    "a7",
                    "a8",
                    "a9",
                    "a10",
                    "a11"
                );
            }

            public void RaiseTwelveArgs()
            {
                TwelveArgs?.Invoke(
                    "a1",
                    "a2",
                    "a3",
                    "a4",
                    "a5",
                    "a6",
                    "a7",
                    "a8",
                    "a9",
                    "a10",
                    "a11",
                    "a12"
                );
            }

            public void RaiseThirteenArgs()
            {
                ThirteenArgs?.Invoke(
                    "a1",
                    "a2",
                    "a3",
                    "a4",
                    "a5",
                    "a6",
                    "a7",
                    "a8",
                    "a9",
                    "a10",
                    "a11",
                    "a12",
                    "a13"
                );
            }

            public void RaiseFourteenArgs()
            {
                FourteenArgs?.Invoke(
                    "a1",
                    "a2",
                    "a3",
                    "a4",
                    "a5",
                    "a6",
                    "a7",
                    "a8",
                    "a9",
                    "a10",
                    "a11",
                    "a12",
                    "a13",
                    "a14"
                );
            }

            public void RaiseFifteenArgs()
            {
                FifteenArgs?.Invoke(
                    "a1",
                    "a2",
                    "a3",
                    "a4",
                    "a5",
                    "a6",
                    "a7",
                    "a8",
                    "a9",
                    "a10",
                    "a11",
                    "a12",
                    "a13",
                    "a14",
                    "a15"
                );
            }

            public void RaiseSixteenArgs()
            {
                SixteenArgs?.Invoke(
                    "a1",
                    "a2",
                    "a3",
                    "a4",
                    "a5",
                    "a6",
                    "a7",
                    "a8",
                    "a9",
                    "a10",
                    "a11",
                    "a12",
                    "a13",
                    "a14",
                    "a15",
                    "a16"
                );
            }

            public delegate void ElevenArgsDelegate(
                object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9,
                object o10,
                object o11
            );

            public delegate void TwelveArgsDelegate(
                object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9,
                object o10,
                object o11,
                object o12
            );

            public delegate void ThirteenArgsDelegate(
                object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9,
                object o10,
                object o11,
                object o12,
                object o13
            );

            public delegate void FourteenArgsDelegate(
                object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9,
                object o10,
                object o11,
                object o12,
                object o13,
                object o14
            );

            public delegate void FifteenArgsDelegate(
                object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9,
                object o10,
                object o11,
                object o12,
                object o13,
                object o14,
                object o15
            );

            public delegate void SixteenArgsDelegate(
                object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9,
                object o10,
                object o11,
                object o12,
                object o13,
                object o14,
                object o15,
                object o16
            );
        }

        private static class StaticSampleEventSource
        {
            private static event EventHandler<DynValue> _event;

            public static int AddInvokeCount { get; private set; }
            public static int RemoveInvokeCount { get; private set; }

            public static event EventHandler<DynValue> GlobalEvent
            {
                add
                {
                    AddInvokeCount++;
                    _event += value;
                }
                remove
                {
                    RemoveInvokeCount++;
                    _event -= value;
                }
            }

            public static void Raise(DynValue arg)
            {
                _event?.Invoke(null, arg);
            }

            public static void Reset()
            {
                _event = null;
                AddInvokeCount = 0;
                RemoveInvokeCount = 0;
            }
        }
    }
}
