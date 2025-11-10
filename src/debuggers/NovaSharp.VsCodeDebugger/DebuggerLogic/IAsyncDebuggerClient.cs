namespace NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;

    internal interface IAsyncDebuggerClient
    {
        public void SendStopEvent();
        public void OnWatchesUpdated(WatchType watchType);
        public void OnSourceCodeChanged(int sourceId);
        public void OnExecutionEnded();
        public void OnException(ScriptRuntimeException ex);
        public void Unbind();
    }
}

#endif
