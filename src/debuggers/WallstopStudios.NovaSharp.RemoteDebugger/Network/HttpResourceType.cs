namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    /// <summary>
    /// Enumerates the supported HTTP payload categories served by the debugger web host.
    /// The value determines the <c>Content-Type</c> header applied to each response.
    /// </summary>
    public enum HttpResourceType
    {
        PlainText,
        Html,
        Xml,
        Json,
        Jpeg,
        Png,
        Binary,
        Callback,
        Css,
        Javascript,
    }
}
