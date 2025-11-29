namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.RemoteDebugger.Network;

    public sealed class RemoteDebuggerServiceTUnitTests
    {
        private static readonly TimeSpan ServerReadyTimeout = TimeSpan.FromSeconds(2);

        [global::TUnit.Core.Test]
        public async Task SingleScriptModeServesIframeWithConfiguredRpcPort()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = true;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeRpcPortBase();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);

            string body = GetHttpBody(options.HttpPort.Value, "/");

            await Assert.That(body).Contains($"Debugger?port={options.RpcPortBase}");
            await Assert
                .That(service.HttpUrlStringLocalHost)
                .IsEqualTo(new Uri($"http://127.0.0.1:{options.HttpPort.Value}/"));
        }

        [global::TUnit.Core.Test]
        public async Task JumpPageListsAttachedScripts()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = false;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeRpcPortBase();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            Script script = BuildScript("return 1", "jump.lua");
            service.Attach(script, "JumpScript");

            string body = GetHttpBody(options.HttpPort.Value, "/");

            await Assert.That(body).Contains("JumpScript");
            await Assert.That(body).Contains($"Debugger?port={options.RpcPortBase}");
        }

        [global::TUnit.Core.Test]
        public async Task JumpPageListsMultipleScriptsWithDistinctPorts()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = false;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeRpcPortBase();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            Script first = BuildScript("return 1", "multi1.lua");
            Script second = BuildScript("return 2", "multi2.lua");
            service.Attach(first, "FirstScript");
            service.Attach(second, "SecondScript");

            string body = GetHttpBody(options.HttpPort.Value, "/");
            string firstLink = $"Debugger?port={options.RpcPortBase}";
            string secondLink = $"Debugger?port={options.RpcPortBase + 1}";

            await Assert.That(body).Contains("FirstScript");
            await Assert.That(body).Contains("SecondScript");
            await Assert.That(body).Contains(firstLink);
            await Assert.That(body).Contains(secondLink);
            await Assert.That(CountOccurrences(body, "Debugger?port=")).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task CliBridgeForwardsAttachAndHttpUrl()
        {
            Script script = BuildScript("return 0", "bridge.lua");

            Type bridgeType = Type.GetType(
                "NovaSharp.Cli.Commands.Implementations.RemoteDebuggerServiceBridge, NovaSharp.Cli",
                throwOnError: true
            )!;
            object bridge = Activator.CreateInstance(bridgeType)!;
            MethodInfo attachMethod = bridgeType.GetMethod(
                "Attach",
                BindingFlags.Public | BindingFlags.Instance
            )!;
            attachMethod.Invoke(bridge, new object[] { script, "BridgeScript", true });
            PropertyInfo httpProperty = bridgeType.GetProperty(
                "HttpUrlStringLocalHost",
                BindingFlags.Public | BindingFlags.Instance
            )!;

            await Assert.That(script.DebuggerEnabled).IsTrue();
            Uri httpUrl = (Uri)httpProperty.GetValue(bridge);
            await Assert.That(httpUrl).IsNotNull();
            await Assert.That(httpUrl.Scheme).IsEqualTo("http");
        }

        [global::TUnit.Core.Test]
        public async Task DebuggerPageServesEmbeddedUi()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = false;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeRpcPortBase();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            Script script = BuildScript("return 1", "debugger.lua");
            service.Attach(script, "DebuggerScript");

            string debuggerResponse = SendHttpRequest(
                options.HttpPort.Value,
                $"/Debugger?port={options.RpcPortBase}"
            );

            await Assert.That(debuggerResponse).Contains("200 OK");
            await Assert.That(debuggerResponse).Contains("<html");
            await Assert.That(debuggerResponse).Contains("swfobject.js");
        }

        [global::TUnit.Core.Test]
        public async Task InvalidDebuggerRequestReturnsPaddedErrorPage()
        {
            RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
            options.SingleScriptMode = false;
            options.HttpPort = GetFreeTcpPort();
            options.RpcPortBase = GetFreeRpcPortBase();
            options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

            using RemoteDebuggerService service = new(options);
            Script script = BuildScript("return 1", "pad.lua");
            service.Attach(script, "PadScript");

            string response = SendHttpRequest(options.HttpPort.Value, "/DebuggerInvalid?port=9999");
            string body = ExtractHttpBody(response);

            await Assert.That(response).Contains("404 Not Found");
            await Assert.That(body).Contains("This padding is added to bring the error message");
        }

        [global::TUnit.Core.Test]
        public async Task AttachFromDirectoryAppliesManifestCompatibility()
        {
            string modDirectory = CreateTempDirectory();
            try
            {
                await File.WriteAllTextAsync(
                        Path.Combine(modDirectory, "mod.json"),
                        "{ \"name\": \"Sample\", \"luaCompatibility\": \"Lua52\" }"
                    )
                    .ConfigureAwait(false);
                await File.WriteAllTextAsync(
                        Path.Combine(modDirectory, "main.lua"),
                        "return _VERSION"
                    )
                    .ConfigureAwait(false);

                RemoteDebuggerOptions options = RemoteDebuggerOptions.Default;
                options.HttpPort = null;
                options.RpcPortBase = GetFreeRpcPortBase();
                options.NetworkOptions = Utf8TcpServerOptions.LocalHostOnly;

                using RemoteDebuggerService service = new(options);
                ScriptOptions baseOptions = new(Script.DefaultOptions);

                List<string> info = new();
                List<string> warnings = new();

                Script script = service.AttachFromDirectory(
                    modDirectory,
                    scriptName: null,
                    modules: CoreModules.Basic,
                    baseOptions: baseOptions,
                    infoSink: info.Add,
                    warningSink: warnings.Add
                );

                await Assert.That(script).IsNotNull();
                await Assert
                    .That(script.CompatibilityVersion)
                    .IsEqualTo(LuaCompatibilityVersion.Lua52);
                await Assert.That(script.DebuggerEnabled).IsTrue();
                await Assert.That(info.Count).IsGreaterThanOrEqualTo(2);
                await Assert.That(info[0]).Contains("Lua 5.2");
                await Assert
                    .That(info.Any(message => ContainsOrdinal(message, "running under")))
                    .IsTrue();
                await Assert.That(warnings.Count).IsEqualTo(0);
            }
            finally
            {
                TryDeleteDirectory(modDirectory);
            }
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
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static readonly object RpcPortAllocationLock = new();
        private static int NextRpcPortBase = 45000;

        private static int GetFreeRpcPortBase()
        {
            lock (RpcPortAllocationLock)
            {
                const int LowerBound = 45000;
                const int UpperBound = 64000;
                int candidate = NextRpcPortBase;

                for (int attempt = 0; attempt < 1024; attempt++)
                {
                    if (candidate < LowerBound || candidate > UpperBound)
                    {
                        candidate = LowerBound;
                    }

                    if (IsPortAvailable(candidate) && IsPortAvailable(candidate + 1))
                    {
                        NextRpcPortBase = candidate + 2;
                        return candidate;
                    }

                    candidate += 2;
                }

                throw new InvalidOperationException(
                    $"Unable to allocate two consecutive RPC ports in the test range {LowerBound}-{UpperBound}."
                );
            }
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using TcpListener listener = new(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static string GetHttpBody(int port, string path)
        {
            string response = SendHttpRequest(port, path);
            return ExtractHttpBody(response);
        }

        private static string ExtractHttpBody(string response)
        {
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

        private static int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return 0;
            }

            int count = 0;
            int index = 0;
            while (true)
            {
                int next = source.IndexOf(value, index, StringComparison.Ordinal);
                if (next < 0)
                {
                    break;
                }

                count++;
                index = next + value.Length;
            }

            return count;
        }

        private static bool ContainsOrdinal(string source, string value)
        {
            return source != null && source.Contains(value, StringComparison.Ordinal);
        }

        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"novasharp_debugger_{Guid.NewGuid():N}"
            );
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
