namespace WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;

    /// <summary>
    /// Base class for hardwired member descriptors emitted by generators (reflection-free).
    /// </summary>
    public abstract class HardwiredMemberDescriptor : IMemberDescriptor
    {
        /// <summary>
        /// Gets the CLR type of the member being exposed to Lua.
        /// </summary>
        public Type MemberType { get; private set; }

        protected HardwiredMemberDescriptor(
            Type memberType,
            string name,
            bool isStatic,
            MemberDescriptorAccess access
        )
        {
            IsStatic = isStatic;
            Name = name;
            MemberAccess = access;
            MemberType = memberType;
        }

        /// <summary>
        /// Gets a value indicating whether the member is static.
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Gets the Lua-visible name of the member.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the supported access flags for the member.
        /// </summary>
        public MemberDescriptorAccess MemberAccess { get; private set; }

        /// <summary>
        /// Reads the member value, enforcing access rules, and converts it to a <see cref="DynValue"/>.
        /// </summary>
        public DynValue GetValue(Script script, object obj)
        {
            this.CheckAccess(MemberDescriptorAccess.CanRead, obj);
            object result = GetValueCore(script, obj);
            return ClrToScriptConversions.ObjectToDynValue(script, result);
        }

        /// <summary>
        /// Writes the member value after converting from a <see cref="DynValue"/>.
        /// </summary>
        public void SetValue(Script script, object obj, DynValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.CheckAccess(MemberDescriptorAccess.CanWrite, obj);
            object v = ScriptToClrConversions.DynValueToObjectOfType(
                value,
                MemberType,
                null,
                false
            );
            SetValueCore(script, obj, v);
        }

        /// <summary>
        /// Override to supply the actual getter implementation for the descriptor.
        /// </summary>
        protected virtual object GetValueCore(Script script, object obj)
        {
            throw new InvalidOperationException(
                "GetValue on write-only hardwired descriptor " + Name
            );
        }

        /// <summary>
        /// Override to supply the actual setter implementation for the descriptor.
        /// </summary>
        protected virtual void SetValueCore(Script script, object obj, object value)
        {
            throw new InvalidOperationException(
                "SetValue on read-only hardwired descriptor " + Name
            );
        }
    }
}
