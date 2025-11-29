namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.RemoteDebugger.Network;

    public sealed class RemoteDebuggerOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultInstancesAreEqual()
        {
            RemoteDebuggerOptions first = RemoteDebuggerOptions.Default;
            RemoteDebuggerOptions second = RemoteDebuggerOptions.Default;

            await Assert.That(first).IsEqualTo(second);
            await Assert.That(first == second).IsTrue();
            await Assert.That(first != second).IsFalse();
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

            await Assert.That(left.Equals(right)).IsTrue();
            await Assert.That(left == right).IsTrue();

            await Assert.That(left.Equals(differentNetwork)).IsFalse();
            await Assert.That(left.Equals(differentMode)).IsFalse();
            await Assert.That(left.Equals(differentHttpPort)).IsFalse();
            await Assert.That(left.Equals(differentRpcPort)).IsFalse();
            await Assert.That(left != differentNetwork).IsTrue();
            await Assert.That(left != differentMode).IsTrue();
            await Assert.That(left != differentHttpPort).IsTrue();
            await Assert.That(left != differentRpcPort).IsTrue();
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

            await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        }
    }
}
