using System.CodeDom;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Interop;
using NovaSharp.Interpreter.Interop.BasicDescriptors;

namespace NovaSharp.Hardwire.Generators
{
    class OverloadedMethodMemberDescriptorGenerator : IHardwireGenerator
    {
        public string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.OverloadedMethodMemberDescriptor"; }
        }

        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generator,
            CodeTypeMemberCollection members
        )
        {
            List<CodeExpression> initializers = new();

            generator.DispatchTablePairs(
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
