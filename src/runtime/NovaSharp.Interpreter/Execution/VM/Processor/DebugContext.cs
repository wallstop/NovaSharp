namespace NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using Debugging;

    internal sealed partial class Processor
    {
        private class DebugContext
        {
            public bool debuggerEnabled = true;
            public IDebugger debuggerAttached = null;
            public DebuggerAction.ActionType debuggerCurrentAction;
            public int debuggerCurrentActionTarget = -1;
            public SourceRef lastHlRef = null;
            public int exStackDepthAtStep = -1;
            public readonly List<SourceRef> breakPoints = new();
            public bool lineBasedBreakPoints = false;
        }
    }
}
