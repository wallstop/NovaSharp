using System.CodeDom;

namespace NovaSharp.Hardwire.Generators
{
    class PropertyMemberDescriptorGenerator : AssignableMemberDescriptorGeneratorBase
    {
        public override string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.PropertyMemberDescriptor"; }
        }

        protected override CodeExpression GetMemberAccessExpression(
            CodeExpression thisObj,
            string name
        )
        {
            return new CodePropertyReferenceExpression(thisObj, name);
        }

        protected override string GetPrefix()
        {
            return "PROP";
        }
    }
}
