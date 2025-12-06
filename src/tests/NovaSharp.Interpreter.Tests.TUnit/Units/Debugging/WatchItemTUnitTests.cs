namespace NovaSharp.Interpreter.Tests.TUnit.Units.Debugging
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;

    public sealed class WatchItemTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ToStringIncludesAddressNameValueAndSymbol()
        {
            WatchItem item = new()
            {
                Address = 1,
                BasePtr = 2,
                RetAddress = 3,
                Name = "counter",
                Value = DynValue.NewNumber(42),
                LValue = SymbolRef.Global("counter", SymbolRef.DefaultEnv),
            };

            string formatted = item.ToString();

            await Assert
                .That(formatted)
                .IsEqualTo("1:2:3:counter:42:counter : Global / (default _ENV)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringHandlesNullMembers()
        {
            WatchItem item = new()
            {
                Address = 0,
                BasePtr = 0,
                RetAddress = 0,
                Name = null,
                Value = null,
                LValue = null,
            };

            await Assert
                .That(item.ToString())
                .IsEqualTo("0:0:0:(null):(null):(null)")
                .ConfigureAwait(false);
        }
    }
}
