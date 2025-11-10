namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Serialization;

    public class DynValueMemberDescriptorGenerator : IHardwireGenerator
    {
        public string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors.DynValueMemberDescriptor";
            }
        }

        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generatorContext,
            CodeTypeMemberCollection members
        )
        {
            string className = "DVAL_" + Guid.NewGuid().ToString("N");
            DynValue kval = table.Get("value");

            DynValue vtype = table.Get("type");
            DynValue vstaticType = table.Get("staticType");

            string type = (vtype.Type == DataType.String) ? vtype.String : null;
            string staticType = (vstaticType.Type == DataType.String) ? vstaticType.String : null;

            CodeTypeDeclaration classCode = new(className)
            {
                TypeAttributes =
                    System.Reflection.TypeAttributes.NestedPrivate
                    | System.Reflection.TypeAttributes.Sealed,
            };

            classCode.BaseTypes.Add(typeof(DynValueMemberDescriptor));

            CodeConstructor ctor = new() { Attributes = MemberAttributes.Assembly };
            classCode.Members.Add(ctor);

            if (type == null)
            {
                Table tbl = new(null);
                tbl.Set(1, kval);
                string str = tbl.Serialize();

                ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(table.Get("name").String));
                ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(str));
            }
            else if (type == "userdata")
            {
                ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(table.Get("name").String));

                CodeMemberProperty p = new()
                {
                    Name = "Value",
                    Type = new CodeTypeReference(typeof(DynValue)),
                    Attributes = MemberAttributes.Override | MemberAttributes.Public,
                };
                p.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(UserData)),
                            "CreateStatic",
                            new CodeTypeOfExpression(staticType)
                        )
                    )
                );

                classCode.Members.Add(p);
            }

            members.Add(classCode);
            return new CodeExpression[] { new CodeObjectCreateExpression(className) };
        }
    }
}
