namespace WallstopStudios.NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.RemoteDebugger;
    using WallstopStudios.NovaSharp.RemoteDebugger.Network;

    public sealed class RemoteDebuggerOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultInstancesAreEqual()
        {
            RemoteDebuggerOptions first = RemoteDebuggerOptions.Default;
            RemoteDebuggerOptions second = RemoteDebuggerOptions.Default;

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert.That(first == second).IsTrue().ConfigureAwait(false);
            await Assert.That(first != second).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualityConsidersAllFields()
        {
            RemoteDebuggerOptions left = new RemoteDebuggerOptions()
            {
                NetworkOptions = Utf8TcpServerOptions.LocalHostOnly,
                SingleScriptMode = true,
                HttpPort = 2800,
                RpcPortBase = 3100,
            };

            RemoteDebuggerOptions right = left;
            RemoteDebuggerOptions differentNetwork = left;
            differentNetwork.NetworkOptions = Utf8TcpServerOptions.SingleClientOnly;

            RemoteDebuggerOptions differentMode = left;
            differentMode.SingleScriptMode = false;

            RemoteDebuggerOptions differentHttpPort = left;
            differentHttpPort.HttpPort = 31000;

            RemoteDebuggerOptions differentRpcPort = left;
            differentRpcPort.RpcPortBase = 4000;

            await Assert.That(left.Equals(right)).IsTrue().ConfigureAwait(false);
            await Assert.That(left == right).IsTrue().ConfigureAwait(false);

            await Assert.That(left.Equals(differentNetwork)).IsFalse().ConfigureAwait(false);
            await Assert.That(left.Equals(differentMode)).IsFalse().ConfigureAwait(false);
            await Assert.That(left.Equals(differentHttpPort)).IsFalse().ConfigureAwait(false);
            await Assert.That(left.Equals(differentRpcPort)).IsFalse().ConfigureAwait(false);
            await Assert.That(left != differentNetwork).IsTrue().ConfigureAwait(false);
            await Assert.That(left != differentMode).IsTrue().ConfigureAwait(false);
            await Assert.That(left != differentHttpPort).IsTrue().ConfigureAwait(false);
            await Assert.That(left != differentRpcPort).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualOptionsProduceSameHashCode()
        {
            RemoteDebuggerOptions left = new RemoteDebuggerOptions()
            {
                NetworkOptions = Utf8TcpServerOptions.None,
                SingleScriptMode = false,
                HttpPort = null,
                RpcPortBase = 1234,
            };

            RemoteDebuggerOptions right = left;

            await Assert
                .That(left.GetHashCode())
                .IsEqualTo(right.GetHashCode())
                .ConfigureAwait(false);
        }
    }
}
