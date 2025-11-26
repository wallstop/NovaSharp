namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.RemoteDebugger;
    using NovaSharp.RemoteDebugger.Network;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RemoteDebuggerOptionsTests
    {
        [Test]
        public void DefaultInstancesAreEqual()
        {
            RemoteDebuggerOptions first = RemoteDebuggerOptions.Default;
            RemoteDebuggerOptions second = RemoteDebuggerOptions.Default;

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
        }

        [Test]
        public void EqualityConsidersAllFields()
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

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);

            Assert.That(left.Equals(differentNetwork), Is.False);
            Assert.That(left.Equals(differentMode), Is.False);
            Assert.That(left.Equals(differentHttpPort), Is.False);
            Assert.That(left.Equals(differentRpcPort), Is.False);
            Assert.That(left != differentNetwork, Is.True);
            Assert.That(left != differentMode, Is.True);
            Assert.That(left != differentHttpPort, Is.True);
            Assert.That(left != differentRpcPort, Is.True);
        }

        [Test]
        public void EqualOptionsProduceSameHashCode()
        {
            RemoteDebuggerOptions left = new RemoteDebuggerOptions()
            {
                NetworkOptions = Utf8TcpServerOptions.None,
                SingleScriptMode = false,
                HttpPort = null,
                RpcPortBase = 1234,
            };

            RemoteDebuggerOptions right = left;

            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }
    }
}
