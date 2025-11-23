namespace NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    /// <summary>
    /// Sentinel used by hardwire generators to represent an unspecified default argument value.
    /// </summary>
    public sealed class DefaultValue
    {
        /// <summary>
        /// Shared singleton instance since the sentinel carries no state.
        /// </summary>
        public static readonly DefaultValue Instance = new();
    }
}
