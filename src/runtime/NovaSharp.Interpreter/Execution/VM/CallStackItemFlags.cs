namespace NovaSharp.Interpreter.Execution.VM
{
    using System;

    [Flags]
    internal enum CallStackItemFlags
    {
        [Obsolete("Prefer explicit CallStackItemFlags combinations.", false)]
        None = 0,

        EntryPoint = 1 << 0,
        ResumeEntryPoint = EntryPoint | (1 << 1),
        CallEntryPoint = EntryPoint | (1 << 2),

        TailCall = 1 << 4,
        MethodCall = 1 << 5,
    }
}
