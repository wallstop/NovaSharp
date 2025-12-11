namespace WallstopStudios.NovaSharp.Hardwire.Generators
{
    using System;
    using System.CodeDom;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Generates descriptors that expose a value type's parameterless constructor as __new.
    /// </summary>
    internal sealed class ValueTypeDefaultCtorMemberDescriptorGenerator : IHardwireGenerator
    {
        /// <inheritdoc />
        public string ManagedType => typeof(ValueTypeDefaultCtorMemberDescriptor).FullName;

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
