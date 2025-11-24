namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using NovaSharp.Hardwire.Generators.Base;

    /// <summary>
    /// Generates descriptors for reflection-backed fields.
    /// </summary>
    internal sealed class FieldMemberDescriptorGenerator : AssignableMemberDescriptorGeneratorBase
    {
        /// <inheritdoc />
        public override string ManagedType
        {
            get
            {
                return "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.FieldMemberDescriptor";
            }
        }

        /// <inheritdoc />
        protected override CodeExpression GetMemberAccessExpression(
            CodeExpression thisObj,
            string name
        )
        {
            return new CodeFieldReferenceExpression(thisObj, name);
        }

        /// <inheritdoc />
        protected override string GetPrefix()
        {
            return "FLDV";
        }
    }
}
