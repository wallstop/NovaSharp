#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class EventFacadeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task AddAndRemoveCallbacksInvokeUnderlyingHandlers()
        {
            TestEventTarget target = new();
            EventFacade facade = new(target.AddHandler, target.RemoveHandler, target);
            Script script = new();

            DynValue add = facade.Index(script, DynValue.NewString("add"), true);
            DynValue remove = facade.Index(script, DynValue.NewString("remove"), true);

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue handler = DynValue.NewCallback((_, _) => DynValue.Nil);

            CallbackArguments args = TestHelpers.CreateArguments(handler);

            add.Callback.Invoke(context, args.GetArray(), args.IsMethodCall);
            remove.Callback.Invoke(context, args.GetArray(), args.IsMethodCall);

            await Assert.That(target.AddInvokeCount).IsEqualTo(1);
            await Assert.That(target.RemoveInvokeCount).IsEqualTo(1);
            await Assert.That(target.LastHandler).IsEqualTo(handler);
        }

        [global::TUnit.Core.Test]
        public async Task IndexThrowsWhenNameUnsupported()
        {
            EventFacade facade = new((_, _, _) => DynValue.Nil, (_, _, _) => DynValue.Nil, new());
            Script script = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                facade.Index(script, DynValue.NewString("other"), true)
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Events only support add and remove methods");
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexAlwaysThrows()
        {
            EventFacade facade = new((_, _, _) => DynValue.Nil, (_, _, _) => DynValue.Nil, new());
            Script script = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                facade.SetIndex(
                    script,
                    DynValue.NewString("any"),
                    DynValue.NewNumber(1),
                    isDirectIndexing: true
                )
            )!;

            await Assert.That(exception.Message).Contains("Events do not have settable fields");
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
#pragma warning restore CA2007
