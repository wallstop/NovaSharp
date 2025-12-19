namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Descriptors
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;

    public sealed class EventMemberDescriptorTUnitTests
    {
        static EventMemberDescriptorTUnitTests()
        {
            _ = new PrivateEventSource();
            _ = new IncompatibleEventSource();
            _ = new VisibilityTestEventSource();
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleReturnsDescriptorForPublicEvent()
        {
            EventInfo eventInfo = typeof(SampleEventSource).GetEvent(
                nameof(SampleEventSource.PublicEvent)
            );
            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                eventInfo,
                InteropAccessMode.Default
            );

            await Assert.That(descriptor).IsNotNull().ConfigureAwait(false);
            await Assert.That(descriptor.EventInfo).IsEqualTo(eventInfo).ConfigureAwait(false);
            await Assert.That(descriptor.IsStatic).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveCallbackWithoutExistingSubscriptionDoesNotUnregister()
        {
            SampleEventSource source = new();
            Script script = new Script();
            DynValue handler = script.DoString("return function() end");

            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler));

            await Assert.That(source.AddInvokeCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(source.RemoveInvokeCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetValueReturnsFacadeGrantingAddRemove()
        {
            SampleEventSource source = new();
            Script script = new Script();
            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            DynValue facadeValue = descriptor.GetValue(script, source);
            await Assert
                .That(facadeValue.UserData.Object)
                .IsTypeOf<EventFacade>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AddAndRemoveCallbacksManageDelegatesAndClosures()
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

            await Assert
                .That(source.AddInvokeCount)
                .IsEqualTo(1)
                .Because("First add should register delegate once")
                .ConfigureAwait(false);
            await Assert.That(source.RemoveInvokeCount).IsEqualTo(0).ConfigureAwait(false);

            source.RaiseEvent(DynValue.NewString("payload"));
            double hits = script.Globals.Get(HitsVariable).Number;
            await Assert.That(hits).IsEqualTo(11).ConfigureAwait(false);

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler1));

            await Assert
                .That(source.RemoveInvokeCount)
                .IsEqualTo(0)
                .Because("Delegate detached only when last handler removed")
                .ConfigureAwait(false);

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler2));

            await Assert.That(source.RemoveInvokeCount).IsEqualTo(1).ConfigureAwait(false);

            source.RaiseEvent(DynValue.NewString("payload2"));
            hits = script.Globals.Get(HitsVariable).Number;
            await Assert
                .That(hits)
                .IsEqualTo(11)
                .Because("No handlers remain after removal")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StaticEventsDispatchHandlersAndTrackSubscriptions()
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

            await Assert.That(descriptor.IsStatic).IsTrue().ConfigureAwait(false);
            await Assert
                .That(StaticSampleEventSource.AddInvokeCount)
                .IsEqualTo(1)
                .ConfigureAwait(false);
            await Assert
                .That(StaticSampleEventSource.RemoveInvokeCount)
                .IsEqualTo(0)
                .ConfigureAwait(false);

            StaticSampleEventSource.Raise(DynValue.NewNumber(2));
            StaticSampleEventSource.Raise(DynValue.NewNumber(3));

            double hits = script.Globals.Get(HitsVariable).Number;
            await Assert.That(hits).IsEqualTo(5).ConfigureAwait(false);

            descriptor.RemoveCallback(descriptor, context, TestHelpers.CreateArguments(handler));
            await Assert
                .That(StaticSampleEventSource.RemoveInvokeCount)
                .IsEqualTo(1)
                .ConfigureAwait(false);

            StaticSampleEventSource.Raise(DynValue.NewNumber(10));
            hits = script.Globals.Get(HitsVariable).Number;
            await Assert
                .That(hits)
                .IsEqualTo(5)
                .Because("Handlers removed from static event")
                .ConfigureAwait(false);
            StaticSampleEventSource.Reset();
        }

        [global::TUnit.Core.Test]
        public async Task EventDescriptorExposesNameAndGuardsAssignments()
        {
            SampleEventSource source = new();
            Script script = new Script();
            EventMemberDescriptor descriptor = new(
                typeof(SampleEventSource).GetEvent(nameof(SampleEventSource.PublicEvent))
            );

            await Assert
                .That(descriptor.Name)
                .IsEqualTo(nameof(SampleEventSource.PublicEvent))
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead)
                .ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(script, source, DynValue.NewString("should fail assignment"))
            )!;
            await Assert
                .That(exception.Message)
                .Contains("cannot be assigned")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemovingSameCallbackTwiceDoesNotDetachDelegateAgain()
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

            await Assert.That(source.RemoveInvokeCount).IsEqualTo(1).ConfigureAwait(false);

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler));

            await Assert.That(source.RemoveInvokeCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemovingUnknownCallbackLeavesDelegateAttached()
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

            await Assert.That(source.RemoveInvokeCount).IsEqualTo(0).ConfigureAwait(false);

            source.RaiseEvent(DynValue.NewNumber(2));
            double hits = script.Globals.Get(HitsVariable).Number;
            await Assert.That(hits).IsEqualTo(2).ConfigureAwait(false);

            descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(registered));
            await Assert.That(source.RemoveInvokeCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AddingSameClosureTwiceDoesNotRegisterDuplicateDelegates()
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

            await Assert.That(source.AddInvokeCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleRejectsPrivateEvents()
        {
            EventInfo hiddenEvent = PrivateEventSourceMetadata.HiddenEvent;

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                hiddenEvent,
                InteropAccessMode.Default
            );

            await Assert.That(descriptor).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleRejectsIncompatibleEvents()
        {
            EventInfo valueTypeEvent = typeof(ValueTypeEventSource).GetEvent(
                nameof(ValueTypeEventSource.ValueTypeEvent)
            );

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                valueTypeEvent,
                InteropAccessMode.Default
            );

            await Assert.That(descriptor).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckEventIsCompatibleRejectsValueTypeEvents()
        {
            EventInfo valueTypeEvent = typeof(ValueTypeEventSource).GetEvent(
                nameof(ValueTypeEventSource.ValueTypeEvent)
            );

            await Assert
                .That(EventMemberDescriptor.CheckEventIsCompatible(valueTypeEvent, false))
                .IsFalse()
                .ConfigureAwait(false);
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                EventMemberDescriptor.CheckEventIsCompatible(valueTypeEvent, true)
            )!;
            await Assert.That(exception.Message).Contains("value types").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckEventIsCompatibleRejectsHandlersReturningValues()
        {
            EventInfo returning = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ReturnsValue)
            );

            await Assert
                .That(EventMemberDescriptor.CheckEventIsCompatible(returning, false))
                .IsFalse()
                .ConfigureAwait(false);
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                EventMemberDescriptor.CheckEventIsCompatible(returning, true)
            )!;
            await Assert.That(exception.Message).Contains("return type").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckEventIsCompatibleRejectsHandlersWithValueTypeParameters()
        {
            EventInfo valueParameter = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ValueParameter)
            );

            await Assert
                .That(EventMemberDescriptor.CheckEventIsCompatible(valueParameter, false))
                .IsFalse()
                .ConfigureAwait(false);
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                EventMemberDescriptor.CheckEventIsCompatible(valueParameter, true)
            )!;
            await Assert
                .That(exception.Message)
                .Contains("value type parameters")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckEventIsCompatibleRejectsHandlersWithByRefParameters()
        {
            EventInfo byRef = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ByRefParameter)
            );

            await Assert
                .That(EventMemberDescriptor.CheckEventIsCompatible(byRef, false))
                .IsFalse()
                .ConfigureAwait(false);
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                EventMemberDescriptor.CheckEventIsCompatible(byRef, true)
            )!;
            await Assert
                .That(exception.Message)
                .Contains("by-ref type parameters")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckEventIsCompatibleRejectsHandlersExceedingMaxArguments()
        {
            EventInfo tooMany = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.TooManyArguments)
            );

            await Assert
                .That(EventMemberDescriptor.CheckEventIsCompatible(tooMany, false))
                .IsFalse()
                .ConfigureAwait(false);
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                EventMemberDescriptor.CheckEventIsCompatible(tooMany, true)
            )!;
            await Assert
                .That(exception.Message)
                .Contains($"{EventMemberDescriptor.MaxArgsInDelegate}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleThrowsWhenEventInfoIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                EventMemberDescriptor.TryCreateIfVisible(null, InteropAccessMode.Default)
            )!;
            await Assert.That(exception.ParamName).IsEqualTo("ei").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckEventIsCompatibleThrowsWhenEventInfoIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                EventMemberDescriptor.CheckEventIsCompatible(null, throwException: true)
            )!;
            await Assert.That(exception.ParamName).IsEqualTo("ei").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DispatchEventInvokesZeroArgumentHandlers()
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
            await Assert.That(hits).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DispatchEventForwardsMultipleArguments()
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

            await Assert
                .That(script.Globals.Get("payload").String)
                .IsEqualTo("one:two:three")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateDelegateHandlesWideRangeOfArgumentCounts()
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

                string handlerSource =
                    $@"
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
                await Assert.That(entry.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);

                DynValue count = entry.Table.Get("count");
                await Assert.That(count.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
                await Assert
                    .That(count.Number)
                    .IsEqualTo(@case.Arity)
                    .Because($"Arity mismatch for {@case.EventName}")
                    .ConfigureAwait(false);

                DynValue args = entry.Table.Get("args");
                await Assert.That(args.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);

                for (int i = 1; i <= @case.Arity; i++)
                {
                    DynValue argValue = args.Table.Get(i);
                    await Assert
                        .That(argValue.String)
                        .IsEqualTo($"a{i}")
                        .Because($"Unexpected argument {i} for {@case.EventName}")
                        .ConfigureAwait(false);
                }

                if (@case.Arity < MultiArityEventSource.MaxArity)
                {
                    DynValue next = args.Table.Get(@case.Arity + 1);
                    await Assert
                        .That(next.IsNil())
                        .IsTrue()
                        .Because($"Trailing argument should be nil for {@case.EventName}")
                        .ConfigureAwait(false);
                }

                descriptor.RemoveCallback(source, context, TestHelpers.CreateArguments(handler));
            }
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleReturnsDescriptorWhenExplicitlyMarkedVisible()
        {
            EventInfo eventInfo = typeof(VisibilityTestEventSource).GetEvent(
                nameof(VisibilityTestEventSource.ExplicitlyVisibleEvent),
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                eventInfo,
                InteropAccessMode.Default
            );

            await Assert.That(descriptor).IsNotNull().ConfigureAwait(false);
            await Assert.That(descriptor.EventInfo).IsEqualTo(eventInfo).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleReturnsNullWhenExplicitlyMarkedHidden()
        {
            EventInfo eventInfo = typeof(VisibilityTestEventSource).GetEvent(
                nameof(VisibilityTestEventSource.ExplicitlyHiddenEvent)
            );

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                eventInfo,
                InteropAccessMode.Default
            );

            await Assert.That(descriptor).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleReturnsNullWhenPublicMethodsNotAvailable()
        {
            EventInfo eventInfo = typeof(VisibilityTestEventSource).GetEvent(
                nameof(VisibilityTestEventSource.NonPublicAccessorEvent),
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            EventMemberDescriptor descriptor = EventMemberDescriptor.TryCreateIfVisible(
                eventInfo,
                InteropAccessMode.Default
            );

            await Assert.That(descriptor).IsNull().ConfigureAwait(false);
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

            internal static EventInfo GetHiddenEventMetadata()
            {
                return typeof(PrivateEventSource)
                    .GetTypeInfo()
                    .DeclaredEvents.Single(e => e.Name == nameof(HiddenEvent));
            }
        }

        private static class PrivateEventSourceMetadata
        {
            internal static EventInfo HiddenEvent { get; } =
                PrivateEventSource.GetHiddenEventMetadata();
        }

        private sealed class IncompatibleEventSource
        {
            [SuppressMessage(
                "Design",
                "CA1003:Use generic EventHandler instances",
                Justification = "These invalid signatures are required to verify EventMemberDescriptor rejects non-EventHandler delegates."
            )]
            public event Func<int> ReturnsValue;

            [SuppressMessage(
                "Design",
                "CA1003:Use generic EventHandler instances",
                Justification = "These invalid signatures are required to verify EventMemberDescriptor rejects non-EventHandler delegates."
            )]
            public event Action<int> ValueParameter;

            [SuppressMessage(
                "Design",
                "CA1003:Use generic EventHandler instances",
                Justification = "These invalid signatures are required to verify EventMemberDescriptor rejects non-EventHandler delegates."
            )]
            public event ByRefHandler ByRefParameter;

            [SuppressMessage(
                "Design",
                "CA1003:Use generic EventHandler instances",
                Justification = "These invalid signatures are required to verify EventMemberDescriptor rejects non-EventHandler delegates."
            )]
            public event TooManyArgumentsHandler TooManyArguments;
        }

        public delegate void ByRefHandler(ref string value);

        public delegate void TooManyArgumentsHandler(
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
            public ArityCase(
                string id,
                string eventName,
                int arity,
                Action<MultiArityEventSource> raise
            )
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

            public static readonly ArityCase[] Cases = new ArityCase[]
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

            public event Action<
                object,
                object,
                object,
                object,
                object,
                object,
                object,
                object
            > EightArgs;

            public event Action<
                object,
                object,
                object,
                object,
                object,
                object,
                object,
                object,
                object
            > NineArgs;

            public event Action<
                object,
                object,
                object,
                object,
                object,
                object,
                object,
                object,
                object,
                object
            > TenArgs;

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

        private sealed class VisibilityTestEventSource
        {
            [NovaSharpVisible(true)]
            internal event Action ExplicitlyVisibleEvent;

            [NovaSharpVisible(false)]
            public event Action ExplicitlyHiddenEvent;

            internal event Action NonPublicAccessorEvent;

            public void RaiseExplicitlyVisibleEvent()
            {
                ExplicitlyVisibleEvent?.Invoke();
            }

            public void RaiseExplicitlyHiddenEvent()
            {
                ExplicitlyHiddenEvent?.Invoke();
            }

            public void RaiseNonPublicAccessorEvent()
            {
                NonPublicAccessorEvent?.Invoke();
            }
        }
    }
}
