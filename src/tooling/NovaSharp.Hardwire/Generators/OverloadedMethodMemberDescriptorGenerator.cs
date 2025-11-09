using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            List<CodeExpression> initializers = new List<CodeExpression>();

            generator.DispatchTablePairs(
                table.Get("overloads").Table,
                members,
                exp =>
                {
                    initializers.Add(exp);
                }
            );

            var name = new CodePrimitiveExpression((table["name"] as string));
            var type = new CodeTypeOfExpression(table["decltype"] as string);

            var array = new CodeArrayCreateExpression(
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
