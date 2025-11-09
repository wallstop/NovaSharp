using System.CodeDom;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

namespace NovaSharp.Hardwire.Generators
{
    public class StandardUserDataDescriptorGenerator : IHardwireGenerator
    {
        public string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.StandardUserDataDescriptor"; }
        }

        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generator,
            CodeTypeMemberCollection members
        )
        {
            string type = (string)table["$key"];
            string className = "TYPE_" + Guid.NewGuid().ToString("N");

            CodeTypeDeclaration classCode = new(className);

            classCode.Comments.Add(new CodeCommentStatement("Descriptor of " + type));

            classCode.StartDirectives.Add(
                new CodeRegionDirective(CodeRegionMode.Start, "Descriptor of " + type)
            );

            classCode.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

            classCode.TypeAttributes =
                System.Reflection.TypeAttributes.NestedPrivate
                | System.Reflection.TypeAttributes.Sealed;

            classCode.BaseTypes.Add(typeof(HardwiredUserDataDescriptor));

            CodeConstructor ctor = new();
            ctor.Attributes = MemberAttributes.Assembly;
            ctor.BaseConstructorArgs.Add(new CodeTypeOfExpression(type));

            classCode.Members.Add(ctor);

            generator.DispatchTablePairs(
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

            generator.DispatchTablePairs(
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
