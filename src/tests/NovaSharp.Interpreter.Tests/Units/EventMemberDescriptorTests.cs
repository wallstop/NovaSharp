namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
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

            Assert.That(
                () => EventMemberDescriptor.CheckEventIsCompatible(returning, true),
                Throws.ArgumentException.With.Message.Contains("return type")
            );
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersWithValueTypeParameters()
        {
            EventInfo valueParameter = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ValueParameter)
            );

            Assert.That(
                () => EventMemberDescriptor.CheckEventIsCompatible(valueParameter, true),
                Throws.ArgumentException.With.Message.Contains("value type parameters")
            );
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersWithByRefParameters()
        {
            EventInfo byRef = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.ByRefParameter)
            );

            Assert.That(
                () => EventMemberDescriptor.CheckEventIsCompatible(byRef, true),
                Throws.ArgumentException.With.Message.Contains("by-ref type parameters")
            );
        }

        [Test]
        public void CheckEventIsCompatibleRejectsHandlersExceedingMaxArguments()
        {
            EventInfo tooMany = typeof(IncompatibleEventSource).GetEvent(
                nameof(IncompatibleEventSource.TooManyArguments)
            );

            Assert.That(
                () => EventMemberDescriptor.CheckEventIsCompatible(tooMany, true),
                Throws.ArgumentException.With.Message.Contains(
                    $"{EventMemberDescriptor.MAX_ARGS_IN_DELEGATE}"
                )
            );
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
