namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using NovaSharp.RemoteDebugger.Network;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HttpServerTests
    {
        private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(2);

        [Test]
        public void AuthenticatorRejectsMissingCredentialsAndAllowsValidOnes()
        {
            int port = GetFreeTcpPort();
            using HttpServer server = new(
                port,
                Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly
            );
            server.Authenticator = (user, password) =>
                user == "nova" && password == "sharp";
            server.RegisterResource(
                "/secure",
                HttpResource.CreateText(HttpResourceType.PlainText, "ok")
            );
            server.Start();

            string unauthorized = SendHttpRequest(port, "/secure");
            Assert.That(unauthorized, Does.Contain("401 Not Authorized"));
            Assert.That(unauthorized, Does.Contain("WWW-Authenticate"));

            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes("nova:sharp"));
            string authorized = SendHttpRequest(
                port,
                "/secure",
                $"Authorization: Basic {token}"
            );
            Assert.That(authorized, Does.Contain("200 OK"));
            Assert.That(GetBody(authorized), Is.EqualTo("ok"));
        }

        [Test]
        public void CallbackResourceReceivesQueryArguments()
        {
            int port = GetFreeTcpPort();
            Dictionary<string, string> observedArgs = null;
            using HttpServer server = new(
                port,
                Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly
            );
            server.RegisterResource(
                "/callback",
                HttpResource.CreateCallback(args =>
                {
                    observedArgs = args;
                    return HttpResource.CreateText(HttpResourceType.Json, "{\"status\":\"ok\"}");
                })
            );
            server.Start();

            string response = SendHttpRequest(port, "/callback?foo=bar&flag");

            Assert.Multiple(() =>
            {
                Assert.That(response, Does.Contain("200 OK"));
                Assert.That(GetBody(response), Is.EqualTo("{\"status\":\"ok\"}"));
                Assert.That(observedArgs, Is.Not.Null);
                Assert.That(observedArgs["foo"], Is.EqualTo("bar"));
                Assert.That(observedArgs["flag"], Is.Null);
                Assert.That(observedArgs["?"], Is.EqualTo("/callback"));
            });
        }

        [Test]
        public void MissingResourceReturns404()
        {
            int port = GetFreeTcpPort();
            using HttpServer server = new(
                port,
                Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly
            );
            server.Start();

            string response = SendHttpRequest(port, "/missing");

            Assert.That(response, Does.Contain("404 Not Found"));
        }

        [Test]
        public void CallbackExceptionReturns500()
        {
            int port = GetFreeTcpPort();
            using HttpServer server = new(
                port,
                Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly
            );
            server.RegisterResource(
                "/explode",
                HttpResource.CreateCallback(_ => throw new InvalidOperationException("boom"))
            );
            server.Start();

            string response = SendHttpRequest(port, "/explode");

            Assert.That(response, Does.Contain("500 Internal Server Error"));
        }

        private static string GetBody(string response)
        {
            int separator = response.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            int delimiterLength = 4;
            if (separator < 0)
            {
                separator = response.IndexOf("\n\n", StringComparison.Ordinal);
                delimiterLength = separator < 0 ? 0 : 2;
            }

            return separator >= 0 ? response.Substring(separator + delimiterLength) : string.Empty;
        }

        private static string SendHttpRequest(int port, string path, params string[] headers)
        {
            Exception lastError = null;
            string response = null;

            bool success = TestWaitHelpers.SpinUntil(
                () =>
                {
                    try
                    {
                        response = ExecuteRequest(port, path, headers);
                        return true;
                    }
                    catch (SocketException ex)
                    {
                        lastError = ex;
                        Thread.Sleep(25);
                        return false;
                    }
                },
                RetryTimeout
            );

            if (!success || response == null)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to HTTP server on port {port}.",
                    lastError
                );
            }

            return response;
        }

        private static string ExecuteRequest(int port, string path, params string[] headers)
        {
            using TcpClient client = new();
            client.Connect(IPAddress.Loopback, port);
            using NetworkStream stream = client.GetStream();

            StringBuilder builder = new();
            builder.Append($"GET {path} HTTP/1.0\r\n");
            builder.Append("Host: localhost\r\n");
            foreach (string header in headers)
            {
                builder.Append(header).Append("\r\n");
            }

            builder.Append("\r\n");
            byte[] payload = Encoding.ASCII.GetBytes(builder.ToString());
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

        private static int GetFreeTcpPort()
        {
            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
