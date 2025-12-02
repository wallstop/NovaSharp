namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.VsCodeDebugger;

    public sealed class VsCodeDebugServerTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task AttachToScriptRegistersDebuggerAndSetsCurrent()
        {
            using NovaSharpVsCodeDebugServer server = new(GetFreeTcpPort());
            Script script = BuildScript("return 1", "vs-code-attach.lua");

            server.AttachToScript(script, "PrimaryScript");

            KeyValuePair<int, string>[] attached = server
                .GetAttachedDebuggersByIdAndName()
                .ToArray();

            await Assert.That(attached.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(attached[0].Value).IsEqualTo("PrimaryScript").ConfigureAwait(false);
            await Assert.That(attached[0].Key).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(server.Current).IsSameReferenceAs(script).ConfigureAwait(false);
            await Assert.That(script.DebuggerEnabled).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AttachToScriptRejectsDuplicateScripts()
        {
            using NovaSharpVsCodeDebugServer server = new(GetFreeTcpPort());
            Script script = BuildScript("return 2", "vs-code-duplicate.lua");

            server.AttachToScript(script, "PrimaryScript");
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                server.AttachToScript(script, "DuplicateScript");
            });

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("Script already attached")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CurrentIdSwitchesBetweenAttachedDebuggers()
        {
            using NovaSharpVsCodeDebugServer server = new(GetFreeTcpPort());
            Script first = BuildScript("return 3", "vs-code-current-a.lua");
            Script second = BuildScript("return 4", "vs-code-current-b.lua");

            server.AttachToScript(first, "FirstScript");
            server.AttachToScript(second, "SecondScript");

            Dictionary<string, int> attached = server
                .GetAttachedDebuggersByIdAndName()
                .ToDictionary(pair => pair.Value, pair => pair.Key);

            int secondId = attached["SecondScript"];
            int firstId = attached["FirstScript"];

            server.CurrentId = secondId;
            await Assert.That(server.Current).IsSameReferenceAs(second).ConfigureAwait(false);

            server.CurrentId = firstId;
            await Assert.That(server.Current).IsSameReferenceAs(first).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DetachRemovesDebuggerAndReselectsCurrent()
        {
            using NovaSharpVsCodeDebugServer server = new(GetFreeTcpPort());
            Script first = BuildScript("return 5", "vs-code-detach-a.lua");
            Script second = BuildScript("return 6", "vs-code-detach-b.lua");

            server.AttachToScript(first, "FirstScript");
            server.AttachToScript(second, "SecondScript");
            server.Current = first;

            server.Detach(first);

            KeyValuePair<int, string>[] remaining = server
                .GetAttachedDebuggersByIdAndName()
                .ToArray();

            await Assert.That(remaining.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(remaining[0].Value).IsEqualTo("SecondScript").ConfigureAwait(false);
            await Assert.That(server.Current).IsSameReferenceAs(second).ConfigureAwait(false);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                server.Detach(first)
            );
            await Assert
                .That(exception.Message)
                .Contains("Cannot detach script")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DisposePreventsFurtherAttachments()
        {
            NovaSharpVsCodeDebugServer server = new(GetFreeTcpPort());
            server.Dispose();

            Script script = BuildScript("return 7", "vs-code-disposed.lua");
            ObjectDisposedException exception = Assert.Throws<ObjectDisposedException>(() =>
            {
                server.AttachToScript(script, "DisposedScript");
            });

            await Assert
                .That(exception.ObjectName)
                .IsEqualTo(nameof(NovaSharpVsCodeDebugServer))
                .ConfigureAwait(false);
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
    }
}
