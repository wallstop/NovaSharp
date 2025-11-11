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
