namespace WallstopStudios.NovaSharp.Interpreter.Debugging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;

    /// <summary>
    /// Wrapper for a debugger initiated action
    /// </summary>
    public class DebuggerAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public enum ActionType
        {
            /// <summary>
            /// Step-in at the bytecode level
            /// </summary>
            [Obsolete("Use a specific Debugger action type.", false)]
            Unknown = 0,
            ByteCodeStepIn = 1,

            /// <summary>
            /// Step-over at the bytecode level
            /// </summary>
            ByteCodeStepOver = 2,

            /// <summary>
            /// Step-out at the bytecode level
            /// </summary>
            ByteCodeStepOut = 3,

            /// <summary>
            /// Step-in at the source level
            /// </summary>
            StepIn = 4,

            /// <summary>
            /// Step-over at the source level
            /// </summary>
            StepOver = 5,

            /// <summary>
            /// Step-out at the source level
            /// </summary>
            StepOut = 6,

            /// <summary>
            /// Continue execution "freely"
            /// </summary>
            Run = 7,

            /// <summary>
            /// Toggles breakpoint
            /// </summary>
            ToggleBreakpoint = 8,

            /// <summary>
            /// Sets a breakpoint
            /// </summary>
            SetBreakpoint = 9,

            /// <summary>
            /// Clears a breakpoint
            /// </summary>
            ClearBreakpoint = 10,

            /// <summary>
            /// Reset all breakpoints
            /// </summary>
            ResetBreakpoints = 11,

            /// <summary>
            /// Refresh the data
            /// </summary>
            Refresh = 12,

            /// <summary>
            /// Hard refresh of data
            /// </summary>
            HardRefresh = 13,

            /// <summary>
            /// No action
            /// </summary>
            None = 14,
        }

        /// <summary>
        /// The type of action
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Gets the time stamp UTC of this action
        /// </summary>
        public DateTime TimeStampUtc { get; private set; }

        /// <summary>
        /// Gets or sets the source identifier this action refers to. <see cref="Script.GetSourceCode"/>
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// Gets or sets the source line this action refers to.
        /// </summary>
        public int SourceLine { get; set; }

        /// <summary>
        /// Gets or sets the source column this action refers to.
        /// </summary>
        public int SourceCol { get; set; }

        /// <summary>
        /// Gets or sets the lines. This is used for the ResetBreakpoints and sets line-based bps only.
        /// </summary>
        private int[] _lines = Array.Empty<int>();

        public IReadOnlyList<int> Lines
        {
            get => _lines;
            set => _lines = value is null ? Array.Empty<int>() : value.ToArray();
        }

        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebuggerAction"/> class.
        /// </summary>
        public DebuggerAction()
            : this(SystemTimeProvider.Instance) { }

        public DebuggerAction(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            TimeStampUtc = _timeProvider.GetUtcNow().UtcDateTime;
        }

        /// <summary>
        /// Gets the age of this debugger action
        /// </summary>
        public TimeSpan Age => _timeProvider.GetUtcNow().UtcDateTime - TimeStampUtc;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (
                Action == ActionType.ToggleBreakpoint
                || Action == ActionType.SetBreakpoint
                || Action == ActionType.ClearBreakpoint
            )
            {
                return $"{Action} {SourceId}:({SourceLine},{SourceCol})";
            }
            else
            {
                return Action.ToString();
            }
        }
    }
}
