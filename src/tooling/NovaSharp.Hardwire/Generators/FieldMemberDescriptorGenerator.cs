namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;

    internal class FieldMemberDescriptorGenerator : AssignableMemberDescriptorGeneratorBase
    {
        public override string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.FieldMemberDescriptor";
            }
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
