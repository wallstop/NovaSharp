namespace NovaSharp.Hardwire.Languages
{
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Linq;

    /// <summary>
    /// Language helper that emits C# source for hardwired descriptors.
    /// </summary>
    public class CSharpHardwireCodeGenerationLanguage : HardwireCodeGenerationLanguage
    {
        private readonly CodeDomProvider _codeDomProvider;

        public CSharpHardwireCodeGenerationLanguage()
        {
            _codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
        }

        public override string Name
        {
            get { return "C#"; }
        }

        public override CodeDomProvider CodeDomProvider
        {
            get { return _codeDomProvider; }
        }

        /// <inheritdoc />
        public override CodeExpression UnaryPlus(CodeExpression arg)
        {
            return SnippetExpression("+{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryNegation(CodeExpression arg)
        {
            return SnippetExpression("-{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryLogicalNot(CodeExpression arg)
        {
            return SnippetExpression("!{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryOneComplement(CodeExpression arg)
        {
            return SnippetExpression("~{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression BinaryXor(CodeExpression arg1, CodeExpression arg2)
        {
            return SnippetExpression("{0} ^ {1}", arg1, arg2);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryIncrement(CodeExpression arg)
        {
            return SnippetExpression("++{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryDecrement(CodeExpression arg)
        {
            return SnippetExpression("--{0}", arg);
        }

        /// <inheritdoc />
        public override string[] GetInitialComment()
        {
            return null;
        }

        /// <inheritdoc />
        public override CodeExpression CreateMultidimensionalArray(
            string type,
            CodeExpression[] args
        )
        {
            CodeSnippetExpression idxexp = new(
                string.Join(", ", args.Select(e => ExpressionToString(e)).ToArray())
            );

            return new CodeArrayCreateExpression(type, idxexp);
        }
    }
}
