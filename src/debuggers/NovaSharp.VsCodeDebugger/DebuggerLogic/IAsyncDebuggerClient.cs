namespace NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Consumes events produced by <see cref="AsyncDebugger"/> so IDE hosts can stay in sync.
    /// </summary>
    internal interface IAsyncDebuggerClient
    {
        /// <summary>
        /// Signals that the runtime has paused and VS Code should present a stop notification.
        /// </summary>
        public void SendStopEvent();

        /// <summary>
        /// Notifies the client that a watch list changed.
        /// </summary>
        /// <param name="watchType">Watch category that requires refresh.</param>
        public void OnWatchesUpdated(WatchType watchType);

        /// <summary>
        /// Informs the client that the backing source code for a chunk changed or was materialized.
        /// </summary>
        /// <param name="sourceId">Identifier of the script chunk.</param>
        public void OnSourceCodeChanged(int sourceId);

        /// <summary>
        /// Indicates that the script finished executing.
        /// </summary>
        public void OnExecutionEnded();

        /// <summary>
        /// Reports a runtime exception encountered by the script.
        /// </summary>
        /// <param name="ex">Exception raised by the interpreter.</param>
        public void OnException(ScriptRuntimeException ex);

        /// <summary>
        /// Releases any resources held by the client because the debugger is shutting down.
        /// </summary>
        public void Unbind();
    }
}

#endif
