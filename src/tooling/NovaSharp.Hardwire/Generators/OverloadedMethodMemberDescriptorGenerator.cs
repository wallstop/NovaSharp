namespace NovaSharp.Hardwire.Generators
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Generates descriptors that aggregate multiple overload descriptors under a single method name.
    /// </summary>
    internal sealed class OverloadedMethodMemberDescriptorGenerator : IHardwireGenerator
    {
        /// <inheritdoc />
        public string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.OverloadedMethodMemberDescriptor";
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Builds an <see cref="OverloadedMethodMemberDescriptor"/> that wraps the generated overload descriptors.
        /// </summary>
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

            List<CodeExpression> initializers = new();

            generatorContext.DispatchTablePairs(
                table.Get("overloads").Table,
                members,
                exp =>
                {
                    initializers.Add(exp);
                }
            );

            CodePrimitiveExpression name = new((table["name"] as string));
            CodeTypeOfExpression type = new(table["decltype"] as string);

            CodeArrayCreateExpression array = new(
                typeof(IOverloadableMemberDescriptor),
                initializers.ToArray()
            );

            return new CodeExpression[]
            {
                new CodeObjectCreateExpression(
                    typeof(OverloadedMethodMemberDescriptor),
                    name,
                    type,
                    array
                ),
            };
        }
    }
}
