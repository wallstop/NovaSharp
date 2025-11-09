using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Interop;
using NovaSharp.Interpreter.Interop.BasicDescriptors;
using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

namespace NovaSharp.Hardwire.Generators
{
    class FieldMemberDescriptorGenerator : AssignableMemberDescriptorGeneratorBase
    {
        public override string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.FieldMemberDescriptor"; }
        }

        protected override CodeExpression GetMemberAccessExpression(
            CodeExpression thisObj,
            string name
        )
        {
            return new CodeFieldReferenceExpression(thisObj, name);
        }

        protected override string GetPrefix()
        {
            return "FLDV";
        }
    }
}
