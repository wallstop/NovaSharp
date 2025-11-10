namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ColonOperatorBehaviourTests
    {
        [Test]
        public void TreatAsDotKeepsMethodCallForCallbacks()
        {
            Script script = new();
            bool? observed = null;
            DataType? firstArgumentType = null;

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
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

            Assert.Multiple(() =>
            {
                Assert.That(observed, Is.True);
                Assert.That(firstArgumentType, Is.EqualTo(DataType.Table));
            });
        }

        [Test]
        public void TreatAsColonDisablesMethodCallFlag()
        {
            Script script = new();
            bool? observed = null;
            DataType? firstArgumentType = null;

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
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

            Assert.Multiple(() =>
            {
                Assert.That(observed, Is.False);
                Assert.That(firstArgumentType, Is.EqualTo(DataType.Table));
            });
        }

        [Test]
        public void TreatAsDotOnUserDataOnlyPreservesUserDataMethodCalls()
        {
            UserData.RegisterType<Probe>();

            try
            {
                Script script = new();
                script.Options.ColonOperatorClrCallbackBehaviour =
                    ColonOperatorBehaviour.TreatAsDotOnUserData;

                bool? tableCallFlag = null;

                DynValue callback = DynValue.NewCallback(
                    (context, args) =>
                    {
                        tableCallFlag = args.IsMethodCall;
                        return DynValue.NewNumber(args.Count);
                    }
                );

                Table tableTarget = new(script);
                tableTarget.Set("invoke", callback);
                script.Globals["tableTarget"] = DynValue.NewTable(tableTarget);

                script.DoString("return tableTarget:invoke(123)");

                Assert.That(tableCallFlag, Is.False);

                Probe probe = new();
                script.Globals["userTarget"] = UserData.Create(probe);

                script.DoString("return userTarget:Invoke(456)");

                Assert.Multiple(() =>
                {
                    Assert.That(probe.LastIsMethodCall, Is.False);
                    Assert.That(probe.LastFirstArgumentType, Is.EqualTo(DataType.Number));
                });
            }
            finally
            {
                UserData.UnregisterType<Probe>();
            }
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
