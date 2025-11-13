namespace NovaSharp.Interpreter.Execution.VM
{
    using System;

    public enum OpCodeMetadataType
    {
        [Obsolete("Use a specific OpCodeMetadataType.", false)]
        Unknown = 0,
        ChunkEntrypoint = 1,
        FunctionEntrypoint = 2,
    }
}
