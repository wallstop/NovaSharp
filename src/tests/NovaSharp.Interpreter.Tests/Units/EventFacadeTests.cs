namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class EventFacadeTests
    {
        [Test]
        public void AddAndRemoveCallbacksInvokeUnderlyingHandlers()
        {
            TestEventTarget target = new();
            EventFacade facade = new(target.AddHandler, target.RemoveHandler, target);
            Script script = new Script();

            DynValue add = facade.Index(script, DynValue.NewString("add"), true);
            DynValue remove = facade.Index(script, DynValue.NewString("remove"), true);

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue handler = DynValue.NewCallback((_, _) => DynValue.Nil);

            add.Callback.Invoke(context, TestHelpers.CreateArguments(handler));
            remove.Callback.Invoke(context, TestHelpers.CreateArguments(handler));

            Assert.Multiple(() =>
            {
                Assert.That(target.AddInvokeCount, Is.EqualTo(1));
                Assert.That(target.RemoveInvokeCount, Is.EqualTo(1));
                Assert.That(target.LastHandler, Is.SameAs(handler));
            });
        }

        [Test]
        public void IndexThrowsWhenNameUnsupported()
        {
            EventFacade facade = new((o, _, _) => DynValue.Nil, (o, _, _) => DynValue.Nil, new());
            Script script = new Script();

            Assert.That(
                () => facade.Index(script, DynValue.NewString("other"), true),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.ScriptRuntimeException>()
                    .With.Message.Contains("Events only support add and remove methods")
            );
        }

        [Test]
        public void SetIndexAlwaysThrows()
        {
            EventFacade facade = new((o, _, _) => DynValue.Nil, (o, _, _) => DynValue.Nil, new());
            Script script = new Script();

            Assert.That(
                () =>
                    facade.SetIndex(
                        script,
                        DynValue.NewString("any"),
                        DynValue.NewNumber(1),
                        isDirectIndexing: true
                    ),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.ScriptRuntimeException>()
                    .With.Message.Contains("Events do not have settable fields")
            );
        }

        private sealed class TestEventTarget
        {
            public int AddInvokeCount { get; private set; }
            public int RemoveInvokeCount { get; private set; }
            public DynValue LastHandler { get; private set; }

            public DynValue AddHandler(object _, ScriptExecutionContext __, CallbackArguments args)
            {
                AddInvokeCount++;
                LastHandler = args[0];
                return DynValue.Nil;
            }

            public DynValue RemoveHandler(
                object _,
                ScriptExecutionContext __,
                CallbackArguments args
            )
            {
                RemoveInvokeCount++;
                LastHandler = args[0];
                return DynValue.Nil;
            }
        }
    }
}
