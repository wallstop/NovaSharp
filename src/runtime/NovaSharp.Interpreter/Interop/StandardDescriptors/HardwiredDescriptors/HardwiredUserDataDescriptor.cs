namespace NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;

    /// <summary>
    /// Base descriptor used by the hardwired generator to expose statically emitted members.
    /// </summary>
    public abstract class HardwiredUserDataDescriptor : DispatchingUserDataDescriptor
    {
        protected HardwiredUserDataDescriptor(Type t)
            : base(t ?? throw new ArgumentNullException(nameof(t)), "::hardwired::" + t.Name) { }
    }
}
