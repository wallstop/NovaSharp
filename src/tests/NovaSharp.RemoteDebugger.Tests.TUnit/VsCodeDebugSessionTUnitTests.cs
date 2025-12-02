namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.VsCodeDebugger;
    using NovaSharp.VsCodeDebugger.DebuggerLogic;

    public sealed class VsCodeDebugSessionTUnitTests
    {
        private static readonly int[] VerifiedBreakpointLines = new[] { 1, 3 };

        [global::TUnit.Core.Test]
        public async Task InitializeLaunchAndThreadsYieldInitializedEvent()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-basic.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("threads", new { });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement initResponse = transcript.RequireResponse("initialize");
            await Assert.That(initResponse.GetProperty("Success").GetBoolean()).IsTrue();

            bool initializedEvent = transcript.Events.Any(e =>
                DebugAdapterTranscript.TryGetEventName(e, out string eventName)
                && string.Equals(eventName, "initialized", StringComparison.Ordinal)
            );
            await Assert.That(initializedEvent).IsTrue();

            JsonElement threadsResponse = transcript.RequireResponse("threads");
            JsonElement threadArray = threadsResponse.GetProperty("Body").GetProperty("Threads");
            await Assert.That(threadArray.GetArrayLength()).IsEqualTo(1);

            string threadName = threadArray[0].GetProperty("Name").GetString();
            await Assert.That(threadName).IsEqualTo("Main Thread");
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReplListOutputsAttachedScriptNames()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-list.lua",
                "return 0"
            );

            fixture.QueueRequest("initialize", new { adapterID = "novasharp" });
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("evaluate", new { expression = "!list", context = "repl" });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            string[] outputMessages = transcript.GetOutputMessages().ToArray();

            bool listedScript = outputMessages.Any(message =>
                message.Contains("dap-list.lua", StringComparison.Ordinal)
            );

            await Assert.That(listedScript).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PauseCommandSetsPauseRequestedAndEmitsGuidance()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-pause.lua",
                "return 0"
            );

            fixture.QueueRequest("initialize", new { adapterID = "novasharp" });
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("pause", new { });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            await Assert.That(fixture.Debugger.PauseRequested).IsTrue();

            bool pauseMessageFound = transcript
                .GetOutputMessages()
                .Any(message =>
                    message.Contains("Pause pending", StringComparison.OrdinalIgnoreCase)
                );

            await Assert.That(pauseMessageFound).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetBreakpointsReturnsVerifiedEntries()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-breakpoints.lua",
                "local x = 1\nlocal y = 2\nreturn x + y\n"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest(
                "setBreakpoints",
                new { source = new { path = fixture.ScriptName }, lines = VerifiedBreakpointLines }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement response = transcript.RequireResponse("setBreakpoints");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(response, "Body");
            JsonElement breakpoints = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "Breakpoints"
            );
            JsonElement[] bpArray = breakpoints.EnumerateArray().ToArray();

            await Assert.That(bpArray.Length).IsEqualTo(2);
            await Assert
                .That(
                    bpArray.All(bp =>
                        DebugAdapterTranscript
                            .GetPropertyCaseInsensitive(bp, "verified")
                            .GetBoolean()
                    )
                )
                .IsTrue();

            int firstLine = DebugAdapterTranscript
                .GetPropertyCaseInsensitive(bpArray[0], "line")
                .GetInt32();
            int secondLine = DebugAdapterTranscript
                .GetPropertyCaseInsensitive(bpArray[1], "line")
                .GetInt32();

            await Assert.That(firstLine).IsEqualTo(1);
            await Assert.That(secondLine).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task ContinueRequestQueuesRunAction()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-continue.lua",
                "return 0"
            );

            fixture.QueueRequest("initialize", new { adapterID = "novasharp" });
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("continue", new { threadId = 0 });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();
            _ = transcript;

            DebuggerAction action = fixture.DrainQueuedAction();
            await Assert.That(action.Action).IsEqualTo(DebuggerAction.ActionType.Run);
        }

        [global::TUnit.Core.Test]
        public async Task SetErrorCommandUpdatesRegex()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-seterror.lua",
                "return 0"
            );

            fixture.QueueRequest("initialize", new { adapterID = "novasharp" });
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest(
                "evaluate",
                new { expression = "!seterror timeout", context = "repl" }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();
            _ = transcript;

            await Assert.That(fixture.Debugger.ErrorRegex.ToString()).IsEqualTo("timeout");
        }

        [global::TUnit.Core.Test]
        public async Task SwitchCommandTerminatesSessionAndUpdatesCurrentId()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "dap-switch-primary.lua",
                "return 0"
            );

            int secondId = fixture.AttachAdditionalScript("dap-switch-secondary.lua", "return 1");

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest(
                "evaluate",
                new { expression = $"!switch {secondId}", context = "repl" }
            );

            DebugAdapterTranscript transcript = fixture.Execute();

            bool terminatedEvent = transcript.Events.Any(e =>
                DebugAdapterTranscript.TryGetEventName(e, out string eventName)
                && string.Equals(eventName, "terminated", StringComparison.Ordinal)
            );

            await Assert.That(terminatedEvent).IsTrue();
            await Assert.That(fixture.CurrentId).IsEqualTo(secondId);
        }
    }

    internal sealed class VsCodeDebugSessionFixture : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null,
        };

        private readonly string _scriptName;
        private readonly Script _script;
        private readonly NovaSharpVsCodeDebugServer _server;
        private readonly AsyncDebugger _debugger;
        private readonly NovaSharpDebugSession _session;
        private readonly MemoryStream _input = new();
        private readonly MemoryStream _output = new();
        private int _sequence;

        private VsCodeDebugSessionFixture(
            string scriptName,
            Script script,
            NovaSharpVsCodeDebugServer server,
            AsyncDebugger debugger,
            NovaSharpDebugSession session
        )
        {
            _scriptName = scriptName;
            _script = script;
            _server = server;
            _debugger = debugger;
            _session = session;
        }

        public string ScriptName
        {
            get { return _scriptName; }
        }

        public AsyncDebugger Debugger
        {
            get { return _debugger; }
        }

        public int? CurrentId
        {
            get { return _server.CurrentId; }
        }

        public static VsCodeDebugSessionFixture Create(string scriptName, string code)
        {
            Script script = BuildScript(code, scriptName);

            NovaSharpVsCodeDebugServer server = new(GetFreeTcpPort());
            server.AttachToScript(script, scriptName);

            AsyncDebugger debugger = (AsyncDebugger)
                typeof(NovaSharpVsCodeDebugServer)
                    .GetField(
                        "_current",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    )
                    ?.GetValue(server);

            if (debugger == null)
            {
                throw new InvalidOperationException("Unable to capture AsyncDebugger from server.");
            }

            NovaSharpDebugSession session = new(server, debugger);
            return new VsCodeDebugSessionFixture(scriptName, script, server, debugger, session);
        }

        public void QueueRequest(string command, object arguments)
        {
            int sequence = ++_sequence;
            object payload = new
            {
                Sequenceuence = sequence,
                type = "request",
                Command = command,
                Arguments = arguments ?? new { },
            };

            string json = JsonSerializer.Serialize(payload, JsonOptions);
            byte[] body = Encoding.UTF8.GetBytes(json);
            string header = $"Content-Length: {body.Length}\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);

            _input.Write(headerBytes, 0, headerBytes.Length);
            _input.Write(body, 0, body.Length);
        }

        public DebugAdapterTranscript Execute()
        {
            _input.Position = 0;
            _output.SetLength(0);
            _session.ProcessLoop(_input, _output);
            _output.Position = 0;
            return DebugAdapterTranscript.Parse(_output);
        }

        public void Dispose()
        {
            _session.Stop();
            _server.Dispose();
            _input.Dispose();
            _output.Dispose();
        }

        public DebuggerAction DrainQueuedAction()
        {
            SourceRef sourceRef = new(0, 0, 0, 1, 1, false);
            return ((IDebugger)_debugger).GetAction(0, sourceRef);
        }

        public int AttachAdditionalScript(string scriptName, string code)
        {
            Script script = BuildScript(code, scriptName);
            _server.AttachToScript(script, scriptName);

            foreach (KeyValuePair<int, string> entry in _server.GetAttachedDebuggersByIdAndName())
            {
                if (string.Equals(entry.Value, scriptName, StringComparison.Ordinal))
                {
                    return entry.Key;
                }
            }

            throw new InvalidOperationException(
                $"Unable to find debugger entry for script '{scriptName}'."
            );
        }

        private static int GetFreeTcpPort()
        {
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static Script BuildScript(string code, string chunkName)
        {
            Script script = new();
            script.Options.DebugPrint = _ => { };
            script.LoadString(code, null, chunkName);
            return script;
        }
    }

    internal sealed class DebugAdapterTranscript
    {
        private static readonly string[] HeaderSeparators = { "\r\n" };

        public DebugAdapterTranscript(
            IReadOnlyList<JsonElement> responses,
            IReadOnlyList<JsonElement> events
        )
        {
            Responses = responses;
            Events = events;
        }

        public IReadOnlyList<JsonElement> Responses { get; }

        public IReadOnlyList<JsonElement> Events { get; }

        public JsonElement RequireResponse(string command)
        {
            foreach (JsonElement response in Responses)
            {
                if (
                    string.Equals(
                        response.GetProperty("Command").GetString(),
                        command,
                        StringComparison.Ordinal
                    )
                )
                {
                    return response;
                }
            }

            throw new InvalidOperationException($"Response for '{command}' not found.");
        }

        public IEnumerable<string> GetOutputMessages()
        {
            foreach (JsonElement evt in Events)
            {
                if (
                    TryGetEventName(evt, out string eventName)
                    && string.Equals(eventName, "output", StringComparison.Ordinal)
                )
                {
                    JsonElement body = GetPropertyCaseInsensitive(evt, "Body");
                    JsonElement message = GetPropertyCaseInsensitive(body, "output");
                    yield return message.GetString();
                }
            }
        }

        public static DebugAdapterTranscript Parse(Stream stream)
        {
            List<JsonElement> responses = new();
            List<JsonElement> events = new();

            while (TryReadFrame(stream, out string json))
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement envelope = document.RootElement.Clone();
                if (
                    !envelope.TryGetProperty("type", out JsonElement typeProperty)
                    && !envelope.TryGetProperty("Type", out typeProperty)
                )
                {
                    throw new InvalidOperationException($"Malformed adapter payload: {json}");
                }

                string type = typeProperty.GetString();

                if (string.Equals(type, "response", StringComparison.Ordinal))
                {
                    responses.Add(envelope);
                }
                else if (string.Equals(type, "event", StringComparison.Ordinal))
                {
                    events.Add(envelope);
                }
            }

            return new DebugAdapterTranscript(responses, events);
        }

        internal static bool TryGetEventName(JsonElement evt, out string name)
        {
            if (
                TryGetPropertyCaseInsensitive(evt, "@event", out JsonElement eventProperty)
                || TryGetPropertyCaseInsensitive(evt, "event", out eventProperty)
            )
            {
                name = eventProperty.GetString();
                return true;
            }

            name = null;
            return false;
        }

        internal static JsonElement GetPropertyCaseInsensitive(
            JsonElement element,
            string propertyName
        )
        {
            if (TryGetPropertyCaseInsensitive(element, propertyName, out JsonElement value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Property '{propertyName}' not found.");
        }

        internal static bool TryGetPropertyCaseInsensitive(
            JsonElement element,
            string propertyName,
            out JsonElement value
        )
        {
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            string camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
            if (
                !string.Equals(camelCase, propertyName, StringComparison.Ordinal)
                && element.TryGetProperty(camelCase, out value)
            )
            {
                return true;
            }

            string pascalCase = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
            if (
                !string.Equals(pascalCase, propertyName, StringComparison.Ordinal)
                && element.TryGetProperty(pascalCase, out value)
            )
            {
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadFrame(Stream stream, out string json)
        {
            json = null;
            StringBuilder headerBuilder = new();
            while (true)
            {
                int value = stream.ReadByte();
                if (value == -1)
                {
                    if (headerBuilder.Length == 0)
                    {
                        return false;
                    }

                    throw new InvalidOperationException(
                        "Unexpected end of stream while reading header."
                    );
                }

                headerBuilder.Append((char)value);

                if (headerBuilder.Length >= 4)
                {
                    int length = headerBuilder.Length;
                    if (
                        headerBuilder[length - 4] == '\r'
                        && headerBuilder[length - 3] == '\n'
                        && headerBuilder[length - 2] == '\r'
                        && headerBuilder[length - 1] == '\n'
                    )
                    {
                        break;
                    }
                }
            }

            string header = headerBuilder.ToString();
            int contentLength = ParseContentLength(header);

            byte[] buffer = new byte[contentLength];
            int read = stream.Read(buffer, 0, contentLength);
            if (read != contentLength)
            {
                throw new InvalidOperationException("Unexpected end of stream while reading body.");
            }

            json = Encoding.UTF8.GetString(buffer);
            return true;
        }

        private static int ParseContentLength(string header)
        {
            string[] lines = header.Split(HeaderSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (
                    line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(
                        line.Substring("Content-Length:".Length).Trim(),
                        out int contentLength
                    )
                )
                {
                    return contentLength;
                }
            }

            throw new InvalidOperationException("Missing Content-Length header.");
        }
    }
}
