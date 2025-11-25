namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Net;
    using System.Net.Sockets;
    using NovaSharp.RemoteDebugger.Network;
    using NUnit.Framework;

    [TestFixture]
    public sealed class Utf8TcpServerTests
    {
        [Test]
        public void CompleteMessageAppendsSeparatorWhenMissing()
        {
            using Utf8TcpServer server = CreateServer('\n');

            Assert.That(server.CompleteMessage(null), Is.EqualTo("\n"));
            Assert.That(server.CompleteMessage(string.Empty), Is.EqualTo("\n"));
            Assert.That(server.CompleteMessage("payload"), Is.EqualTo("payload\n"));
            Assert.That(server.CompleteMessage("payload\n"), Is.EqualTo("payload\n"));
        }

        [Test]
        public void BroadcastMessageHandlesNullPayloadWithoutPeers()
        {
            using Utf8TcpServer server = CreateServer('\r');

            Assert.DoesNotThrow(() => server.BroadcastMessage(null));
        }

        private static Utf8TcpServer CreateServer(char packetSeparator)
        {
            int port = GetFreeTcpPort();
            return new Utf8TcpServer(
                port,
                bufferSize: 64,
                packetSeparator,
                Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly
            );
        }

        private static int GetFreeTcpPort()
        {
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
