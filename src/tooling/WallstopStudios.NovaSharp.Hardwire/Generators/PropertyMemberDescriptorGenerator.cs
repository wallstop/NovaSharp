namespace WallstopStudios.NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using WallstopStudios.NovaSharp.Hardwire.Generators.Base;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Generates descriptors for reflection-backed properties.
    /// </summary>
    internal sealed class PropertyMemberDescriptorGenerator
        : AssignableMemberDescriptorGeneratorBase
    {
        /// <inheritdoc />
        public override string ManagedType => typeof(PropertyMemberDescriptor).FullName;

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
