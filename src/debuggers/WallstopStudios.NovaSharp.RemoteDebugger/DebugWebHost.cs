namespace WallstopStudios.NovaSharp.RemoteDebugger
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Network;

    /// <summary>
    /// Serves the remote debugger HTML, SWF, and JavaScript assets over HTTP so the client UI
    /// can connect to a running script instance.
    /// </summary>
    public class DebugWebHost : HttpServer
    {
        public DebugWebHost(int port, Utf8TcpServerOptions options)
            : base(port, options)
        {
            RegisterEmbeddedResource("Main.html", HttpResourceType.Html, "Debugger");
            RegisterEmbeddedResource("Main.swf", HttpResourceType.Binary);
            RegisterEmbeddedResource("playerProductInstall.swf", HttpResourceType.Binary);
            RegisterEmbeddedResource("swfobject.js", HttpResourceType.PlainText);

            RegisterEmbeddedResource("bootstrap.min.css", HttpResourceType.Css);
            RegisterEmbeddedResource("theme.css", HttpResourceType.Css);
            RegisterEmbeddedResource("NovaSharpdbg.png", HttpResourceType.Png);
            RegisterEmbeddedResource("bootstrap.min.js", HttpResourceType.Javascript);
            RegisterEmbeddedResource("jquery.min.js", HttpResourceType.Javascript);
        }

        private HttpResource RegisterEmbeddedResource(
            string resourceName,
            HttpResourceType type,
            string urlName = null
        )
        {
            urlName = urlName ?? resourceName;

            byte[] data = GetResourceData(resourceName);

            HttpResource r = HttpResource.CreateBinary(type, data);
            RegisterResource("/" + urlName, r);
            RegisterResource(urlName, r);
            return r;
        }

        private static byte[] GetResourceData(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream stream =
                assembly.GetManifestResourceStream(
                    "WallstopStudios.NovaSharp.RemoteDebugger.Resources." + resourceName
                )
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{resourceName}' could not be located."
                );

            byte[] data = new byte[stream.Length];
            int offset = 0;
            while (offset < data.Length)
            {
                int read = stream.Read(data, offset, data.Length - offset);
                if (read == 0)
                {
                    throw new EndOfStreamException(
                        $"Unexpected end of stream while reading '{resourceName}'."
                    );
                }

                offset += read;
            }

            return data;
        }

        /// <summary>
        /// Retrieves the HTML template used for the debugger selection jump page.
        /// </summary>
        /// <returns>UTF-8 decoded HTML string for <c>JumpPage.html</c>.</returns>
        public static string GetJumpPageText()
        {
            byte[] data = GetResourceData("JumpPage.html");
            return Encoding.UTF8.GetString(data);
        }
    }
}
