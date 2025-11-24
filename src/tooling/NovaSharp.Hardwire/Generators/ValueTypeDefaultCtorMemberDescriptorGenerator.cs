namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Generates descriptors that expose a value type's parameterless constructor as __new.
    /// </summary>
    internal sealed class ValueTypeDefaultCtorMemberDescriptorGenerator : IHardwireGenerator
    {
        /// <inheritdoc />
        public string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.ValueTypeDefaultCtorMemberDescriptor";
            }
        }

        /// <inheritdoc />
        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generator,
            CodeTypeMemberCollection members
        )
        {
            MethodMemberDescriptorGenerator mgen = new("VTDC");

            Table mt = new(null)
            {
                ["params"] = new Table(null),
                ["name"] = "__new",
                ["type"] = table["type"],
                ["ctor"] = true,
                ["extension"] = false,
                ["decltype"] = table["type"],
                ["ret"] = table["type"],
                ["special"] = false,
            };

            return mgen.Generate(mt, generator, members);
        }
    }
}
