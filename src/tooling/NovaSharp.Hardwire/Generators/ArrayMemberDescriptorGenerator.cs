using System.CodeDom;
using NovaSharp.Hardwire.Utils;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Interop;
using NovaSharp.Interpreter.Interop.BasicDescriptors;

namespace NovaSharp.Hardwire.Generators
{
    public class ArrayMemberDescriptorGenerator : IHardwireGenerator
    {
        public string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.ArrayMemberDescriptor"; }
        }

        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generatorContext,
            CodeTypeMemberCollection members
        )
        {
            string className = "AIDX_" + Guid.NewGuid().ToString("N");
            string name = table.Get("name").String;
            bool setter = table.Get("setter").Boolean;

            CodeTypeDeclaration classCode = new(className);

            classCode.TypeAttributes =
                System.Reflection.TypeAttributes.NestedPrivate
                | System.Reflection.TypeAttributes.Sealed;

            classCode.BaseTypes.Add(typeof(ArrayMemberDescriptor));

            CodeConstructor ctor = new();
            ctor.Attributes = MemberAttributes.Assembly;
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
