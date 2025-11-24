namespace NovaSharp.Hardwire.Languages
{
    using System.CodeDom;
    using System.CodeDom.Compiler;

    /// <summary>
    /// Abstraction over the language-specific helpers needed to emit hardwired descriptors.
    /// </summary>
    public abstract class HardwireCodeGenerationLanguage
    {
        /// <summary>
        /// Gets a code-generation language targeting C#.
        /// </summary>
        public static HardwireCodeGenerationLanguage CSharp
        {
            get { return new CSharpHardwireCodeGenerationLanguage(); }
        }

        /// <summary>
        /// Gets a code-generation language targeting VB.NET.
        /// </summary>
        public static HardwireCodeGenerationLanguage Vb
        {
            get { return new VbHardwireCodeGenerationLanguage(); }
        }

        /// <summary>
        /// Gets the display name of the language.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the <see cref="CodeDomProvider"/> that emits code in this language.
        /// </summary>
        public abstract CodeDomProvider CodeDomProvider { get; }

        /// <summary>
        /// Builds a unary plus expression if the language supports it.
        /// </summary>
        public abstract CodeExpression UnaryPlus(CodeExpression arg);

        /// <summary>Builds a ++ expression.</summary>
        public abstract CodeExpression UnaryIncrement(CodeExpression arg);

        /// <summary>Builds a -- expression.</summary>
        public abstract CodeExpression UnaryDecrement(CodeExpression arg);

        /// <summary>Builds a unary negation expression.</summary>
        public abstract CodeExpression UnaryNegation(CodeExpression arg);

        /// <summary>Builds a logical NOT expression.</summary>
        public abstract CodeExpression UnaryLogicalNot(CodeExpression arg);

        /// <summary>Builds a bitwise complement expression.</summary>
        public abstract CodeExpression UnaryOneComplement(CodeExpression arg);

        /// <summary>Builds a bitwise XOR expression.</summary>
        public abstract CodeExpression BinaryXor(CodeExpression arg1, CodeExpression arg2);

        /// <summary>
        /// Creates a multidimensional array instantiation expression (language-specific syntax).
        /// </summary>
        public abstract CodeExpression CreateMultidimensionalArray(
            string type,
            CodeExpression[] args
        );

        /// <summary>
        /// Returns language-specific header comments inserted at the top of the generated file.
        /// </summary>
        public abstract string[] GetInitialComment();

        protected string ExpressionToString(CodeExpression exp)
        {
            using StringWriter sourceWriter = new();
            CodeDomProvider.GenerateCodeFromExpression(
                exp,
                sourceWriter,
                new CodeGeneratorOptions()
            );
            return sourceWriter.ToString();
        }

        protected CodeExpression SnippetExpression(string format, params CodeExpression[] args)
        {
            string fmt = "(" + format + ")";
            string res = string.Format(
                fmt,
                args.Select(e => ExpressionToString(e)).OfType<object>().ToArray()
            );
            return new CodeSnippetExpression(res);
        }
    }
}
