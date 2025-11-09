using System;
using NovaSharp.Interpreter.Interop.BasicDescriptors;

namespace NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    public abstract class HardwiredUserDataDescriptor : DispatchingUserDataDescriptor
    {
        protected HardwiredUserDataDescriptor(Type T)
            : base(T, "::hardwired::" + T.Name) { }
    }
}
