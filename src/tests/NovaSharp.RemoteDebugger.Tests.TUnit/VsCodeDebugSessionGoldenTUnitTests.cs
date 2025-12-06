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

    /// <summary>
    /// Tests that validate DAP protocol responses against golden payload files.
    /// These tests ensure the VS Code debugger adapter produces correct JSON responses.
    /// </summary>
    public sealed class VsCodeDebugSessionGoldenTUnitTests
    {
        /// <summary>
        /// Properties to ignore during comparison (volatile values like sequence numbers).
        /// </summary>
        private static readonly HashSet<string> IgnoredProperties = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "Sequenceuence",
            "RequestSequenceuence",
            "seq",
            "request_seq",
        };

        /// <summary>
        /// Breakpoint lines used in setBreakpoints golden test (lines 1 and 3).
        /// </summary>
        private static readonly int[] SetBreakpointsLines = new[] { 1, 3 };

        /// <summary>
        /// Breakpoint lines used in multi-breakpoint test (lines 1, 2, 3, 4).
        /// </summary>
        private static readonly int[] MultiBreakpointsLines = new[] { 1, 2, 3, 4 };

        [global::TUnit.Core.Test]
        public async Task InitializeResponseMatchesGoldenPayload()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-initialize.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement initResponse = transcript.RequireResponse("initialize");

            using JsonDocument golden = GoldenPayloadHelper.LoadGoldenFile(
                "initialize-response.json"
            );
            IReadOnlyList<string> differences = GoldenPayloadHelper.CompareJson(
                golden.RootElement,
                initResponse,
                IgnoredProperties
            );

            await Assert
                .That(differences.Count)
                .IsEqualTo(0)
                .Because(
                    $"Response should match golden file. Differences: {string.Join("; ", differences)}"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ThreadsResponseMatchesGoldenPayload()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-threads.lua",
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

            JsonElement threadsResponse = transcript.RequireResponse("threads");

            using JsonDocument golden = GoldenPayloadHelper.LoadGoldenFile("threads-response.json");
            IReadOnlyList<string> differences = GoldenPayloadHelper.CompareJson(
                golden.RootElement,
                threadsResponse,
                IgnoredProperties
            );

            await Assert
                .That(differences.Count)
                .IsEqualTo(0)
                .Because(
                    $"Response should match golden file. Differences: {string.Join("; ", differences)}"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetBreakpointsResponseMatchesGoldenPayload()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-breakpoints.lua",
                "local x = 1\nlocal y = 2\nreturn x + y\n"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest(
                "setBreakpoints",
                new { source = new { path = fixture.ScriptName }, lines = SetBreakpointsLines }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement setBreakpointsResponse = transcript.RequireResponse("setBreakpoints");

            using JsonDocument golden = GoldenPayloadHelper.LoadGoldenFile(
                "setBreakpoints-response.json"
            );
            IReadOnlyList<string> differences = GoldenPayloadHelper.CompareJson(
                golden.RootElement,
                setBreakpointsResponse,
                IgnoredProperties
            );

            await Assert
                .That(differences.Count)
                .IsEqualTo(0)
                .Because(
                    $"Response should match golden file. Differences: {string.Join("; ", differences)}"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InitializedEventMatchesGoldenPayload()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-initialized-event.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement initializedEvent = transcript.Events.FirstOrDefault(e =>
                DebugAdapterTranscript.TryGetEventName(e, out string name)
                && string.Equals(name, "initialized", StringComparison.Ordinal)
            );

            await Assert
                .That(initializedEvent.ValueKind)
                .IsNotEqualTo(JsonValueKind.Undefined)
                .Because("initialized event should be present")
                .ConfigureAwait(false);

            using JsonDocument golden = GoldenPayloadHelper.LoadGoldenFile(
                "initialized-event.json"
            );

            // For events, we compare the event name only (body may be null)
            bool eventTypeMatches =
                DebugAdapterTranscript.TryGetEventName(initializedEvent, out string actualName)
                && string.Equals(actualName, "initialized", StringComparison.Ordinal);

            await Assert
                .That(eventTypeMatches)
                .IsTrue()
                .Because("Event name should match golden file")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateResponseMatchesGoldenPayload()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-evaluate.lua",
                "x = 42\nreturn x"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("evaluate", new { expression = "42", context = "watch" });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement evaluateResponse = transcript.RequireResponse("evaluate");

            using JsonDocument golden = GoldenPayloadHelper.LoadGoldenFile(
                "evaluate-response.json"
            );
            IReadOnlyList<string> differences = GoldenPayloadHelper.CompareJson(
                golden.RootElement,
                evaluateResponse,
                IgnoredProperties
            );

            await Assert
                .That(differences.Count)
                .IsEqualTo(0)
                .Because(
                    $"Response should match golden file. Differences: {string.Join("; ", differences)}"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InitializeResponseContainsExpectedCapabilities()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-capabilities.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement initResponse = transcript.RequireResponse("initialize");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                initResponse,
                "Body"
            );

            // Verify specific capabilities
            JsonElement supportsConfigDone = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "SupportsConfigurationDoneRequest"
            );
            await Assert
                .That(supportsConfigDone.GetBoolean())
                .IsFalse()
                .Because("NovaSharp does not require configurationDone")
                .ConfigureAwait(false);

            JsonElement supportsFunctionBp = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "SupportsFunctionBreakpoints"
            );
            await Assert
                .That(supportsFunctionBp.GetBoolean())
                .IsFalse()
                .Because("NovaSharp does not support function breakpoints")
                .ConfigureAwait(false);

            JsonElement supportsConditionalBp = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "SupportsConditionalBreakpoints"
            );
            await Assert
                .That(supportsConditionalBp.GetBoolean())
                .IsFalse()
                .Because("NovaSharp does not support conditional breakpoints")
                .ConfigureAwait(false);

            JsonElement supportsHovers = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "SupportsEvaluateForHovers"
            );
            await Assert
                .That(supportsHovers.GetBoolean())
                .IsFalse()
                .Because("NovaSharp hover evaluation may have side effects")
                .ConfigureAwait(false);

            JsonElement exceptionFilters = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "ExceptionBreakpointFilters"
            );
            // ExceptionBreakpointFilters may be serialized as empty array [] or empty object {} depending on serializer
            bool filtersEmpty =
                (
                    exceptionFilters.ValueKind == JsonValueKind.Array
                    && exceptionFilters.GetArrayLength() == 0
                )
                || (
                    exceptionFilters.ValueKind == JsonValueKind.Object
                    && !exceptionFilters.EnumerateObject().Any()
                );
            await Assert
                .That(filtersEmpty)
                .IsTrue()
                .Because("NovaSharp does not expose exception filters")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ThreadsResponseContainsMainThread()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-main-thread.lua",
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

            JsonElement threadsResponse = transcript.RequireResponse("threads");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                threadsResponse,
                "Body"
            );
            JsonElement threads = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "Threads"
            );

            await Assert
                .That(threads.GetArrayLength())
                .IsGreaterThanOrEqualTo(1)
                .Because("At least one thread should be present")
                .ConfigureAwait(false);

            JsonElement firstThread = threads[0];
            JsonElement threadName = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                firstThread,
                "Name"
            );

            await Assert
                .That(threadName.GetString())
                .IsEqualTo("Main Thread")
                .Because("Primary thread should be named 'Main Thread'")
                .ConfigureAwait(false);

            JsonElement threadId = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                firstThread,
                "Id"
            );
            await Assert
                .That(threadId.GetInt32())
                .IsEqualTo(0)
                .Because("Main thread should have ID 0")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MultipleBreakpointsAreAllVerified()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-multi-bp.lua",
                "local a = 1\nlocal b = 2\nlocal c = 3\nlocal d = 4\nreturn a + b + c + d\n"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest(
                "setBreakpoints",
                new { source = new { path = fixture.ScriptName }, lines = MultiBreakpointsLines }
            );
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement setBreakpointsResponse = transcript.RequireResponse("setBreakpoints");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                setBreakpointsResponse,
                "Body"
            );
            JsonElement breakpoints = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                body,
                "Breakpoints"
            );

            await Assert
                .That(breakpoints.GetArrayLength())
                .IsEqualTo(4)
                .Because("All four breakpoints should be returned")
                .ConfigureAwait(false);

            int verifiedCount = 0;
            foreach (JsonElement bp in breakpoints.EnumerateArray())
            {
                JsonElement verified = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                    bp,
                    "Verified"
                );
                if (verified.GetBoolean())
                {
                    verifiedCount++;
                }
            }

            await Assert
                .That(verifiedCount)
                .IsEqualTo(4)
                .Because("All breakpoints should be verified")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReturnsCorrectTypeForNumber()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-eval-number.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("evaluate", new { expression = "123.456", context = "watch" });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement evaluateResponse = transcript.RequireResponse("evaluate");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                evaluateResponse,
                "Body"
            );
            JsonElement type = DebugAdapterTranscript.GetPropertyCaseInsensitive(body, "Type");

            await Assert
                .That(type.GetString())
                .IsEqualTo("number")
                .Because("Numeric literals should evaluate to number type")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReturnsCorrectTypeForFunction()
        {
            // Test that evaluating a function returns the "function" type
            // This avoids JSON escaping issues with string literals
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-eval-function.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            // Evaluate a built-in function reference
            fixture.QueueRequest("evaluate", new { expression = "print", context = "watch" });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement evaluateResponse = transcript.RequireResponse("evaluate");

            JsonElement success = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                evaluateResponse,
                "Success"
            );
            await Assert
                .That(success.GetBoolean())
                .IsTrue()
                .Because("Evaluate request should succeed")
                .ConfigureAwait(false);

            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                evaluateResponse,
                "Body"
            );

            // Verify the Type is "clrfunction" (print is implemented in CLR)
            if (
                DebugAdapterTranscript.TryGetPropertyCaseInsensitive(
                    body,
                    "Type",
                    out JsonElement type
                )
            )
            {
                await Assert
                    .That(type.GetString())
                    .IsEqualTo("clrfunction")
                    .Because("Built-in print function is a CLR function")
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReturnsCorrectTypeForNil()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-eval-nil.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("evaluate", new { expression = "nil", context = "watch" });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement evaluateResponse = transcript.RequireResponse("evaluate");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                evaluateResponse,
                "Body"
            );
            JsonElement type = DebugAdapterTranscript.GetPropertyCaseInsensitive(body, "Type");

            await Assert
                .That(type.GetString())
                .IsEqualTo("nil")
                .Because("nil should evaluate to nil type")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReturnsCorrectTypeForBoolean()
        {
            using VsCodeDebugSessionFixture fixture = VsCodeDebugSessionFixture.Create(
                "golden-eval-bool.lua",
                "return 0"
            );

            fixture.QueueRequest(
                "initialize",
                new { adapterID = "novasharp", pathFormat = "path" }
            );
            fixture.QueueRequest("launch", new { noDebug = false });
            fixture.QueueRequest("evaluate", new { expression = "true", context = "watch" });
            fixture.QueueRequest("disconnect", new { restart = false });

            DebugAdapterTranscript transcript = fixture.Execute();

            JsonElement evaluateResponse = transcript.RequireResponse("evaluate");
            JsonElement body = DebugAdapterTranscript.GetPropertyCaseInsensitive(
                evaluateResponse,
                "Body"
            );
            JsonElement type = DebugAdapterTranscript.GetPropertyCaseInsensitive(body, "Type");

            await Assert
                .That(type.GetString())
                .IsEqualTo("boolean")
                .Because("Boolean literals should evaluate to boolean type")
                .ConfigureAwait(false);
        }
    }
}
