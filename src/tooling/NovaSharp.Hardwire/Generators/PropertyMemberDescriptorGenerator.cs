namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;

    internal sealed class PropertyMemberDescriptorGenerator
        : AssignableMemberDescriptorGeneratorBase
    {
        public override string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.PropertyMemberDescriptor";
            }
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
