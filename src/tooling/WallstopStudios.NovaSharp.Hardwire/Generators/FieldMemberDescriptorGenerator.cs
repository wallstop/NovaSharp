namespace WallstopStudios.NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using WallstopStudios.NovaSharp.Hardwire.Generators.Base;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Generates descriptors for reflection-backed fields.
    /// </summary>
    internal sealed class FieldMemberDescriptorGenerator : AssignableMemberDescriptorGeneratorBase
    {
        /// <inheritdoc />
        public override string ManagedType => typeof(FieldMemberDescriptor).FullName;

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
