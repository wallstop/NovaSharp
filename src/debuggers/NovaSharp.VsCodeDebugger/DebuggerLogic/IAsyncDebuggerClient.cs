#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Debugging;

namespace NovaSharp.VsCodeDebugger.DebuggerLogic
{
    internal interface IAsyncDebuggerClient
    {
        void SendStopEvent();
        void OnWatchesUpdated(WatchType watchType);
        void OnSourceCodeChanged(int sourceID);
        void OnExecutionEnded();
        void OnException(ScriptRuntimeException ex);
        void Unbind();
    }
}

#endif
