namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents a resource served by the debugger HTTP host, including static binaries,
    /// textual payloads, or callbacks that generate responses on demand.
    /// </summary>
    public class HttpResource
    {
        /// <summary>
        /// Gets the MIME-oriented classification of the resource.
        /// </summary>
        public HttpResourceType Type { get; private set; }

        /// <summary>
        /// Gets the raw payload bytes for static resources.
        /// </summary>
        public ReadOnlyMemory<byte> Data { get; private set; }

        /// <summary>
        /// Gets the callback executed to build a dynamic response when <see cref="Type"/> is
        /// <see cref="HttpResourceType.Callback"/>.
        /// </summary>
        public Func<Dictionary<string, string>, HttpResource> Callback { get; private set; }

        private HttpResource() { }

        /// <summary>
        /// Creates a binary resource from the provided byte array.
        /// </summary>
        /// <param name="type">Resource classification used to pick the response MIME type.</param>
        /// <param name="data">Payload bytes written directly to the HTTP response stream.</param>
        /// <returns>A new <see cref="HttpResource"/> that always returns <paramref name="data"/>.</returns>
        public static HttpResource CreateBinary(HttpResourceType type, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return new HttpResource() { Type = type, Data = new ReadOnlyMemory<byte>(data) };
        }

        /// <summary>
        /// Creates a binary resource by decoding a Base64 string.
        /// </summary>
        /// <param name="type">Resource classification used to pick the response MIME type.</param>
        /// <param name="base64data">Base64 encoded payload.</param>
        /// <returns>A <see cref="HttpResource"/> representing the decoded bytes.</returns>
        public static HttpResource CreateBinary(HttpResourceType type, string base64data)
        {
            if (base64data == null)
            {
                throw new ArgumentNullException(nameof(base64data));
            }

            return new HttpResource()
            {
                Type = type,
                Data = new ReadOnlyMemory<byte>(Convert.FromBase64String(base64data)),
            };
        }

        /// <summary>
        /// Creates a UTF-8 encoded textual resource.
        /// </summary>
        /// <param name="type">Resource classification used to pick the response MIME type.</param>
        /// <param name="data">Plain-text payload that will be UTF-8 encoded.</param>
        /// <returns>A <see cref="HttpResource"/> that emits the specified string.</returns>
        public static HttpResource CreateText(HttpResourceType type, string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return new HttpResource()
            {
                Type = type,
                Data = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(data)),
            };
        }

        /// <summary>
        /// Creates a callback-backed resource that can compute its response per request.
        /// </summary>
        /// <param name="callback">
        /// Delegate invoked with the query-string dictionary and expected to return the final resource.
        /// </param>
        /// <returns>
        /// A <see cref="HttpResource"/> wrapper that executes <paramref name="callback"/> each time
        /// the endpoint is requested.
        /// </returns>
        public static HttpResource CreateCallback(
            Func<Dictionary<string, string>, HttpResource> callback
        )
        {
            return new HttpResource() { Type = HttpResourceType.Callback, Callback = callback };
        }

        /// <summary>
        /// Gets the HTTP <c>Content-Type</c> header value associated with <see cref="Type"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a callback type is queried for a content type.
        /// </exception>
        public string ContentTypeString
        {
            get
            {
                switch (Type)
                {
                    case HttpResourceType.PlainText:
                        return "text/plain";
                    case HttpResourceType.Html:
                        return "text/html";
                    case HttpResourceType.Json:
                        return "application/json";
                    case HttpResourceType.Xml:
                        return "application/xml";
                    case HttpResourceType.Jpeg:
                        return "image/jpeg";
                    case HttpResourceType.Png:
                        return "image/png";
                    case HttpResourceType.Binary:
                        return "application/octet-stream";
                    case HttpResourceType.Javascript:
                        return "application/javascript";
                    case HttpResourceType.Css:
                        return "text/css";
                    case HttpResourceType.Callback:
                    default:
                        throw new InvalidOperationException(
                            FormattableString.Invariant(
                                $"HttpResourceType value {Type} does not have a content type string."
                            )
                        );
                }
            }
        }
    }
}
