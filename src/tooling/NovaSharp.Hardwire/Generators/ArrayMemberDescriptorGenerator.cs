namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using Utils;

    /// <summary>
    /// Generates hardwired descriptors for array members described in the Lua dump.
    /// </summary>
    public class ArrayMemberDescriptorGenerator : IHardwireGenerator
    {
        /// <inheritdoc />
        public string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors.ArrayMemberDescriptor";
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Emits a nested descriptor type tailored to the provided array member metadata.
        /// </summary>
        /// <param name="table">Dump table describing the member.</param>
        /// <param name="generatorContext">Context used for logging and shared metadata.</param>
        /// <param name="members">Collection receiving the generated type.</param>
        /// <returns>Expressions that instantiate the descriptor.</returns>
        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generatorContext,
            CodeTypeMemberCollection members
        )
        {
            string className = "AIDX_" + Guid.NewGuid().ToString("N");
            string name = table.Get("name").String;
            bool setter = table.Get("setter").Boolean;

            CodeTypeDeclaration classCode = new(className)
            {
                TypeAttributes =
                    System.Reflection.TypeAttributes.NestedPrivate
                    | System.Reflection.TypeAttributes.Sealed,
            };

            classCode.BaseTypes.Add(typeof(ArrayMemberDescriptor));

            CodeConstructor ctor = new() { Attributes = MemberAttributes.Assembly };
            classCode.Members.Add(ctor);

            ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(name));
            ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(setter));

            DynValue vparams = table.Get("params");

            if (vparams.Type == DataType.Table)
            {
                List<HardwireParameterDescriptor> paramDescs =
                    HardwireParameterDescriptor.LoadDescriptorsFromTable(vparams.Table);

                ctor.BaseConstructorArgs.Add(
                    new CodeArrayCreateExpression(
                        typeof(ParameterDescriptor),
                        paramDescs.Select(e => e.Expression).ToArray()
                    )
                );
            }

            members.Add(classCode);
            return new CodeExpression[] { new CodeObjectCreateExpression(className) };
        }
    }
}
