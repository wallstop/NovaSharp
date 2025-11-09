namespace NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    using System;
    using BasicDescriptors;

    public abstract class HardwiredUserDataDescriptor : DispatchingUserDataDescriptor
    {
        protected HardwiredUserDataDescriptor(Type t)
            : base(t, "::hardwired::" + t.Name) { }
    }
}
