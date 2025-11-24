namespace NovaSharp.Hardwire.Generators
{
    using System.CodeDom;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Placeholder generator used when no concrete generator exists for a managed type.
    /// </summary>
    internal sealed class NullGenerator : IHardwireGenerator
    {
        public NullGenerator()
        {
            ManagedType = "";
        }

        public NullGenerator(string type)
        {
            ManagedType = type;
        }

        /// <inheritdoc />
        public string ManagedType { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Emits an error to highlight the missing generator and returns no code.
        /// </summary>
        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generator,
            CodeTypeMemberCollection members
        )
        {
            generator.Error("Missing code generator for '{0}'.", ManagedType);

            return new CodeExpression[0];
        }
    }
}
