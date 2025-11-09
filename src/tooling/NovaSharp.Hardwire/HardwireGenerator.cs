using System.CodeDom.Compiler;
using NovaSharp.Hardwire.Languages;
using NovaSharp.Interpreter;

namespace NovaSharp.Hardwire
{
    public class HardwireGenerator
    {
        readonly HardwireCodeGenerationContext m_Context;
        readonly HardwireCodeGenerationLanguage m_Language;

        public HardwireGenerator(
            string namespaceName,
            string entryClassName,
            ICodeGenerationLogger logger,
            HardwireCodeGenerationLanguage language = null
        )
        {
            m_Language = language ?? HardwireCodeGenerationLanguage.CSharp;
            m_Context = new HardwireCodeGenerationContext(
                namespaceName,
                entryClassName,
                logger,
                language
            );
        }

        public void BuildCodeModel(Table table)
        {
            m_Context.GenerateCode(table);
        }

        public string GenerateSourceCode()
        {
            CodeDomProvider codeDomProvider = m_Language.CodeDomProvider;
            CodeGeneratorOptions codeGeneratorOptions = new();

            using (StringWriter sourceWriter = new())
            {
                codeDomProvider.GenerateCodeFromCompileUnit(
                    m_Context.CompileUnit,
                    sourceWriter,
                    codeGeneratorOptions
                );
                return sourceWriter.ToString();
            }
        }

        public bool AllowInternals
        {
            get { return m_Context.AllowInternals; }
            set { m_Context.AllowInternals = value; }
        }
    }
}
