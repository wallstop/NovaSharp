namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using NovaSharp.Hardwire.Generators.Base;

    /// <summary>
    /// Generates descriptors for reflection-backed properties.
    /// </summary>
    internal sealed class PropertyMemberDescriptorGenerator
        : AssignableMemberDescriptorGeneratorBase
    {
        /// <inheritdoc />
        public override string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.PropertyMemberDescriptor";
            }
        }

        /// <inheritdoc />
        protected override CodeExpression GetMemberAccessExpression(
            CodeExpression thisObj,
            string name
        )
        {
            return new CodePropertyReferenceExpression(thisObj, name);
        }

        /// <inheritdoc />
        protected override string GetPrefix()
        {
            return "PROP";
        }
    }
}
