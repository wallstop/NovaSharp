namespace NovaSharp.Hardwire
{
    using System.CodeDom.Compiler;
    using Languages;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Infrastructure;

    /// <summary>
    /// High-level orchestrator that converts Lua dump tables into hardwired source code files.
    /// </summary>
    public class HardwireGenerator
    {
        private readonly HardwireCodeGenerationContext _context;
        private readonly HardwireCodeGenerationLanguage _language;

        public HardwireGenerator(
            string namespaceName,
            string entryClassName,
            ICodeGenerationLogger logger,
            HardwireCodeGenerationLanguage language = null,
            ITimeProvider timeProvider = null
        )
        {
            _language = language ?? HardwireCodeGenerationLanguage.CSharp;
            _context = new HardwireCodeGenerationContext(
                namespaceName,
                entryClassName,
                logger,
                _language,
                timeProvider
            );
        }

        /// <summary>
        /// Builds the CodeDOM model from the provided dump table.
        /// </summary>
        public void BuildCodeModel(Table table)
        {
            _context.GenerateCode(table);
        }

        /// <summary>
        /// Generates source code from the current CodeDOM model.
        /// </summary>
        public string GenerateSourceCode()
        {
            CodeDomProvider codeDomProvider = _language.CodeDomProvider;
            CodeGeneratorOptions codeGeneratorOptions = new();

            using StringWriter sourceWriter = new();
            codeDomProvider.GenerateCodeFromCompileUnit(
                _context.CompileUnit,
                sourceWriter,
                codeGeneratorOptions
            );
            return sourceWriter.ToString();
        }

        /// <summary>
        /// Gets or sets a value indicating whether internal members are eligible for generation.
        /// </summary>
        public bool AllowInternals
        {
            get { return _context.AllowInternals; }
            set { _context.AllowInternals = value; }
        }
    }
}
