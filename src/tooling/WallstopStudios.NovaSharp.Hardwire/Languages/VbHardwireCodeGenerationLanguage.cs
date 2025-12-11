namespace WallstopStudios.NovaSharp.Hardwire.Languages
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Linq;

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
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
            return SnippetExpression("+{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryNegation(CodeExpression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
            return SnippetExpression("-{0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryLogicalNot(CodeExpression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
            return SnippetExpression("NOT {0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryOneComplement(CodeExpression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
            return SnippetExpression("NOT {0}", arg);
        }

        /// <inheritdoc />
        public override CodeExpression BinaryXor(CodeExpression arg1, CodeExpression arg2)
        {
            if (arg1 == null)
            {
                throw new ArgumentNullException(nameof(arg1));
            }

            if (arg2 == null)
            {
                throw new ArgumentNullException(nameof(arg2));
            }
            return SnippetExpression("{0} XOR {1}", arg1, arg2);
        }

        /// <inheritdoc />
        public override CodeExpression UnaryIncrement(CodeExpression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
            return null;
        }

        /// <inheritdoc />
        public override CodeExpression UnaryDecrement(CodeExpression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
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
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Type cannot be null or whitespace.", nameof(type));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            CodeSnippetExpression idxexp = new(
                string.Join(", ", args.Select(e => ExpressionToString(e)).ToArray())
            );

            return new CodeArrayCreateExpression(type, idxexp);
        }
    }
}
