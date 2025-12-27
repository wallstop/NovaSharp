namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Captures the lifetime of a lexical block after compilation so the VM knows when locals enter/leave scope.
    /// </summary>
    internal class RuntimeScopeBlock
    {
        /// <summary>
        /// Gets or sets the first stack slot that belongs to the block.
        /// </summary>
        public int From { get; internal set; }

        /// <summary>
        /// Gets or sets the index of the first slot that no longer belongs to the block.
        /// </summary>
        public int To { get; internal set; }

        /// <summary>
        /// Gets or sets the last slot that is still part of the block (inclusive).
        /// </summary>
        public int ToInclusive { get; internal set; }

        /// <summary>
        /// Gets or sets the locals that must run <c>__close</c> when the block exits (Lua 5.4 §3.3.8).
        /// </summary>
        public SymbolRef[] ToBeClosed { get; internal set; } = Array.Empty<SymbolRef>();

        /// <summary>
        /// Returns a human-readable description of the slot range covered by the block.
        /// </summary>
        public override string ToString()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append("ScopeBlock : ");
            sb.Append(From);
            sb.Append(" -> ");
            sb.Append(To);
            sb.Append(" --> ");
            sb.Append(ToInclusive);
            return sb.ToString();
        }
    }
}
