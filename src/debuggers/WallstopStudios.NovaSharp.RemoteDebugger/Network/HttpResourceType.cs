namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;

    /// <summary>
    /// Enumerates the supported HTTP payload categories served by the debugger web host.
    /// The value determines the <c>Content-Type</c> header applied to each response.
    /// </summary>
    public enum HttpResourceType
    {
        /// <summary>
        /// Legacy placeholder; prefer an explicit resource type.
        /// </summary>
        [Obsolete("Use a specific HttpResourceType.", false)]
        Unknown = 0,
        PlainText = 1,
        Html = 2,
        Xml = 3,
        Json = 4,
        Jpeg = 5,
        Png = 6,
        Binary = 7,
        Callback = 8,
        Css = 9,
        Javascript = 10,
    }
}
