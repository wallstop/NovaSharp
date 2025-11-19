namespace NovaSharp.Interpreter.Debugging
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Class providing services specific to debugger implementations.
    /// </summary>
    /// <seealso cref="NovaSharp.Interpreter.IScriptPrivateResource" />
    public sealed class DebugService : IScriptPrivateResource
    {
        private readonly Processor _processor;

        internal DebugService(Script script, Processor processor)
        {
            OwnerScript = script;
            _processor = processor;
        }

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        /// <value>
        /// The script owning this resource.
        /// </value>
        public Script OwnerScript { get; private set; }

        /// <summary>
        /// Resets the break points for a given file. Supports only line-based breakpoints.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="lines">The lines.</param>
        /// <returns>The lines for which breakpoints have been set</returns>
        public HashSet<int> ResetBreakpoints(SourceCode src, HashSet<int> lines)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            return _processor.ResetBreakpoints(src, lines);
        }
    }
}
