namespace NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using Debugging;

    /// <content>
    /// Houses debugger-related helpers shared across <see cref="Processor"/> partials.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Tracks debugger state for a processor instance.
        /// </summary>
        private class DebugContext
        {
            /// <summary>
            /// Gets or sets a value indicating whether debugging is enabled for this processor.
            /// </summary>
            public bool DebuggerEnabled = true;

            /// <summary>
            /// Gets or sets the debugger currently attached to the processor.
            /// </summary>
            public IDebugger DebuggerAttached;

            /// <summary>
            /// Gets or sets the pending debugger action (step, continue, break).
            /// </summary>
            public DebuggerAction.ActionType DebuggerCurrentAction;

            /// <summary>
            /// Gets or sets the bytecode target for the current debugger action.
            /// </summary>
            public int DebuggerCurrentActionTarget = -1;

            /// <summary>
            /// Gets or sets the last highlighted source reference.
            /// </summary>
            public SourceRef LastHlRef;

            /// <summary>
            /// Gets or sets the execution stack depth at which a step action was scheduled.
            /// </summary>
            public int ExecutionStackDepthAtStep = -1;

            /// <summary>
            /// Gets the list of breakpoints evaluated by the debugger.
            /// </summary>
            public readonly List<SourceRef> BreakPoints = new();

            /// <summary>
            /// Gets or sets a value indicating whether breakpoints are matched on line numbers only.
            /// </summary>
            public bool LineBasedBreakPoints;
        }
    }
}
