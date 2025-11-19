namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.RemoteDebugger.Network;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RemoteDebuggerServiceTests
    {
        private static readonly TimeSpan ServerReadyTimeout = TimeSpan.FromSeconds(2);

        [Test]
        public void SingleScriptModeServesIframeWithConfiguredRpcPort()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = true;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeTcpPort();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);

            string body = GetHttpBody(options.HttpPort.Value, "/");

            Assert.Multiple(() =>
            {
                Assert.That(body, Does.Contain($"Debugger?port={options.RpcPortBase}"));
                Assert.That(
                    service.HttpUrlStringLocalHost,
                    Is.EqualTo($"http://127.0.0.1:{options.HttpPort.Value}/")
                );
            });
        }

        [Test]
        public void JumpPageListsAttachedScripts()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = false;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeTcpPort();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            Script script = BuildScript("return 1", "jump.lua");
            service.Attach(script, "JumpScript");

            string body = GetHttpBody(options.HttpPort.Value, "/");

            Assert.Multiple(() =>
            {
                Assert.That(body, Does.Contain("JumpScript"));
                Assert.That(body, Does.Contain("Debugger?port=" + options.RpcPortBase));
            });
        }

        [Test]
        public void CliBridgeForwardsAttachAndHttpUrl()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = true;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeTcpPort();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            RemoteDebuggerServiceBridge bridge = new(service);
            Script script = BuildScript("return 0", "bridge.lua");

            bridge.Attach(script, "BridgeScript", freeRunAfterAttach: true);

            Assert.Multiple(() =>
            {
                Assert.That(script.DebuggerEnabled, Is.True);
                Assert.That(
                    bridge.HttpUrlStringLocalHost,
                    Is.EqualTo($"http://127.0.0.1:{options.HttpPort.Value}/")
                );
            });
        }

        [Test]
        public void DebuggerPageServesEmbeddedUi()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = false;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeTcpPort();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            Script script = BuildScript("return 1", "debugger.lua");
            service.Attach(script, "DebuggerScript");

            string debuggerResponse = SendHttpRequest(
                options.HttpPort.Value,
                $"/Debugger?port={options.RpcPortBase}"
            );

            Assert.Multiple(() =>
            {
                Assert.That(debuggerResponse, Does.Contain("200 OK"));
                Assert.That(debuggerResponse, Does.Contain("<html"));
                Assert.That(debuggerResponse, Does.Contain("swfobject.js"));
            });
        }

        private static Script BuildScript(string code, string chunkName)
        {
            Script script = new();
            script.Options.DebugPrint = _ => { };
            script.LoadString(code, null, chunkName);
            return script;
        }

        private static int GetFreeTcpPort()
        {
            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string GetHttpBody(int port, string path)
        {
            string response = SendHttpRequest(port, path);
            int separator = response.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            int delimiterLength = 4;
            if (separator < 0)
            {
                separator = response.IndexOf("\n\n", StringComparison.Ordinal);
                delimiterLength = 2;
            }

            return separator >= 0 ? response.Substring(separator + delimiterLength) : response;
        }

        private static string SendHttpRequest(int port, string path)
        {
            Exception lastException = null;
            string response = null;

            bool success = TestWaitHelpers.SpinUntil(
                () =>
                {
                    try
                    {
                        response = IssueRequest(port, path);
                        return true;
                    }
                    catch (SocketException ex)
                    {
                        lastException = ex;
                        Thread.Sleep(25);
                        return false;
                    }
                },
                ServerReadyTimeout
            );

            if (!success || response == null)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to HTTP debug host on port {port}.",
                    lastException
                );
            }

            return response;
        }

        private static string IssueRequest(int port, string path)
        {
            using TcpClient client = new();
            client.Connect(IPAddress.Loopback, port);
            using NetworkStream stream = client.GetStream();

            string request = $"GET {path} HTTP/1.0\r\nHost: localhost\r\n\r\n";
            byte[] payload = Encoding.ASCII.GetBytes(request);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();

            using MemoryStream buffer = new();
            byte[] chunk = new byte[1024];
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                buffer.Write(chunk, 0, read);
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }
    }
}
