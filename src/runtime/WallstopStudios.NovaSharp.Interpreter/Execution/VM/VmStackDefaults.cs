namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    /// <summary>
    /// Initial VM stack sizes. The stacks grow geometrically when deeper code needs more space.
    /// </summary>
    internal static class VmStackDefaults
    {
        public const int ValueStackInitialCapacity = 512;
        public const int ExecutionStackInitialCapacity = 64;
    }
}
