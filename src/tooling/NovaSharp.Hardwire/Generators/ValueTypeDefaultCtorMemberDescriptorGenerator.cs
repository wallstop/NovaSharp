namespace NovaSharp.Hardwire.Generators
{
    using System;
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
            HardwireCodeGenerationContext generatorContext,
            CodeTypeMemberCollection members
        )
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (generatorContext == null)
            {
                throw new ArgumentNullException(nameof(generatorContext));
            }

            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

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

            return mgen.Generate(mt, generatorContext, members);
        }
    }
}
