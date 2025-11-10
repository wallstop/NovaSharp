namespace NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    using System;
    using BasicDescriptors;
    using NovaSharp.Interpreter.DataTypes;

    public abstract class HardwiredUserDataDescriptor : DispatchingUserDataDescriptor
    {
        protected HardwiredUserDataDescriptor(Type t)
            : base(t, "::hardwired::" + t.Name) { }
    }
}
