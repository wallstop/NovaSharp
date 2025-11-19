namespace NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using Debugging;

    internal sealed partial class Processor
    {
        private class DebugContext
        {
            public bool DebuggerEnabled = true;
            public IDebugger DebuggerAttached;
            public DebuggerAction.ActionType DebuggerCurrentAction;
            public int DebuggerCurrentActionTarget = -1;
            public SourceRef LastHlRef;
            public int ExecutionStackDepthAtStep = -1;
            public readonly List<SourceRef> BreakPoints = new();
            public bool LineBasedBreakPoints;
        }
    }
}
