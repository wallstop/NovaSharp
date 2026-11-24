namespace WallstopStudios.NovaSharp.Interpreter.Compatibility
{
    using System;

    /// <summary>
    /// Roots a reflection-bound method in the Unity linker without a UnityEngine dependency.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class PreserveAttribute : Attribute { }
}
