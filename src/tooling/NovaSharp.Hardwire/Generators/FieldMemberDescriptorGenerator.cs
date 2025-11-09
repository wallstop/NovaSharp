using System.CodeDom;

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
