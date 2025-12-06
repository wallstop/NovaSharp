namespace WallstopStudios.NovaSharp.Hardwire
{
    /// <summary>
    /// Logging abstraction consumed by the hardwire generator while emitting source code.
    /// </summary>
    public interface ICodeGenerationLogger
    {
        /// <summary>
        /// Logs a fatal code generation error.
        /// </summary>
        public void LogError(string message);

        /// <summary>
        /// Logs a warning while keeping generation in progress.
        /// </summary>
        public void LogWarning(string message);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public void LogMinor(string message);
    }
}
