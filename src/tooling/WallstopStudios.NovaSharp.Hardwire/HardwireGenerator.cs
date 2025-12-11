namespace WallstopStudios.NovaSharp.Hardwire
{
    using System;
    using System.CodeDom.Compiler;
    using System.IO;
    using Languages;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;

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
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                throw new ArgumentException(
                    "Namespace cannot be null or whitespace.",
                    nameof(namespaceName)
                );
            }

            if (string.IsNullOrWhiteSpace(entryClassName))
            {
                throw new ArgumentException(
                    "Entry class name cannot be null or whitespace.",
                    nameof(entryClassName)
                );
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

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
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            _context.GenerateCode(table);
        }

        /// <summary>
        /// Generates source code from the current CodeDOM model.
        /// </summary>
        public string GenerateSourceCode()
        {
            CodeDomProvider codeDomProvider = _language.CodeDomProvider;
            if (codeDomProvider == null)
            {
                throw new InvalidOperationException("CodeDom provider cannot be null.");
            }
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
