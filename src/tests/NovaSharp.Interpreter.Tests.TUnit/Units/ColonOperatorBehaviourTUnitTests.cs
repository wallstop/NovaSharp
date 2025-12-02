namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Options;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class ColonOperatorBehaviourTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TreatAsDotKeepsMethodCallForCallbacks()
        {
            Script script = new();
            bool? observed = null;
            DataType? firstArgumentType = null;

            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                {
                    observed = args.IsMethodCall;
                    firstArgumentType = args.Count > 0 ? args[0].Type : null;
                    return DynValue.NewNumber(args.Count);
                }
            );

            Table target = new(script);
            target.Set("invoke", callback);
            script.Globals["target"] = DynValue.NewTable(target);
            script.Options.ColonOperatorClrCallbackBehaviour = ColonOperatorBehaviour.TreatAsDot;

            script.DoString("return target:invoke(123)");

            await Assert.That(observed).IsTrue().ConfigureAwait(false);
            await Assert.That(firstArgumentType).IsEqualTo(DataType.Table).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TreatAsColonDisablesMethodCallFlag()
        {
            Script script = new();
            bool? observed = null;
            DataType? firstArgumentType = null;

            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                {
                    observed = args.IsMethodCall;
                    firstArgumentType = args.Count > 0 ? args[0].Type : null;
                    return DynValue.NewNumber(args.Count);
                }
            );

            Table target = new(script);
            target.Set("invoke", callback);
            script.Globals["target"] = DynValue.NewTable(target);
            script.Options.ColonOperatorClrCallbackBehaviour = ColonOperatorBehaviour.TreatAsColon;

            script.DoString("return target:invoke(123)");

            await Assert.That(observed).IsFalse().ConfigureAwait(false);
            await Assert.That(firstArgumentType).IsEqualTo(DataType.Table).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TreatAsDotOnUserDataOnlyPreservesUserDataMethodCalls()
        {
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<Probe>(ensureUnregistered: true);
            registrationScope.RegisterType<Probe>();

            Script script = new();
            script.Options.ColonOperatorClrCallbackBehaviour =
                ColonOperatorBehaviour.TreatAsDotOnUserData;

            bool? tableCallFlag = null;

            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                {
                    tableCallFlag = args.IsMethodCall;
                    return DynValue.NewNumber(args.Count);
                }
            );

            Table tableTarget = new(script);
            tableTarget.Set("invoke", callback);
            script.Globals["tableTarget"] = DynValue.NewTable(tableTarget);

            script.DoString("return tableTarget:invoke(123)");
            await Assert.That(tableCallFlag).IsFalse().ConfigureAwait(false);

            Probe probe = new();
            script.Globals["userTarget"] = UserData.Create(probe);

            script.DoString("return userTarget:Invoke(456)");

            await Assert.That(probe.LastIsMethodCall).IsFalse().ConfigureAwait(false);
            await Assert
                .That(probe.LastFirstArgumentType)
                .IsEqualTo(DataType.Number)
                .ConfigureAwait(false);
        }

        private sealed class Probe
        {
            public bool? LastIsMethodCall { get; private set; }

            public DataType? LastFirstArgumentType { get; private set; }

            public DynValue Invoke(ScriptExecutionContext context, CallbackArguments args)
            {
                LastIsMethodCall = args.IsMethodCall;
                LastFirstArgumentType = args.Count > 0 ? args[0].Type : null;
                return DynValue.NewNumber(args.Count);
            }
        }
    }
}
