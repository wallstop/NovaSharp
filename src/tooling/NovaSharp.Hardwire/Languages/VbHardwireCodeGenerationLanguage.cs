namespace NovaSharp.Hardwire.Languages
{
    using System.CodeDom;
    using System.CodeDom.Compiler;

    /// <summary>
    /// Language helper that emits VB.NET source for hardwired descriptors.
    /// </summary>
    public class VbHardwireCodeGenerationLanguage : HardwireCodeGenerationLanguage
    {
        private readonly CodeDomProvider _codeDomProvider;

        public VbHardwireCodeGenerationLanguage()
        {
            _codeDomProvider = CodeDomProvider.CreateProvider("VB");
        }

        public override string Name
        {
            get { return "VB.NET"; }
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
            return SnippetExpression("NOT {0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryOneComplement(CodeExpression arg)
        {
            return SnippetExpression("NOT {0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression BinaryXor(CodeExpression arg1, CodeExpression arg2)
        {
            return SnippetExpression("{0} XOR {1}", arg1, arg2);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryIncrement(CodeExpression arg)
        {
            return null;
        }

        /// <inheritdoc />
        public override CodeExpression UnaryDecrement(CodeExpression arg)
        {
            return null;
        }

        /// <inheritdoc />
        public override string[] GetInitialComment()
        {
            return new string[]
            {
                " *** WARNING *** : VB.NET support is experimental and",
                "is not officially supported.",
            };
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
