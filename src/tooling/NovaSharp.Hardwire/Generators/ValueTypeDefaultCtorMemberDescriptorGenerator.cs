using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Interop.BasicDescriptors;
using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

namespace NovaSharp.Hardwire.Generators
{
    class ValueTypeDefaultCtorMemberDescriptorGenerator : IHardwireGenerator
    {
        public string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.ValueTypeDefaultCtorMemberDescriptor"; }
        }

        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generator,
            CodeTypeMemberCollection members
        )
        {
            MethodMemberDescriptorGenerator mgen = new MethodMemberDescriptorGenerator("VTDC");

            Table mt = new Table(null);

            mt["params"] = new Table(null);
            mt["name"] = "__new";
            mt["type"] = table["type"];
            mt["ctor"] = true;
            mt["extension"] = false;
            mt["decltype"] = table["type"];
            mt["ret"] = table["type"];
            mt["special"] = false;

            return mgen.Generate(mt, generator, members);
        }
    }
}
