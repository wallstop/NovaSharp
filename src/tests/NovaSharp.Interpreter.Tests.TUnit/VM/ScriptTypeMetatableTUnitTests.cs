namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class ScriptTypeMetatableTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetTypeMetatableReturnsNullForUnsupportedTypes()
        {
            Script script = new();

            Table metatable = script.GetTypeMetatable((DataType)999);

            await Assert.That(metatable).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task SetTypeMetatableThrowsForUnsupportedTypes()
        {
            Script script = new();
            Table table = new(script);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.SetTypeMetatable((DataType)(-1), table)
            );

            await Assert.That(exception.Message).Contains("Specified type not supported");
        }

        [global::TUnit.Core.Test]
        public void WarmUpInitializesParser()
        {
            Script.WarmUp();
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
