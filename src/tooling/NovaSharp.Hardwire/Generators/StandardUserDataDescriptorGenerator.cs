namespace NovaSharp.Hardwire.Generators
{
    using System;
    using System.CodeDom;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

    /// <summary>
    /// Generates hardwired descriptors for standard userdata types.
    /// </summary>
    public class StandardUserDataDescriptorGenerator : IHardwireGenerator
    {
        /// <inheritdoc />
        public string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.StandardUserDataDescriptor";
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Builds a descriptor type for the userdata described in <paramref name="table"/>, wiring both members and metamembers.
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

            string type = (string)table["$key"];
            string className = "TYPE_" + Guid.NewGuid().ToString("N");

            CodeTypeDeclaration classCode = new(className);

            classCode.Comments.Add(new CodeCommentStatement("Descriptor of " + type));

            classCode.TypeAttributes =
                System.Reflection.TypeAttributes.NestedPrivate
                | System.Reflection.TypeAttributes.Sealed;

            classCode.BaseTypes.Add(typeof(HardwiredUserDataDescriptor));

            CodeConstructor ctor = new() { Attributes = MemberAttributes.Assembly };
            ctor.BaseConstructorArgs.Add(new CodeTypeOfExpression(type));

            classCode.Members.Add(ctor);

            generatorContext.DispatchTablePairs(
                table.Get("members").Table,
                classCode.Members,
                (key, exp) =>
                {
                    CodePrimitiveExpression mname = new(key);

                    ctor.Statements.Add(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "AddMember",
                            mname,
                            exp
                        )
                    );
                }
            );

            generatorContext.DispatchTablePairs(
                table.Get("metamembers").Table,
                classCode.Members,
                (key, exp) =>
                {
                    CodePrimitiveExpression mname = new(key);

                    ctor.Statements.Add(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "AddMetaMember",
                            mname,
                            exp
                        )
                    );
                }
            );

            members.Add(classCode);

            return new CodeExpression[] { new CodeObjectCreateExpression(className) };
        }
    }
}
