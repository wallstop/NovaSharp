namespace NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using Interpreter;
    using Interpreter.Debugging;

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
