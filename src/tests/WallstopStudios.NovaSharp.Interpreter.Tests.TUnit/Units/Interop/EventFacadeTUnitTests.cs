namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;

    public sealed class EventFacadeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task AddAndRemoveCallbacksInvokeUnderlyingHandlers()
        {
            TestEventTarget target = new();
            EventFacade facade = new(target.AddHandler, target.RemoveHandler, target);
            Script script = new();

            LuaValue add = facade.Index(script, LuaValue.NewString("add"), true).Value;
            LuaValue remove = facade.Index(script, LuaValue.NewString("remove"), true).Value;

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            LuaValue handler = LuaValue.NewCallback((_, _) => LuaValue.Nil);

            CallbackArguments args = TestHelpers.CreateArguments(handler);

            add.Callback.Invoke(context, args.GetArray(), args.IsMethodCall);
            remove.Callback.Invoke(context, args.GetArray(), args.IsMethodCall);

            await Assert.That(target.AddInvokeCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(target.RemoveInvokeCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(target.LastHandler).IsEqualTo(handler).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexThrowsWhenNameUnsupported()
        {
            EventFacade facade = new((_, _, _) => LuaValue.Nil, (_, _, _) => LuaValue.Nil, new());
            Script script = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                facade.Index(script, LuaValue.NewString("other"), true)
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Events only support add and remove methods")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexAlwaysThrows()
        {
            EventFacade facade = new((_, _, _) => LuaValue.Nil, (_, _, _) => LuaValue.Nil, new());
            Script script = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                facade.SetIndex(
                    script,
                    LuaValue.NewString("any"),
                    LuaValue.NewNumber(1),
                    isDirectIndexing: true
                )
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Events do not have settable fields")
                .ConfigureAwait(false);
        }

        private sealed class TestEventTarget
        {
            public int AddInvokeCount { get; private set; }
            public int RemoveInvokeCount { get; private set; }
            public LuaValue LastHandler { get; private set; }

            public LuaValue AddHandler(object _, ScriptExecutionContext __, CallbackArguments args)
            {
                AddInvokeCount++;
                LastHandler = args[0];
                return LuaValue.Nil;
            }

            public LuaValue RemoveHandler(
                object _,
                ScriptExecutionContext __,
                CallbackArguments args
            )
            {
                RemoveInvokeCount++;
                LastHandler = args[0];
                return LuaValue.Nil;
            }
        }
    }
}
