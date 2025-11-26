namespace NovaSharp.Interpreter.Execution.VM
{
    using System;

    /// <summary>
    /// Describes metadata markers emitted alongside <see cref="OpCode.Meta"/> instructions.
    /// </summary>
    public enum OpCodeMetadataType
    {
        [Obsolete("Use a specific OpCodeMetadataType.", false)]
        Unknown = 0,
        ChunkEntrypoint = 1,
        FunctionEntrypoint = 2,
    }
}
