using System.CodeDom;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Interop;
using NovaSharp.Interpreter.Serialization;

namespace NovaSharp.Hardwire.Generators
{
    public class DynValueMemberDescriptorGenerator : IHardwireGenerator
    {
        public string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.DynValueMemberDescriptor"; }
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

            CodeTypeDeclaration classCode = new(className);

            classCode.TypeAttributes =
                System.Reflection.TypeAttributes.NestedPrivate
                | System.Reflection.TypeAttributes.Sealed;

            classCode.BaseTypes.Add(typeof(DynValueMemberDescriptor));

            CodeConstructor ctor = new();
            ctor.Attributes = MemberAttributes.Assembly;
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

                CodeMemberProperty p = new();
                p.Name = "Value";
                p.Type = new CodeTypeReference(typeof(DynValue));
                p.Attributes = MemberAttributes.Override | MemberAttributes.Public;
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
