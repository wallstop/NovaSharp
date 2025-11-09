namespace NovaSharp.Hardwire
{
    using System.CodeDom.Compiler;
    using Interpreter;
    using Languages;

    public class HardwireGenerator
    {
        private readonly HardwireCodeGenerationContext _context;
        private readonly HardwireCodeGenerationLanguage _language;

        public HardwireGenerator(
            string namespaceName,
            string entryClassName,
            ICodeGenerationLogger logger,
            HardwireCodeGenerationLanguage language = null
        )
        {
            _language = language ?? HardwireCodeGenerationLanguage.CSharp;
            _context = new HardwireCodeGenerationContext(
                namespaceName,
                entryClassName,
                logger,
                language
            );
        }

        public void BuildCodeModel(Table table)
        {
            _context.GenerateCode(table);
        }

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

        public bool AllowInternals
        {
            get { return _context.AllowInternals; }
            set { _context.AllowInternals = value; }
        }
    }
}
